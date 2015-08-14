namespace SyncPointSettings


open System.Collections.ObjectModel
open FsXaml
open FSharp.Qualia
open MahApps.Metro.Controls
open NonLocality.Lib
open Amazon.Runtime
open System.Windows

type Profile = { name: string
                 accessKey: string
                 secretKey: string}
type SubEvents =
| AddRule
type Events =
| Cancel | CreateProfile of Profile | SelectedProfile of string | LoadProfiles
| SubEvent of SubEvents


type SyncPointSettingsControl = XAML<"SyncPointSettings.xaml", true>
type Model() =
    member val SyncPoint:SyncPoint option = None

type SyncPointSettingsView(elt:SyncPointSettingsControl, m) =
    inherit View<SubEvents, FrameworkElement, Model>(elt.Root, m)
        override x.SetBindings m =()
    override x.EventStreams = [
        elt.btnAddRule.Click --> AddRule
    ]