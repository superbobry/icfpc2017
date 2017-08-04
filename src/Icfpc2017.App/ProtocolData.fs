module ProtocolData

open System.Linq
open Newtonsoft.Json
open Newtonsoft.Json.Linq

// 0. Handshake

type HandshakeOut = {
    me : string
}

type HandshakeIn = {
    you : string
}

// 1. Setup

type Site = {
    id : int
}

type River = {
    source : int
    target : int
}

type Map = {
    sites : Site array
    rivers : River array
    mines : int array
}

type SetupIn = {
    punter : int
    punters : int
    map : Map
}

type SetupOut = {
    ready : int
}

// 2. Gameplay

type Claim = {
    punter : int
    source : int
    target : int
}

type Pass = {
    punter : int
}

type Move =
    | Claim of claim : Claim
    | Pass of pass : Pass

type Moves = {
    moves : Move array
}

type MoveIn = {
    move : Moves
}

type MoveOut = Move

// 3. Scoring

type Score = {
    punter : int
    score : int
}

type Stop = {
    moves : Move array
    scores : Score array
}

type StopIn = {
    stop : Stop
}

// Misc

type TimeoutIn = {
    timeout : int
}

type MessageIn =
    | Handshake of HandshakeIn
    | Setup of SetupIn
    | Move of MoveIn
    | Stop of StopIn
    | Timeout of TimeoutIn

type MessageOut = MoveOut

let convertArray (arr : JToken) (doDeserialize : 'a -> 'b) : 'b array =
    (arr :?> JArray)
        |> Seq.cast<'a>
        |> Seq.map doDeserialize
        |> Seq.toArray

let deserializeHandshakeIn (o : JObject) : HandshakeIn =
    {
        you = o.["you"].ToObject<string>()
    }

let deserializeSite (o : JObject) : Site =
    {
        id = o.["id"].ToObject<int>()
    }

let deserializeRiver (o : JObject) : River =
    {
        source = o.["source"].ToObject<int>()
        target = o.["target"].ToObject<int>()
    }

let deserializeMap (o : JObject) : Map =
    {
        sites = convertArray o.["sites"] deserializeSite
        rivers = convertArray o.["rivers"] deserializeRiver
        mines = convertArray o.["mines"] (fun (v : JToken) -> v.ToObject<int>())
    }

let deserializeSetupIn (o : JObject) : SetupIn =
    {
        punter = o.["punter"].ToObject<int>()
        punters = o.["punters"].ToObject<int>()
        map = (o.["map"] :?> JObject) |> deserializeMap
    }

let deserializeMove (o : JObject) : Move =
    let prop = o.Properties().First()
    let v = prop.Value :?> JObject
    match (prop.Name) with
    | "claim" ->
        Claim {
            punter = v.["punter"].ToObject<int>()
            source = v.["source"].ToObject<int>()
            target = v.["target"].ToObject<int>()
        }
    | "pass" ->
        Pass {
            punter = v.["punter"].ToObject<int>()
        }
    | x -> raise (exn x)

let deserializeMoves (o : JObject) : Moves =
    {
        moves = convertArray o.["moves"] deserializeMove
    }

let deserializeMoveIn (o : JObject) : MoveIn =
    {
        move = (o.["move"] :?> JObject) |> deserializeMoves
    }

let deserializeScore (o : JObject) : Score =
    {
        punter = o.["punter"].ToObject<int>()
        score = o.["score"].ToObject<int>()
    }

let deserializeStop (o : JObject) : Stop =
    {
        moves = convertArray o.["moves"] deserializeMove
        scores = convertArray o.["scores"] deserializeScore
    }

let deserializeStopIn (o : JObject) : StopIn =
    {
        stop = (o.["stop"] :?> JObject) |> deserializeStop
    }

let deserializeTimeoutIn (o : JObject) : TimeoutIn =
    {
        timeout = o.["timeout"].ToObject<int>()
    }

let deserializeMessageIn (o : JObject) : MessageIn =
    let prop = o.Properties().First()
    match (prop.Name) with
    | "you" -> Handshake (deserializeHandshakeIn o)
    | "punter" -> Setup (deserializeSetupIn o)
    | "move" -> Move (deserializeMoveIn o)
    | "stop" -> Stop (deserializeStopIn o)
    | "timeout" -> Timeout (deserializeTimeoutIn o)
    | x -> raise (exn x)

let deserialize (message : string) : MessageIn =
    deserializeMessageIn (JsonConvert.DeserializeObject<JObject>(message))
