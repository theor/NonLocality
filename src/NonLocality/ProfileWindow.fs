module ProfileWindow

open FsXaml
open FSharp.Qualia
open MahApps.Metro.Controls

type Events = Register
type Model() =
    member val ProfileName = ReactiveProperty("")

type ProfileWindow = XAML<"ProfileWindow.xaml", true>
type ProfileView (w:ProfileWindow, mw) =
    inherit FSharp.Qualia.View<Events, MetroWindow, Model>(w.Root, mw)
    override x.SetBindings(m) = ()
    override x.EventStreams = []

type Dispatcher() = 
    interface IDispatcher<Events,Model> with
        member x.Dispatcher =
            function
            | Register -> failwith "Not implemented yet"
        
        member x.InitModel(m) = 
            ()
        
