module Tray

open System
open System.Reactive.Linq
open System.Windows
open Amazon.S3
open FSharp.Qualia
open NonLocality

type Events = Created | Show | Exit
type Model(c:Config) =
    member val config:Config = c with get,set
    member val s3:IAmazonS3 option = None with get,set
    member val syncpoints: SyncPointConf[] = [||] with get,set//SyncPoint.fromConfig s3.Value c
//    member val sp:SyncPointConf option = None with get,set

let createIcon(app:Application) (m:Model) =
    let addItem (icon:System.Windows.Forms.NotifyIcon) (text:string) handler = icon.ContextMenu.MenuItems.Add(text).Click.Add handler
    let icon = new System.Windows.Forms.NotifyIcon()
    icon.Visible <- true
    icon.Text <- "NonLocality"
    icon.Icon <- new Drawing.Icon("..\\..\\icon.ico")
    icon.ContextMenu <- new Forms.ContextMenu()
    addItem icon "Sync" (fun _ ->
        tracefn "sync"
        let lm = SyncModel(m.s3, m.syncpoints |> Array.tryHead)
        let v = SyncView(new SyncWindow(),lm)
        let c = SyncController()
        use loop = EventLoop(v, c).Start()
        v.Root.ShowDialog() |> ignore
        )
    addItem icon "Exit" (fun  _ -> tracefn "exit"; app.Shutdown())
    icon

type View(app, m) =
    inherit View<Events,System.Windows.Forms.NotifyIcon,Model>((createIcon app m), m)

    override x.SetBindings _ = ()
    override x.EventStreams =
        [ Observable.Return Created
          x.Root.Click --> Show
        ]
let init (m:Model) =
    tracefn "tray created"
    
    m.s3 <- Settings.initS3()
    match m.s3 with
    | Some s3 -> m.syncpoints <- SyncPoint.fromConfig s3 m.config
    | _ -> ()
   // m.sp <- Settings.initSyncPoint()
    //m.s3 <- Settings.initS3 m.sp
let dispatcher = function
| Show -> Sync (fun _ -> ())
| Exit -> Sync (fun _ -> ())
| Created -> Sync init