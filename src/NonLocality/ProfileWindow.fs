namespace ProfileWindow

open System.Collections.ObjectModel
open FsXaml
open FSharp.Qualia
open MahApps.Metro.Controls
open NonLocality.Lib
open Amazon.Runtime

type Profile = { name: string
                 accessKey: string
                 secretKey: string}
type Events = Cancel | CreateProfile of Profile | SelectedProfile of string | LoadProfiles
type Model() =
//    member val ProfileName = ReactiveProperty("")
//    member val AccessKey = ReactiveProperty("")
//    member val SecretKey = ReactiveProperty("")
    member val Profiles:ObservableCollection<string> = ObservableCollection()
    member val SelectedProfile = ""
    member val Credentials : AWSCredentials option = None with get,set


type ProfileWindow = XAML<"ProfileWindow.xaml", true>
type ProfileView (w:ProfileWindow, mw) =
    inherit FSharp.Qualia.WPF.DerivedCollectionSourceView<Events, MetroWindow, Model>(w.Root, mw)
    override x.SetBindings m =
        w.cbProfiles.ItemsSource <- m.Profiles
    override x.EventStreams = [
        w.Root.Loaded --> LoadProfiles
        w.cbProfiles.SelectionChanged |> Observable.map (fun _ -> SelectedProfile (string w.cbProfiles.SelectedItem))
        w.btnCreate.Click |> Observable.map (fun _ ->
            CreateProfile {name=w.tbName.Text; accessKey=w.tbAccessKey.Text; secretKey=w.tbSecretKey.Text})]

type Dispatcher() = 
    let create n a s (m:Model) =
        let cred = Profiles.registerProfile n a s
        m.Credentials <- cred
    interface IDispatcher<Events,Model> with
        member x.Dispatcher =
            function
            | LoadProfiles -> Sync (fun m -> m.Profiles.Clear(); Profiles.listProfiles() |> Seq.iter m.Profiles.Add)
            | Cancel -> failwith "Not implemented yet"
            | CreateProfile { name=n; accessKey=a; secretKey=s } -> Sync (create n a s)
            | SelectedProfile s -> Sync (fun m -> ())
        
        member x.InitModel _ = 
            ()
        
