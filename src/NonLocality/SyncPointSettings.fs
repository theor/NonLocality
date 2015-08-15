namespace SyncPointSettings


open System.Collections.ObjectModel
open FsXaml
open FSharp.Qualia
open FSharp.Qualia.WPF
open MahApps.Metro.Controls
open NonLocality.Lib
open Amazon.Runtime
open System.Windows
open System.Text.RegularExpressions

type Profile = { name: string
                 accessKey: string
                 secretKey: string}
type SubEvents =
| AddRule
type Events =
| Cancel | CreateProfile of Profile | SelectedProfile of string | LoadProfiles
| SubEvent of SubEvents

// Rules
type RuleModel(r:Rule) =
    member val pattern = ReactiveProperty(r.pattern.ToString())
    member val count = ReactiveProperty(r.count)
    member val sync = ReactiveProperty(r.sync)
    member x.ToRule() =
        { pattern = new Regex(x.pattern.Value)
          count = x.count.Value
          sync = x.sync.Value }

type RuleItemControl = XAML<"RuleItem.xaml", true>
type RuleItemView(m, elt:RuleItemControl) =
    inherit View<SubEvents, FrameworkElement, RuleModel>(elt.Root, m)
    
    override x.EventStreams =
        [ //elt.btnDelete.Click |> Observable.map (fun _ ->)
        //elt.cbCount.SelectionChanged |> Observable.map (fun _ -> )
        ]
    override x.SetBindings _ =
        x.Model.pattern |> Observable.add (fun x -> elt.tbPattern.Text <- x)
        x.Model.sync |> Observable.add (fun x ->
             match x with
             | Latest -> elt.cbSyncType.SelectedItem <- elt.cbiSyncTypeLatest)
        x.Model.count |> Observable.add (fun x ->
            match x with
            | All ->
                elt.nupNumber.Visibility <- Visibility.Hidden
                elt.cbSyncType.Visibility <- Visibility.Hidden
                elt.cbCount.SelectedItem <- elt.cbiCountAll
            | Number n ->
                elt.nupNumber.Value <- System.Nullable(float n)
                elt.cbCount.SelectedItem <- elt.cbiCountNumber
            )

// Sync point
type SyncPointSettingsControl = XAML<"SyncPointSettings.xaml", true>

type SyncPointSettingsView(elt:SyncPointSettingsControl, m) =
    inherit DerivedCollectionSourceView<SubEvents, FrameworkElement, SyncPoint>(elt.Root, m)
        override x.SetBindings m =
            elt.tbBucketName.Text <- m.bucketName
            elt.tbPath.Text <- m.path
            match m.trigger with
            | Manual ->
                elt.cbSyncType.SelectedItem <- elt.cbiSyncTypeManual
                elt.nudSyncFreq.Visibility <- Visibility.Hidden
            | Periodic p ->
                elt.cbSyncType.SelectedItem <- elt.cbiSyncTypeScheduled
                elt.nudSyncFreq.Value <- System.Nullable(float p.TotalDays)
                
            ignore <| x.linkCollection elt.listRules (fun x -> RuleItemView(x, RuleItemControl())) (ObservableCollection(m.rules |> Array.map (fun x -> RuleModel(x))))
    override x.EventStreams = [
        elt.btnAddRule.Click --> AddRule
    ]
module Dispatcher =
    let dispatcher x = 
        match x with
        | AddRule -> Sync (fun _ -> tracefn "%A" x)
