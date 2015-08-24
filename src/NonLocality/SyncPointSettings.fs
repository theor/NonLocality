namespace SyncPointSettings


open System.Collections.ObjectModel
open FsXaml
open FSharp.Qualia
open FSharp.Qualia.WPF
open MahApps.Metro.Controls
open NonLocality.Lib
open System.Windows
open System.Text.RegularExpressions

type Profile = { name: string
                 accessKey: string
                 secretKey: string}
type SubEvents =
| AddRule | RemoveRule of RuleModel
and Events =
| Cancel | Save of (unit -> unit) | CreateProfile of Profile | SelectedProfile of string | LoadProfiles
| SubEvent of SyncPointModel * SubEvents



// Rules
and RuleModel(r:Rule) =
    member val pattern = ReactiveProperty(r.pattern.ToString())
    member val count = ReactiveProperty(r.count)
    member val sync = ReactiveProperty(r.sync)
    member x.ToRule() =
        { pattern = new Regex(x.pattern.Value)
          count = x.count.Value
          sync = x.sync.Value }
and SyncPointModel(sp:SyncPointConf) =
    member val rules = ObservableCollection(sp.rules |> Array.map (fun x -> RuleModel(x) ))
    member val trigger = ReactiveProperty(sp.trigger)
    member val path = ReactiveProperty(sp.path)
    member val bucketName = ReactiveProperty(sp.syncpoint.bucketName)

type RuleItemControl = XAML<"RuleItem.xaml", true>
type RuleItemView(m, elt:RuleItemControl) =
    inherit View<SubEvents, FrameworkElement, RuleModel>(elt.Root, m)
    
    override x.EventStreams =
        [ elt.btnDelete.Click |> Observable.map (fun _ -> RemoveRule x.Model)
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

type SyncPointSettingsView(elt:SyncPointSettingsControl, m:SyncPointModel) =
    inherit DerivedCollectionSourceView<SubEvents, FrameworkElement, SyncPointModel>(elt.Root, m)
        override x.SetBindings m =
            m.bucketName |> Observable.add(fun _ -> elt.tbBucketName.Text <- m.bucketName.Value)
            m.path |> Observable.add(fun _ ->elt.tbPath.Text <- m.path.Value)
            m.trigger |> Observable.add(fun _ ->
                match m.trigger.Value with
                | Manual ->
                    elt.cbSyncType.SelectedItem <- elt.cbiSyncTypeManual
                    elt.nudSyncFreq.Visibility <- Visibility.Hidden
                | Periodic p ->
                    elt.cbSyncType.SelectedItem <- elt.cbiSyncTypeScheduled
                    elt.nudSyncFreq.Value <- System.Nullable(float p.TotalDays))
                
            ignore <| x.linkCollection elt.listRules (fun x -> RuleItemView(x, RuleItemControl())) m.rules
    override x.EventStreams = [
        elt.btnAddRule.Click --> AddRule
    ]
module Dispatcher =
    let private removeRule rm (m:SyncPointModel) =
        m.rules.Remove rm |> ignore
    let private addRule (m:SyncPointModel) =
        m.rules.Add(RuleModel(Rule.fromPattern ".*" Count.All))
    let dispatcher x = 
        match x with
        | AddRule -> Sync(addRule)
        | RemoveRule rm -> Sync(removeRule rm)
