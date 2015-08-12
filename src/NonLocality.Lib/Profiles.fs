namespace NonLocality.Lib

module Profiles =

    open Amazon
    open Amazon.Util
    open Amazon.S3.Model

    let getProfile() =
        let l = ProfileManager.ListProfileNames() |> List.ofSeq    
        match l with
        | [] -> None
        | p :: _ -> Some (ProfileManager.GetAWSCredentials(p))
    let registerProfile profileName accessKeyId secretKey =
        ProfileManager.RegisterProfile(profileName, accessKeyId, secretKey)
        ProfileManager.GetAWSCredentials(profileName) |> Option.ofObj
    let listProfiles() = ProfileManager.ListProfileNames()
    let createClient (p:Amazon.Runtime.AWSCredentials) =
        AWSClientFactory.CreateAmazonS3Client(p, Amazon.RegionEndpoint.USEast1)