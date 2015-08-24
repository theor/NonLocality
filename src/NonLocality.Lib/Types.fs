[<AutoOpen>]
module Types

open System
open System.Text.RegularExpressions
open Newtonsoft.Json
open Utils

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
    open Amazon.S3.Model
    open System.IO

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

type Count = All | Number of int
type RuleSync = Latest
type Rule = { pattern : Regex
              count : Count
              sync : RuleSync }
              
type SyncTrigger = Manual | Periodic of TimeSpan

type FileSyncAction = NoAction | GetRemote | SendLocal | ResolveConflict
type FileSyncPreview = { file : ControlledFile
                         action : FileSyncAction }
type SyncPointDef =
    { bucketName : string
      pathOverride: string option }

      
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SyncPointDef =
    let load path = Utils.load<SyncPointDef> path
    let save path c = Utils.save<SyncPointDef> path c

type SyncPointConf =
    { syncpoint: SyncPointDef
      path : string
      rules : Rule[]
      trigger : SyncTrigger }


type Config = 
    { profile: string
      syncpoints: SyncPointDef[] }
    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Config =
    let loadFrom path = Utils.load<Config> path
    let getPath() =
        let appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        let folder = System.IO.Path.Combine(appdata, "NonLocality")
        if not <| IO.Directory.Exists folder then IO.Directory.CreateDirectory folder |> ignore
        System.IO.Path.Combine(folder, "config.json")
    let save c = Utils.save<Config> (getPath()) c
    let load() = 
        match loadFrom <| getPath() with
        | Some c -> c
        | None -> {profile="default"; syncpoints=[|{bucketName="asd";pathOverride=None}|]}