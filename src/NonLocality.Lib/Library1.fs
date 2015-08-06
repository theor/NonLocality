namespace NonLocality.Lib

open Amazon.Util

module Profiles =
    open Amazon

    let getProfile =
        let l = ProfileManager.ListProfileNames() |> List.ofSeq    
        match l with
        | [] -> None
        | p :: _ -> Some (ProfileManager.GetAWSCredentials(p))
    let createClient (p:Amazon.Runtime.AWSCredentials) =
        AWSClientFactory.CreateAmazonS3Client(p, Amazon.RegionEndpoint.USEast1)
    
type SyncPoint(bucketName) =
    member x.BucketName = bucketName