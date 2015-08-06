module NonLocality

open System.Collections.ObjectModel
open System.Windows
open System.Windows.Controls
open System

open Amazon.S3
open FsXaml
open FSharp.Qualia
open FSharp.Qualia.WPF
open NonLocality.Lib

let cast<'a> (x:obj) : 'a option =
    match x with
    | :? 'a as y -> Some y
    | _ -> None

type Events = Fetch | Remove | SelectionChanged of ControlledFile.T option

type ListViewWindow = XAML<"SyncWindow.xaml", true>

type ItemView(m) =
    inherit View<Events, Label, ControlledFile.T>(Label(), m)
     override x.EventStreams = []
     override x.SetBindings m = x.Root.Content <- m.key

type ListModel() =
    member val Items = new ObservableCollection<ControlledFile.T>()
    member val SelectedItem = new ReactiveProperty<ControlledFile.T option>(None)

type ListAView(elt:ListViewWindow, m) =
    inherit DerivedCollectionSourceView<Events, Window, ListModel>(elt.Root, m)

    override x.EventStreams = [
        (** Add an item when clicking the Add button *)
        elt.button.Click --> Fetch
        (** This one just fetch the selected item, tries to cast it to ItemView, then select the view's Model as an option. *)
        elt.list.SelectionChanged |> Observable.map (fun _ -> SelectionChanged((cast<ItemView> elt.list.SelectedItem |> Option.map(fun v -> v.Model))))
        (** Send a remove event, only when <Del> is pressed *)
        elt.list.KeyDown |> Observable.filter (fun (e:Input.KeyEventArgs) -> e.Key = Input.Key.Delete) |> Observable.mapTo Remove ]
    override x.SetBindings m =
        (** That's all the collection plumbing: which WPF list, how to create a view for each item model, and which model collection to monitor.
            We could use the returned CollectionView to do some filtering/grouping/sorting/... *)
        let collview = x.linkCollection elt.list (fun i -> ItemView(i)) m.Items
        m.SelectedItem |> Observable.add (fun i -> elt.label.Content <- sprintf "Press <DEL> to delete the selection item. Current Selection: %A" i)
        ()
(**
Typical dispatcher - 
*)
type ListController(s3:IAmazonS3, sp:SyncPoint.T) =
    let fetch (m:ListModel) =
        async {
            do m.Items.Clear()
            let! files = sp |> SyncPoint.fetch s3
            do files |> Array.iter (m.Items.Add)
//            return files// |> Array.iter (m.Items.Add)
        }
//        let files = Async.RunSynchronously f
//        m.Items.Clear()
//        files |> Array.iter (m.Items.Add)
    interface IDispatcher<Events,ListModel> with
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
        let sp = SyncPoint.create "sync-bucket-test" "F:\\tmp\\nonlocality"  [||] SyncTrigger.Manual
        let app = App()
        let lm = ListModel()
        let v = ListAView(new ListViewWindow(),lm)
        let c = ListController(s3, sp)
        let loop = EventLoop(v, c)
        use l = loop.Start()
        app.Root.Run(v.Root)

