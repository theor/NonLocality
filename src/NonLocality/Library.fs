module NonLocality

open System.Collections.ObjectModel
open System.Reactive.Linq
open System.Windows
open System.Windows.Controls
open System

open Amazon.S3
open FsXaml
open FSharp.Qualia
open FSharp.Qualia.WPF
open NonLocality.Lib
open System.Threading

let cast<'a> (x:obj) : 'a option =
    match x with
    | :? 'a as y -> Some y
    | _ -> None

type Events = Fetch | Remove | SelectionChanged of ControlledFile.T option

type SyncWindow = XAML<"SyncWindow.xaml", true>
type SyncItem = XAML<"SyncItem.xaml", true>

type SyncItemView(m, elt:SyncItem) =
    inherit View<Events, FrameworkElement, ControlledFile.T>(elt.Root, m)
     override x.EventStreams = []
     override x.SetBindings m =
        elt.name.Content <- m.key
        elt.status.Content <- sprintf "%A" m.status

type SyncModel() =
    member val Items = new ObservableCollection<ControlledFile.T>()
    member val SelectedItem = new ReactiveProperty<ControlledFile.T option>(None)

type SyncView(elt:SyncWindow, m) =
    inherit DerivedCollectionSourceView<Events, Window, SyncModel>(elt.Root, m)

    override x.EventStreams = [
        elt.button.Click --> Fetch
        elt.list.SelectionChanged |> Observable.map (fun _ -> SelectionChanged((cast<SyncItemView> elt.list.SelectedItem |> Option.map(fun v -> v.Model))))
        elt.list.KeyDown |> Observable.filter (fun (e:Input.KeyEventArgs) -> e.Key = Input.Key.Delete) |> Observable.mapTo Remove
        Observable.Return Fetch ]
    override x.SetBindings m =
        let collview = x.linkCollection elt.list (fun i -> SyncItemView(i, SyncItem())) m.Items
        m.SelectedItem |> Observable.add (fun i -> elt.label.Content <- sprintf "Press <DEL> to delete the selection item. Current Selection: %A" i)
        ()

type SyncController(s3:IAmazonS3, sp:SyncPoint.T) =
    let fetch (m:SyncModel) =
        async {
            do m.Items.Clear()
            let! files = sp |> SyncPoint.fetch s3
            do files |> Array.iter (m.Items.Add)
        }
    interface IDispatcher<Events,SyncModel> with
        member this.InitModel m = ()
        member this.Dispatcher = 
            function
            | Fetch -> Async fetch
            | Remove -> Sync (fun m -> m.SelectedItem.Value |> Option.iter (m.Items.Remove >> ignore))
            | SelectionChanged item -> printfn "%A" item; Sync (fun m -> m.SelectedItem.Value <- item)

type App = XAML<"App.xaml">

[<EntryPoint>]
[<STAThread>]
let main args =
    let p = Profiles.getProfile()
    if Option.isNone p then 1
    else
        let pp = p.Value
        let s3 = Profiles.createClient pp
//        let buckets = SyncPoint.listBuckets s3
        let sp = SyncPoint.load "..\\..\\sp.json"
//        let sp = SyncPoint.create "sync-bucket-test" "F:\\tmp\\nonlocality"  [||] SyncTrigger.Manual
//        let json = JsonConvert.SerializeObject(sp, Formatting.Indented)
//        do System.IO.File.WriteAllText(path, json)
        let app = App()
        let lm = SyncModel()
        let v = SyncView(new SyncWindow(),lm)
        let c = SyncController(s3, sp)
        let loop = EventLoop(v, c)
//        use l = loop.Start()
        WpfApp.runApp loop v app.Root

