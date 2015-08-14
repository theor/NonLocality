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

type SyncPointSettingsView(elt:SyncPointSettingsControl, m) =
    inherit View<SubEvents, FrameworkElement, SyncPoint>(elt.Root, m)
        override x.SetBindings m =
            elt.tbBucketName.Text <- m.bucketName
            elt.tbPath.Text <- m.path
            elt.cbSyncType.Text <- m.trigger.ToString()
    override x.EventStreams = [
        elt.btnAddRule.Click --> AddRule
    ]
module Dispatcher =
    let dispatcher x = 
        match x with
        | AddRule -> Sync (fun _ -> tracefn "%A" x)
