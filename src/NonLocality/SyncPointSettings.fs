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
type Events =
| Cancel | CreateProfile of Profile | SelectedProfile of string | LoadProfiles
| AddRule

type SyncPointSettingsControl = XAML<"SyncPointSettings.xaml", true>
type Model() =
    member val SyncPoint:SyncPoint option = None

type SyncPointSettingsView(elt:SyncPointSettingsControl, m) =
    inherit View<Events, FrameworkElement, Model>(elt.Root, m)
        override x.SetBindings m =()
    override x.EventStreams = [
        elt.btnAddRule.Click --> AddRule
    ]