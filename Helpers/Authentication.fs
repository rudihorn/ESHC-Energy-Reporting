module EnergyReporting.Helpers.Authentication

open System
open System.Linq
open System.Security.Cryptography

open EnergyReporting.Database

(* generates a random authentication key for the user *)

type UserAuth = {
    is_master : bool;
    uid : string;
    name : string;
    flat : string option;
    key : string;
}

type Database = EnergyDatabase

let genAuthKey () =
    let rng = RandomNumberGenerator.Create ()
    let set = List.concat [['0'..'9'];['a'..'z'];['A'..'Z']] |> Array.ofList
    let nextChar () = 
        let bytes : byte [] = Array.create 4 (byte 0)
        rng.GetBytes(bytes)
        let i = BitConverter.ToUInt32(bytes, 0)
        let len = uint32 set.Length
        set.[i % len |> int]
    let key = [|1..32|] |> Array.map (fun _ -> nextChar ()) |> String
    key

let getUserAuth (db : Database) username flat = 
    let mutable user = 
        db.UserAuths.FirstOrDefault(fun u -> u.user = username)
    
    match user with
    | null ->
        user <- UserAuth (user = username, flat = flat, key = genAuthKey ())
        db.UserAuths.Add(user) |> ignore
    | _ -> 
        user.flat <- flat
    user

let authMaster (db : Database) uid key =    
    let ma = db.MasterAuths.FirstOrDefault(fun ma -> ma.user = uid && ma.key = key)
    Null.option ma
    |> Option.map (fun ma -> { is_master = true; uid = ma.user; name = ma.name; flat = None; key = ma.key })

let authUser (db : Database) uid key flat =
    let auth = db.UserAuths.FirstOrDefault(fun auth -> auth.user = uid && auth.key = key && auth.flat = flat)
    Null.option auth
    |> Option.map (fun auth -> { is_master = false; uid = auth.user; name = auth.name; flat = Some auth.flat; key = auth.key })

let authAny (db : Database) uid key flat =
    authMaster db uid key
    |> Option.map Some
    |> Option.defaultWith (fun () -> authUser db uid key flat)