module ProfileWindow

open FsXaml
open FSharp.Qualia
open MahApps.Metro.Controls
open NonLocality.Lib
open Amazon.Runtime

type Profile = { name: string
                 accessKey: string
                 secretKey: string}
type Events = Cancel | CreateProfile of Profile
type Model() =
//    member val ProfileName = ReactiveProperty("")
//    member val AccessKey = ReactiveProperty("")
//    member val SecretKey = ReactiveProperty("")
    member val Credentials : AWSCredentials option = None with get,set


type ProfileWindow = XAML<"ProfileWindow.xaml", true>
type ProfileView (w:ProfileWindow, mw) =
    inherit FSharp.Qualia.View<Events, MetroWindow, Model>(w.Root, mw)
    override x.SetBindings _ = ()
    override x.EventStreams = [
        w.btnCreate.Click |> Observable.map (fun _ ->
            CreateProfile {name=w.tbName.Text; accessKey=w.tbAccessKey.Text; secretKey=w.tbSecretKey.Text})]

type Dispatcher() = 
    let create n a s (m:Model) =
        let cred = Profiles.registerProfile n a s
        m.Credentials <- cred
    interface IDispatcher<Events,Model> with
        member x.Dispatcher =
            function
            | Cancel -> failwith "Not implemented yet"
            | CreateProfile { name=n; accessKey=a; secretKey=s } -> Sync (create n a s)
        
        member x.InitModel _ = 
            ()
        
