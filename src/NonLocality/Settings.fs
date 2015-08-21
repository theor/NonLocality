module Settings

open System.Windows
open FSharp.Qualia

let openSettings (m:SyncPoint option) =
    let prd = ProfileWindow.Dispatcher()
    let prm = ProfileWindow.Model(m |> Option.map (fun x -> SyncPointSettings.SyncPointModel(x)))
    let prw = ProfileWindow.ProfileView(ProfileWindow.ProfileWindow(), prm)
    use ev = EventLoop(prw, prd).Start()
    prw.Root.Owner <- Application.Current.MainWindow
    match prw.Root.ShowDialog() |> Option.ofNullable with
    | Some true -> prm.Credentials
    | _ -> None
        
let initSyncPoint() =
    match SyncPoint.load "..\\..\\sp.json" with
    | None ->
        let sp = SyncPoint.create "sync-bucket-test" @"G:\tmp\nonlocality" [| Rule.fromPattern ".*\\.jpg" (Number 1)
                                                                              Rule.fromPattern ".*\\.png" All|] SyncTrigger.Manual
        SyncPoint.save "..\\..\\sp.json" sp
        Some sp
    | Some sp -> Some sp
let initS3 sp =
    let p = NonLocality.Lib.Profiles.getProfile()
    match p with
    | None ->
        match openSettings sp with
        | Some c -> Some <| Amazon.AWSClientFactory.CreateAmazonS3Client(c, Amazon.RegionEndpoint.USEast1)
        | None -> None
    | Some pp -> Some <| NonLocality.Lib.Profiles.createClient pp
let init (m:SyncModel) =
    m.sp <- initSyncPoint()
    m.s3 <- initS3(m.sp)