module Utils

open System.Security.Cryptography
open System.IO
open System

let Md5Hash(filename:string):string =
    use md5 = MD5.Create()
    use stream = File.OpenRead(filename)
    let bytesHash = md5.ComputeHash(stream)
    BitConverter.ToString(bytesHash).Replace("-", "").ToLower()

let merge (a : Map<'a, 'b>) (b : Map<'a, 'b>) (f : 'a -> 'b * 'b -> 'b) =
    Map.fold (fun s k v ->
        match Map.tryFind k s with
        | Some v' -> Map.add k (f k (v, v')) s
        | None -> Map.add k v s) a b