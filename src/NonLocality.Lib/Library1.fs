namespace NonLocality.Lib

open System
open System.IO
open System.Text.RegularExpressions
open Amazon
open Amazon.Util
open Amazon.S3.Model

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
        | Number(n) -> files |> Array.sortByDescending modifiedDate |> Array.take (min n files.Length)
        
    let matchRule (files:ControlledFile[]) (result:Set<ControlledFile>) (r:Rule) =
        match r.sync with
        | Latest -> files
                    |> Array.filter (fun f ->  r.pattern.IsMatch f.key)
                    |> takeRule r.count
                    |> Set.ofArray
                    |> Set.union result
//        | _ -> failwith "not implemented"
    let matchRules (sp:SyncPointConf) files =
        sp.rules |> Array.fold (matchRule files) Set.empty |> Array.ofSeq
//        files |> Array.filter (fun f -> Array.exists (matchRule f) sp.rules)
                         

//        let tasksGet = toGet |> Array.map get
//        let tasksSet = toSet |> Array.map put
//        Array. |> Async.Parallel