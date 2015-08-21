module Main

open System
open System.Windows
open FsXaml
open NonLocality
open FSharp.Qualia

type App = XAML<"App.xaml">

[<EntryPoint>]
[<STAThread>]
let main _ =
//        let buckets = SyncPoint.listBuckets s3
//        let sp = SyncPoint.create "sync-bucket-test" "F:\\tmp\\nonlocality"  [||] SyncTrigger.Manual
//        let sp = { sp with rules = [| Rule.fromPattern "\\*\\.jpg" (Number 1) |] }
//        SyncPoint.save "..\\..\\sp.json" sp
//        let sp = SyncPoint.create "sync-bucket-test" "F:\\tmp\\nonlocality"  [||] SyncTrigger.Manual
//        let json = JsonConvert.SerializeObject(sp, Formatting.Indented)
//        do System.IO.File.WriteAllText(path, json)
    let app = App()
    app.Root.ShutdownMode <- ShutdownMode.OnExplicitShutdown

    let tm:Tray.Model = Tray.Model()
    let tv = Tray.View(app.Root, tm)
    let td = FSharp.Qualia.Dispatcher.fromHandler Tray.dispatcher
    use loop = EventLoop(tv, td).Start()
    app.Root.Run()

//    let lm = SyncModel()
//    let v = SyncView(new SyncWindow(),lm)
//    let c = SyncController()
//    let loop = EventLoop(v, c)
//
//    WpfApp.runApp loop v app.Root