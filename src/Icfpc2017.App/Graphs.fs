module Graphs

open System.Collections.Generic

type Color = int

module Vertex = 
    type T = private {
        Id: int
        IsSource: bool
        Coords: (float * float) option
    }

    let create id isSource coords: T = {Id=id; IsSource=isSource; Coords=coords}

    let id {Id=id} = id
    let isSource {IsSource=isSource} = isSource
    let coords {Coords=coords} = coords

module Edge =
    type T = private {
        id: int
        uv: int * int
    }

    (* Enforces an invariant that the first vertex ID is smaller. *)
    let create id uv = { id=id; uv=uv }

    let id { id=id } = id
    let ends { uv=uv } = uv

    let opposite { uv=(u, v) } w =
        if w = u
        then v
        else
            assert (w = v)
            u

    let contains { uv=(u, v) } w = u = w || v = w

(**
 * The Graph.
 *)
module Graph =
    type T = private {
        Vertices: Vertex.T array
        Sources: int array
        Edges: Edge.T array
        Colors: Map<int, Color>
    }

    let create nVertices sources uvs: T =
        let vertices =
            {0..nVertices - 1}
            |> Seq.map (fun vid -> Vertex.create vid (Array.contains vid sources) None)
            |> Seq.toArray

        {Vertices=vertices;
         Sources=sources;
         Edges=Array.mapi Edge.create uvs;
         Colors=Map.empty}

    let vertices {Vertices=vertices} = vertices
    let sources {Sources=sources} = sources
    let edges {Edges=es} = es

    let nVertices = vertices >> Array.length
    let nEdges = edges >> Array.length

    let withEdges (graph: T) es =
        { graph with Edges=es }

    (** Focus on a subgraph of a specific color. *)
    let subgraph (g : T) (color: Color): T =
        (* TODO: ideally just filter in [[adjacent]]. *)
        let subColors = Map.filter (fun _ -> (=) color) g.Colors
        let subEdges =
            g.Edges
            |> Array.filter (fun edge -> Map.containsKey (Edge.id edge) subColors)
        {g with Edges=subEdges; Colors=subColors}

    let adjacent {Edges=es} vid =
        Array.toSeq es
        |> Seq.filter (fun e -> Edge.contains e vid)
        |> Seq.map (fun e -> Edge.opposite e vid)

    let adjacentEdges {Edges=es} vid =
        es
        |> Array.toSeq
        |> Seq.filter (fun e -> Edge.contains e vid)

    let unclaimed {Edges=es; Colors=colors}: Edge.T seq = 
        Array.toSeq es
        |> Seq.filter (fun e -> not (Map.containsKey (Edge.id e) colors))

    let claimEdge ({Colors=cs} as g) punter eid: T =
        {g with Colors=Map.add eid punter cs}

    let isClaimed {Colors=cs} edge: bool =
        cs.ContainsKey (Edge.id edge)

    let isClaimedBy punter {Colors=cs} edge: bool =
        match cs.TryFind (Edge.id edge) with
        | Some color -> color = punter
        | None -> false

    let fromOriginalEnds (graph: T) (u, v): Edge.T = 
        let iu = Array.findIndex (fun x -> Vertex.id x = u) graph.Vertices
        let iv = Array.findIndex (fun x -> Vertex.id x = v) graph.Vertices
        let (iu, iv) = (min iu iv, max iu iv)
        Array.find (fun e -> Edge.ends e = (iu, iv)) graph.Edges

    let originalEnds (graph: T) (e: Edge.T) = 
        let (iu, iv) = Edge.ends e
        (Vertex.id graph.Vertices.[iu], Vertex.id graph.Vertices.[iv])

module Traversal =
    (** Computes the shortest paths from [source] to all other vertices. *)
    let shortestPath (graph: Graph.T) (source: int): int array =
        let distances = Array.zeroCreate (Graph.nVertices graph) in
        let seen = Array.create (Graph.nVertices graph) false in
        let q = new Queue<int>() in
        distances.[source] <- 0
        q.Enqueue source
        while q.Count <> 0 do
            let current = q.Dequeue () in
            seen.[current] <- true
            for next in Graph.adjacent graph current do
                if not seen.[next]
                then
                    distances.[next] <- distances.[current] + 1
                    q.Enqueue next

        distances

    let shortestPaths (graph: Graph.T): Map<int, int array> =
        Graph.sources graph
        |> Array.map (fun v -> (v, shortestPath graph v))
        |> Map.ofArray
