namespace ProfileWindow

open System.Collections.ObjectModel
open FsXaml
open FSharp.Qualia
open MahApps.Metro.Controls
open NonLocality.Lib
open SyncPointSettings
open System.Windows
open Amazon.Runtime
open Chessie.ErrorHandling


type Model()=
    member val Profiles:ObservableCollection<string> = ObservableCollection()
    member val SelectedProfile = ReactiveProperty("") with get,set
    member val Credentials : AWSCredentials option = None with get,set
    //member val SyncPoint: SyncPointModel option = sp

type ProfileWindow = XAML<"ProfileWindow.xaml", true>
type ProfileView (w:ProfileWindow, mw) =
    inherit FSharp.Qualia.WPF.DerivedCollectionSourceView<Events, MetroWindow, Model>(w.Root, mw)
    override x.SetBindings m =
        w.cbProfiles.ItemsSource <- m.Profiles
        m.SelectedProfile |> Observable.add (fun _ -> w.cbProfiles.SelectedValue <- m.SelectedProfile.Value)
//        let spView = SyncPointSettings.SyncPointSettingsView(w.ucSyncPoint :?> SyncPointSettingsControl, mw.SyncPoint.Value)
//        x.ComposeViewEvents spView (fun x -> SubEvent(mw.SyncPoint.Value, x)) |> ignore
        w.btnCancel.Click |> Observable.add (fun _ -> w.Root.Close())
    override x.EventStreams = [
        w.Root.Loaded --> LoadProfiles
        w.cbProfiles.SelectionChanged |> Observable.map (fun _ -> SelectedProfile (string w.cbProfiles.SelectedItem))
        w.btnCreate.Click |> Observable.map (fun _ ->
            CreateProfile {name=w.tbName.Text; accessKey=w.tbAccessKey.Text; secretKey=w.tbSecretKey.Text})
        w.btnSave.Click --> Save (fun () -> w.Root.Close())]

type Dispatcher() = 
    let create n a s (m:Model) =
        let cred = Profiles.registerProfile n a s
        m.Credentials <- cred
        ok ()
    interface IDispatcher<Events,Model> with
        member x.Dispatcher =
            function
            | Cancel -> Sync (ignore >> ok)
            | Save onsuccess -> Sync (fun _ -> onsuccess() |> ok)
            | LoadProfiles -> Sync (fun m ->
                let profiles = Profiles.listProfiles()
                m.Profiles.Clear()
                profiles |> Seq.iter m.Profiles.Add
                if not <| Seq.isEmpty profiles then
                    m.SelectedProfile.Value <- profiles |> Seq.head
                ok ())
            | CreateProfile { name=n; accessKey=a; secretKey=s } -> Sync (create n a s)
            | SelectedProfile _ -> Sync (fun _ -> ok ())
            | SubEvent (mm,x) -> match SyncPointSettings.Dispatcher.dispatcher x with
                                 | Sync f -> Sync (fun _ -> f mm)
                                 | Async f -> Async (fun _ -> f mm)
        member x.InitModel _ = 
            ()
        
