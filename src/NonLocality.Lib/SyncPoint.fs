[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SyncPoint

open Newtonsoft.Json
open Newtonsoft.Json.Converters
open System.Threading
open System
open System.IO
open Amazon
open Amazon.S3.Model

type FetchResult = SyncPointConf * DateTime * ControlledFile[]

let private settings = new JsonSerializerSettings(Converters=ResizeArray([RegexConverter() :> JsonConverter]),
                                                    Formatting = Formatting.Indented)
let save path sp = 
    let json = JsonConvert.SerializeObject(sp, settings)
    do System.IO.File.WriteAllText(path, json)
let load path =
    if not <| File.Exists path
    then None
    else
        let json = File.ReadAllText(path)
        Some <| JsonConvert.DeserializeObject<SyncPointConf>(json, settings)

let create conf path rules trigger : SyncPointConf =
    { syncpoint = conf
      path = path
      rules = rules
      trigger = trigger }

let localPath sp f = Path.Combine(sp.path, f.key)

let listBuckets (s3:S3.IAmazonS3) = s3.ListBuckets().Buckets
let listLocalFiles sp =
    let d =
        if not <| Directory.Exists sp.path
        then Directory.CreateDirectory sp.path
        else new DirectoryInfo(sp.path)
    d.GetFiles() |> Array.ofSeq
let listRemoteFiles (s3:S3.IAmazonS3) sp = 
    async {
        let! r = s3.ListObjectsAsync (S3.Model.ListObjectsRequest(BucketName = sp.bucketName)) |> Async.AwaitTask
        return r.S3Objects |> Seq.map ControlledFile.fromS3Object |> List.ofSeq
    }

   
let fetch (s3:S3.IAmazonS3) withRules (sp:SyncPointConf) =
    let fileKey (f:ControlledFile) = (f.key,f)
    async {
        let time = DateTime.Now
        let! rf = s3.ListObjectsAsync (S3.Model.ListObjectsRequest(BucketName = sp.syncpoint.bucketName)) |> Async.AwaitTask
        let remotes = rf.S3Objects |> Seq.map (ControlledFile.fromS3Object >> fileKey) |> Map.ofSeq
        let locals = listLocalFiles sp |> Array.map ControlledFile.fromLocal |> Array.map fileKey |> Map.ofSeq
        let files = Utils.merge remotes locals (fun k (r,l) -> ControlledFile.fromLocalRemote l r)
                    |> Map.toSeq
                    |> Seq.map snd
                    |> Seq.toArray
        let finalFiles = if withRules then files |> Rule.matchRules sp else files
        return (sp,time,finalFiles)
//            return rmap
    }
let syncPreview (s3:S3.IAmazonS3) sp (files:ControlledFile[]) =
    let determineAction f =
        match f.status with
        | Identical -> NoAction
        | Local -> SendLocal
        | Remote -> GetRemote
    files |> Array.map (fun f -> { file = f; action = determineAction f })


let doSync (s3:S3.IAmazonS3) sp (files:FileSyncPreview[]) =
    let get f = 
        async {
            let! resp = s3.GetObjectAsync(GetObjectRequest(BucketName=sp.syncpoint.bucketName, Key=f.file.key)) |> Async.AwaitTask
            do! resp.WriteResponseStreamToFileAsync((localPath sp f.file), false, CancellationToken.None) |> Async.AwaitTask
        }
                            
    let put f =
        s3.PutObjectAsync(PutObjectRequest(BucketName=sp.syncpoint.bucketName, Key=f.file.key,FilePath=localPath sp f.file)) |> Async.AwaitTask
        
    let toGet, toSet = files
                        |> Array.filter (fun f -> f.action <> NoAction)
                        |> Array.partition (fun f -> f.action = GetRemote)
    async {
        let gets = toGet |> Array.map (get >> Async.RunSynchronously)
        return gets
    }