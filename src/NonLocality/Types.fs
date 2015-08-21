[<AutoOpen>]
module Types

open System.Collections.ObjectModel
open Amazon.S3
open FSharp.Qualia

type SyncModel(s3, sp) =
    member val Items = new ObservableCollection<FileSyncPreview>()
    member val SelectedItem = new ReactiveProperty<FileSyncPreview option>(None)
    member val Refresh = new ReactiveProperty<Unit>(())
    
    member val s3:IAmazonS3 option = s3 with get,set
    member val sp:SyncPoint option = sp with get,set

