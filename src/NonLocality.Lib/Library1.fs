namespace NonLocality.Lib

open System
open System.IO
open System.Text.RegularExpressions
open Amazon
open Amazon.Util
open Amazon.S3.Model



type Status = Identical | Local | Remote    

[<StructuredFormatDisplay("{key}: {status}")>]
type ControlledFile =
    { key:string
      etag:string
      status:Status
      localModifiedDate:DateTime option
      remoteModifiedDate:DateTime option }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ControlledFile =
    let fromS3Object (o:S3Object) =
        { key = o.Key
          etag = o.ETag
          status = Remote
          localModifiedDate = None
          remoteModifiedDate = Some o.LastModified }

    let fromLocal (o:FileInfo) =
        { key = o.Name
          etag = Utils.Md5Hash o.FullName
          status = Local
          localModifiedDate = Some o.CreationTime
          remoteModifiedDate = None }
          
    let fromLocalRemote (r:ControlledFile) (l:ControlledFile) =
        { key = r.key
          etag = r.etag
          status = Identical
          localModifiedDate = l.localModifiedDate
          remoteModifiedDate = r.remoteModifiedDate }

type Count = All | Zero | Number of int
type RuleSync = Latest
type Rule = { pattern : Regex
              count : Count
              sync : RuleSync }
              
type SyncTrigger = Manual | Periodic of TimeSpan

type FileSyncAction = NoAction | GetRemote | SendLocal | ResolveConflict
type FileSyncPreview = { file : ControlledFile
                         action : FileSyncAction }
type SyncPoint =
    { bucketName : string
      path : string
      rules : Rule[]
      trigger : SyncTrigger }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Rule =
    let fromPattern pattern count =
        { pattern = Regex(pattern)
          count = count
          sync = Latest }

    let modifiedDate (f:ControlledFile) =
        match f.localModifiedDate, f.remoteModifiedDate with
        | Some d, None | None, Some d -> d
        | Some a, Some b -> max a b
        | _ -> failwith "Controlled file has neither a local modification date nor a remote one"

    let takeRule (c:Count) (files:ControlledFile[]) =
        match c with
        | All -> files
        | Zero -> Array.empty
        | Number(n) -> files |> Array.sortByDescending modifiedDate |> Array.take (min n files.Length)
        
    let matchRule (files:ControlledFile[]) (result:Set<ControlledFile>) (r:Rule) =
        match r.sync with
        | Latest -> files
                    |> Array.filter (fun f ->  r.pattern.IsMatch f.key)
                    |> takeRule r.count
                    |> Set.ofArray
                    |> Set.union result
//        | _ -> failwith "not implemented"
    let matchRules (sp:SyncPoint) files =
        sp.rules |> Array.fold (matchRule files) Set.empty |> Array.ofSeq
//        files |> Array.filter (fun f -> Array.exists (matchRule f) sp.rules)
                         

//        let tasksGet = toGet |> Array.map get
//        let tasksSet = toSet |> Array.map put
//        Array. |> Async.Parallel