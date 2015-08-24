module NonLocality

open System.Collections.ObjectModel
open System.Reactive.Linq
open System.Windows
open System.Windows.Controls
open System
open System.IO
open System.Diagnostics

//open Amazon.S3
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

        
        
    let fetch (m:SyncModel) =
        match m.sp, m.s3 with
        | Some sp, Some s3 ->
            async {
                do m.Items.Clear()
                let! (_,_,files) = m.sp.Value |> SyncPoint.fetch s3 true
                let syncPreview = files |> SyncPoint.syncPreview s3 sp
                do syncPreview |> Array.iter (m.Items.Add)
            }
        | _ -> failwith "not init"// init m
    let openfolder (m:SyncModel) =
        m.sp |> Option.iter (fun s -> if Directory.Exists(s.path) then Process.Start s.path |> ignore)
    member x.doSync (m:SyncModel) =
        async {
            do! SyncPoint.doSync m.s3.Value m.sp.Value (Array.ofSeq m.Items) |> Async.Ignore
            do! fetch m
        }
    interface IDispatcher<Events,SyncModel> with
        member x.InitModel _ =()
        member x.Dispatcher = 
            function
            | OpenSettings -> Sync (fun _ -> Settings.openProfileSettings() |> ignore)
            | Fetch -> Async fetch
            | Remove -> Sync (fun m -> m.SelectedItem.Value |> Option.iter (m.Items.Remove >> ignore))
            | SelectionChanged item -> printfn "%A" item; Sync (fun m -> m.SelectedItem.Value <- item)
            | DoSync -> Async x.doSync
            | OpenSyncFolder -> Sync openfolder
