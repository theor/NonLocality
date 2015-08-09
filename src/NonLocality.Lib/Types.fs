[<AutoOpen>]
module Types

open System
open System.Text.RegularExpressions

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