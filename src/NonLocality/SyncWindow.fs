module NonLocality

open System.Collections.ObjectModel
open System.Reactive.Linq
open System.Windows
open System.Windows.Controls
open System
open System.IO
open System.Diagnostics

open Amazon.S3
open FsXaml
open FSharp.Qualia
open FSharp.Qualia.WPF
open System.Threading
open MahApps.Metro.Controls

let cast<'a> (x:obj) : 'a option =
    match x with
    | :? 'a as y -> Some y
    | _ -> None

type Events =
    | OpenSettings | OpenSyncFolder | DoSync | Fetch | Remove | SelectionChanged of FileSyncPreview option

type SyncWindow = XAML<"SyncWindow.xaml", true>
type SyncItem = XAML<"SyncItem.xaml", true>

type SyncItemView(m, elt:SyncItem) =
    inherit View<Events, FrameworkElement, FileSyncPreview>(elt.Root, m)
    let {file=file;action=action} = m
    override x.EventStreams = []
    override x.SetBindings _ =
        elt.name.Content <- file.key
        elt.status.Content <- sprintf "%A" file.status
        match action with
        | NoAction -> elt.actionSkip.IsChecked <- Nullable true
        | GetRemote -> elt.actionGet.IsChecked <- Nullable true
        | SendLocal -> elt.actionSend.IsChecked <- Nullable true
        | ResolveConflict -> failwith "Not implemented yet"

//        elt.action.Content <- sprintf "%A" action

type SyncModel() =
    member val Items = new ObservableCollection<FileSyncPreview>()
    member val SelectedItem = new ReactiveProperty<FileSyncPreview option>(None)
    member val Refresh = new ReactiveProperty<Unit>(())
    
    member val s3:IAmazonS3 = null with get,set
    member val sp:SyncPoint option = None with get,set

type SyncView(elt:SyncWindow, m) =
    inherit DerivedCollectionSourceView<Events, MetroWindow, SyncModel>(elt.Root, m)

    do
        elt.buttonCancel.Click.Add (fun _ -> elt.Root.Close())

    override x.EventStreams = [
        elt.Root.Loaded --> Fetch
        elt.Root.Loaded --> OpenSettings
        elt.btnOpenFolder.Click --> OpenSyncFolder
        elt.btnSettings.Click --> OpenSettings
        elt.buttonSync.Click --> DoSync
        elt.button.Click --> Fetch
        elt.list.SelectionChanged |> Observable.map (fun _ -> SelectionChanged((cast<SyncItemView> elt.list.SelectedItem |> Option.map(fun v -> v.Model))))
        elt.list.KeyDown |> Observable.filter (fun (e:Input.KeyEventArgs) -> e.Key = Input.Key.Delete) |> Observable.mapTo Remove ]
    override x.SetBindings m =
        let _ = x.linkCollection elt.list (fun i -> SyncItemView(i, SyncItem())) m.Items
        m.SelectedItem |> Observable.add (fun i -> elt.label.Content <- sprintf "Press <DEL> to delete the selection item. Current Selection: %A" i)
        ()

type SyncController() =
    let openSettings (m:SyncPoint option) =
        let prd = ProfileWindow.Dispatcher()
        let prm = ProfileWindow.Model(m |> Option.map (fun x -> SyncPointSettings.SyncPointModel(x)))
        let prw = ProfileWindow.ProfileView(ProfileWindow.ProfileWindow(), prm)
        use ev = EventLoop(prw, prd).Start()
        prw.Root.Owner <- Application.Current.MainWindow
        match prw.Root.ShowDialog() |> Option.ofNullable with
        | Some true -> prm.Credentials
        | _ -> None
        
    let init (m:SyncModel) =
        m.sp <- match SyncPoint.load "..\\..\\sp.json" with
                | None -> SyncPoint.create "sync-bucket-test" @"G:\tmp\nonlocality" [|Rule.fromPattern ".*\\.jpg" (Number 1)
                                                                                      Rule.fromPattern ".*\\.png" All|] SyncTrigger.Manual |> Some
                | Some sp -> Some sp
        let p = NonLocality.Lib.Profiles.getProfile()
        match p with
        | None ->
            match openSettings m.sp with
            | Some c -> m.s3 <- Amazon.AWSClientFactory.CreateAmazonS3Client(c, Amazon.RegionEndpoint.USEast1)
            | None -> failwith "NO CREDENTIALS"
        | Some pp -> m.s3 <- NonLocality.Lib.Profiles.createClient pp
        
    let fetch (m:SyncModel) =
        match m.sp, m.s3 with
        | None, _ | _, null -> init m
        | _ -> ()
        async {
            do m.Items.Clear()
            let! (_,_,files) = m.sp.Value |> SyncPoint.fetch m.s3 true
            let syncPreview = files |> SyncPoint.syncPreview m.s3 m.sp.Value
            do syncPreview |> Array.iter (m.Items.Add)
        }
    let openfolder (m:SyncModel) =
        m.sp |> Option.iter (fun s -> if Directory.Exists(s.path) then Process.Start s.path |> ignore)
        
    member x.doSync (m:SyncModel) =
        async {
            do! SyncPoint.doSync m.s3 m.sp.Value (Array.ofSeq m.Items) |> Async.Ignore
            do! fetch m
        }
    interface IDispatcher<Events,SyncModel> with
        member x.InitModel _ =()
        member x.Dispatcher = 
            function
            | OpenSettings -> Sync (fun m -> openSettings(m.sp) |> ignore)
            | Fetch -> Async fetch
            | Remove -> Sync (fun m -> m.SelectedItem.Value |> Option.iter (m.Items.Remove >> ignore))
            | SelectionChanged item -> printfn "%A" item; Sync (fun m -> m.SelectedItem.Value <- item)
            | DoSync -> Async x.doSync
            | OpenSyncFolder -> Sync openfolder
type App = XAML<"App.xaml">

//let run pp =

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
    let lm = SyncModel()
    let v = SyncView(new SyncWindow(),lm)
    let c = SyncController()
    let loop = EventLoop(v, c)
//        use l = loop.Start()
    WpfApp.runApp loop v app.Root

