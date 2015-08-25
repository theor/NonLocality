module Settings

open System.Windows
open FSharp.Qualia
open Chessie.ErrorHandling

let openProfileSettings () =
    let prd = ProfileWindow.Dispatcher()
    let prm = ProfileWindow.Model()//m |> Option.map (fun x -> SyncPointSettings.SyncPointModel(x)))
    let prw = ProfileWindow.ProfileView(ProfileWindow.ProfileWindow(), prm)
    use ev = EventLoop(prw, prd).Start()
    prw.Root.Owner <- Application.Current.MainWindow
    match prw.Root.ShowDialog() |> Option.ofNullable with
    | Some true -> prm.Credentials
    | _ -> None

        
let initSyncPoint(c:Config) = None
//let initSyncPoint() =
//    let def = {bucketName="sync-bucket-test"; pathOverride=None}
//    match SyncPoint.load "..\\..\\sp.json" with
//    | None ->
//        let sp = SyncPoint.create def @"G:\tmp\nonlocality" SyncTrigger.Manual
//                                  [| Rule.fromPattern ".*\\.jpg" (Number 1)
//                                     Rule.fromPattern ".*\\.png" All|]
//        SyncPoint.save "..\\..\\sp.json" sp
//        Some sp
//    | Some sp -> Some sp

let initS3(c) =
    let p = NonLocality.Lib.Profiles.getProfile(c)
    
    match p with
//        match openProfileSettings() with
//        | Some c -> Some <| Amazon.AWSClientFactory.CreateAmazonS3Client(c, Amazon.RegionEndpoint.USEast1)
//        | None -> None
    | Pass(_:Amazon.Runtime.AWSCredentials) -> None // Some <| NonLocality.Lib.Profiles.createClient pp
    | Fail x -> None
let init (_:SyncModel) =
    
   // m.sp <- initSyncPoint()
   // m.s3 <- initS3(m.sp)
   ()