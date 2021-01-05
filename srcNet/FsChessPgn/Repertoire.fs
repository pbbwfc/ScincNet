﻿namespace FsChessPgn

open FsChess
open System.IO
open FSharp.Json

module Repertoire =

    let mutable rfol = 
        let def = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),"ScincNet\\repertoire")
        Directory.CreateDirectory(def)|>ignore
        def

    let mutable BlackRep:RepOpts*RepMove = Map.empty,Map.empty
    let mutable BlackErrors:string list = []
    let mutable WhiteRep:RepOpts*RepMove = Map.empty,Map.empty
    let mutable WhiteErrors:string list = []
    
    let setfol fol = rfol <- fol
    let whitedb = Path.Combine(rfol,"WhiteRep")
    let whiterep = Path.Combine(rfol,"whte.json")
    let whiteerrs = Path.Combine(rfol,"whteerrs.txt")
    let blackdb = Path.Combine(rfol,"BlackRep")
    let blackrep = Path.Combine(rfol,"blck.json")
    let blackerrs = Path.Combine(rfol,"blckerrs.txt")
    
    
    let LoadWhite() =
        if File.Exists(whiterep) then 
            let str = File.ReadAllText(whiterep)  
            WhiteRep <- Json.deserialize (str)
            WhiteErrors <- File.ReadAllLines(whiteerrs)|>List.ofArray

    let LoadBlack() =
        if File.Exists(blackrep) then 
            let str = File.ReadAllText(blackrep)  
            BlackRep <- Json.deserialize (str)
            BlackErrors <- File.ReadAllLines(blackerrs)|>List.ofArray

    let savewhite() =
        let str = Json.serialize WhiteRep
        File.WriteAllText(whiterep, str)
        File.WriteAllLines(whiteerrs,WhiteErrors|>List.toArray)
    
    let saveblack() =
        let str = Json.serialize BlackRep
        File.WriteAllText(blackrep, str)
        File.WriteAllLines(blackerrs,BlackErrors|>List.toArray)
    
    let optsHasSan (san:string) (opts:RepOpt list) =
        let filt = opts|>List.filter(fun op -> op.San=san)
        filt.Length>0
    
    let UpdateBlack () =
        let rec domvt camv (imtel:MoveTextEntry list) (repopts:RepOpts) (repmove:RepMove) = 
            if List.isEmpty imtel then repopts,repmove
            else
                let mte = imtel.Head
                match mte with
                |HalfMoveEntry(_,_,pmv,amvo) -> 
                    let amv = amvo.Value
                    let fen = amv.PreBrd|>Board.ToStr
                    let isw = amv.Isw
                    let san = pmv|>PgnWrite.MoveStr
                    let cro = {San = san; Nag = NAG.Null; Comm = ""}
                    //doing opts
                    if isw then
                        let nrepopts = 
                            if repopts.ContainsKey(fen) then
                                let curopts = repopts.[fen]
                                if not (optsHasSan san curopts) then
                                    repopts.Add(fen,(cro::curopts))
                                else repopts
                            else
                                repopts.Add(fen,[cro])
                        domvt amvo imtel.Tail nrepopts repmove
                    //doing move
                    else
                        let nrepmove =
                            if repmove.ContainsKey(fen) then
                                repmove//need to log an error
                            else
                                repmove.Add(fen,cro)
                        domvt camv imtel.Tail repopts nrepmove
                |RAVEntry(mtel) -> 
                    let nrepopts,nrepmove = domvt camv mtel repopts repmove
                    domvt camv imtel.Tail nrepopts nrepmove
                |NAGEntry(ng) -> 
                    if camv.IsSome then
                        let amv = camv.Value
                        let fen = amv.PreBrd|>Board.ToStr
                        let san = MoveUtil.toPgn amv.PreBrd amv.Mv
                        let isw = amv.Isw
                        if isw then
                            let curopts = repopts.[fen]
                            let newopts = curopts|>List.map(fun ro -> if ro.San=san then {ro with Nag=ng} else ro)
                            let nrepopts = repopts.Add(fen,newopts)
                            domvt camv imtel.Tail nrepopts repmove
                        else
                            let cro = repmove.[fen]
                            let nro = {cro with Nag=ng}
                            let nrepmove = repmove.Add(fen,nro)
                            domvt camv imtel.Tail repopts nrepmove
                    else domvt camv imtel.Tail repopts repmove
                |CommentEntry(cm) -> 
                    if camv.IsSome then
                        let amv = camv.Value
                        let fen = amv.PreBrd|>Board.ToStr
                        let san = MoveUtil.toPgn amv.PreBrd amv.Mv
                        let isw = amv.Isw
                        if isw then
                            let curopts = repopts.[fen]
                            let newopts = curopts|>List.map(fun ro -> if ro.San=san then {ro with Comm=cm} else ro)
                            let nrepopts = repopts.Add(fen,newopts)
                            domvt camv imtel.Tail nrepopts repmove
                        else
                            let cro = repmove.[fen]
                            let nro = {cro with Comm=cm}
                            let nrepmove = repmove.Add(fen,nro)
                            domvt camv imtel.Tail repopts nrepmove
                    else domvt camv imtel.Tail repopts repmove
                |_ -> domvt camv imtel.Tail repopts repmove
        ScincFuncs.Base.Open(blackdb)|>ignore
        let numgames = ScincFuncs.Base.NumGames()
        BlackRep <- Map.empty,Map.empty
        BlackErrors <- []
        for i = 1 to numgames do
            ScincFuncs.ScidGame.Load(uint(i))|>ignore
            let mutable pgnstr = ""
            ScincFuncs.ScidGame.Pgn(&pgnstr)|>ignore
            let gm = RegParse.GameFromString(pgnstr)
            let mvs = (gm|>Game.SetaMoves).MoveText
            let repopts,repmove = BlackRep
            BlackRep <- domvt None mvs repopts repmove
        saveblack()
        ScincFuncs.Base.Close()|>ignore

