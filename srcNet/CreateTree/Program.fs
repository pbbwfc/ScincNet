
open System
open ScincFuncs
open FsChess

type private State = 
    | Unknown
    | InHeader
    | InMove
    | InComment of int
    | InSingleLineComment
    | InRAV of int
    | InNAG
    | InNum
    | InRes
    | FinishedOK
    | Invalid
    | FinishedInvalid

type private GameBrds = {Bds:string list;Mvs:string list}
type private GameInfo = {Gmno:int;Welo:int;Belo:int;Year:int;Result:int}
type private TreeData = {TotElo:int;EloCount:int;TotPerf:int;PerfCount:int;TotYear:int;YearCount:int;TotScore:int;DrawCount:int;TotCount:int}
type private MvGameInfos = Collections.Generic.Dictionary<string,TreeData>
type private BrdMvGameInfos = Collections.Generic.Dictionary<string,MvGameInfos>

[<EntryPoint>]
let main argv =
    
    let nl = System.Environment.NewLine
    
    let GameRdr(lns:string list,ply:int) = 
        let rec proclin st cstr s (imvl:string list) (ibdl:Brd list) = 
            if s = "" then 
                match st with
                |InMove ->
                    let cbd = ibdl.Head
                    let pmv,mv =
                        let bits = s.Trim().Split([|' ';'.'|])
                        let mvtxt = bits.[bits.Length-1].Trim()
                        FsChessPgn.pMove.Parse(mvtxt),mvtxt
                    let nmvl = mv::imvl
                    let amv = pmv|>FsChessPgn.pMove.ToaMove cbd 1
                    let nbdl = amv.PostBrd::ibdl
                    Unknown,"",nmvl,nbdl
                |InNAG ->
                    Unknown,"",imvl,ibdl
                |InSingleLineComment ->
                    Unknown,"",imvl,ibdl
                |InRes ->
                    FinishedOK,"",imvl,ibdl
                |InComment(_) |InRAV(_) -> st, cstr+nl,imvl,ibdl
                |Unknown |InNum -> st, cstr,imvl,ibdl
                |InHeader |Invalid |FinishedOK |FinishedInvalid -> failwith "Invalid state at end of line"
            else 
                let hd = s.[0]
                let tl = s.[1..]
                match st with
                |InComment(cl) -> 
                    if hd='}' && cl=1 then
                           proclin Unknown "" tl imvl ibdl
                    elif hd='}' then
                        proclin (InComment(cl-1)) (cstr+hd.ToString()) tl imvl ibdl
                    elif hd='{' then
                        proclin (InComment(cl+1)) (cstr+hd.ToString()) tl imvl ibdl
                    else
                        proclin st (cstr+hd.ToString()) tl imvl ibdl
                |InSingleLineComment ->
                    proclin st (cstr+hd.ToString()) tl imvl ibdl
                |InRAV(cl) -> 
                    if hd=')' && cl=1 then
                        proclin Unknown "" tl imvl ibdl
                    elif hd=')' then
                        proclin (InRAV(cl-1)) (cstr+hd.ToString()) tl imvl ibdl
                    elif hd='(' then
                        proclin (InRAV(cl+1)) (cstr+hd.ToString()) tl imvl ibdl
                    else
                        proclin st (cstr+hd.ToString()) tl imvl ibdl
                |InNAG -> 
                    if hd=' ' then
                        proclin Unknown "" tl imvl ibdl
                    else
                        proclin st (cstr+hd.ToString()) tl imvl ibdl
                |InNum -> 
                    if System.Char.IsNumber(hd) || hd = '.' || hd = ' ' //&& tl.Length>0 && tl.StartsWith(".")
                    then
                        proclin st (cstr+hd.ToString()) tl imvl ibdl
                    elif hd='/'||hd='-' then
                        proclin InRes (cstr+hd.ToString()) tl imvl ibdl
                    else
                        proclin InMove (cstr+hd.ToString()) tl imvl ibdl
                |InRes -> 
                    proclin st (cstr+hd.ToString()) tl imvl ibdl
                |Invalid -> 
                    proclin st cstr tl imvl ibdl
                |InHeader -> 
                    if hd=']' then
                        //TODO - special processing if tag is game result of FEN or ELO
                        //let ngm = gm|>Game.AddTag cstr
                        proclin Unknown "" tl imvl ibdl
                    else
                        proclin st (cstr+hd.ToString()) tl imvl ibdl
                |InMove -> 
                    if hd=' ' then
                        let cbd = ibdl.Head
                        let pmv,mv =
                            let bits = cstr.Trim().Split([|' ';'.'|])
                            let mvtxt = bits.[bits.Length-1].Trim()
                            FsChessPgn.pMove.Parse(mvtxt),mvtxt
                        let nmvl = mv::imvl
                        let amv = pmv|>FsChessPgn.pMove.ToaMove cbd 1
                        let nbdl = amv.PostBrd::ibdl
                        if nmvl.Length>ply then
                            proclin FinishedOK "" tl nmvl nbdl
                        else proclin Unknown "" tl nmvl nbdl
                    else
                        proclin st (cstr+hd.ToString()) tl imvl ibdl
                |FinishedOK |FinishedInvalid -> st, cstr,imvl,ibdl
                |Unknown -> 
                    let st, ns = 
                        match hd with
                        | '[' -> InHeader, s.[1..]
                        | '{' -> InComment(1), s.[1..]
                        | '(' -> InRAV(1), s.[1..]
                        | '$' -> InNAG, s.[1..]
                        | '*' -> InRes, s
                        | ';' -> InSingleLineComment, s.[1..]
                        | c when System.Char.IsNumber(c) || c = '.' -> InNum, s
                        | ' ' -> Unknown, s.[1..]
                        | _ -> InMove, s
                    proclin st cstr ns imvl ibdl
    
        let rec getgm ilns st cstr imvl ibdl = 
            if List.isEmpty ilns then imvl,ibdl
            else
                let lin = ilns.Head
                let nst, ncstr, nmvl, nbdl = proclin st cstr lin imvl ibdl
                if nst = FinishedOK then nmvl,nbdl
                elif nst = FinishedInvalid then [],[]
                else getgm ilns.Tail nst ncstr nmvl nbdl
    
        let mvl,bdl = getgm lns Unknown "" [] [Board.Start]
        mvl|>List.rev,bdl|>List.rev

    // Open the database:
    //let basename = @"C:\Users\phil\Documents\ScincNet\bases_extra\Caissabase_2020_11_14"
    let basename = @"D:\tmp\SimonWilliams"
    
    if (Base.Open(basename)<>1) then
        printfn "Error opening database %s" basename
    
    if (Base.Isreadonly()) then
        printfn "Error database %s is read only" basename
    
    let basenum = Base.Current()
    let ply = 20
    let numgames = Base.NumGames()

    //load gamebds
    let GetGmBds i =
        ScidGame.Load(uint(i))|>ignore
        let mutable pgn = ""
        ScidGame.Pgn(&pgn)|>ignore

        let lns = pgn.Split('\n')|>List.ofArray
    
        let mvl,bdl = GameRdr(lns,ply)
        let mutable welo = ""
        ScidGame.GetTag("WhiteElo",&welo)|>ignore
        let mutable belo = ""
        ScidGame.GetTag("BlackElo",&belo)|>ignore
        let mutable res = ""
        ScidGame.GetTag("Result",&res)|>ignore
        //{ "*",  "1-0",  "0-1",  "1/2-1/2" }
        let mutable dt = ""
        ScidGame.GetTag("Date",&dt)|>ignore
        let gminfo =
            {
                Gmno = i
                Welo = if welo="" then 0 else int(welo)
                Belo = if belo="" then 0 else int(belo)
                Year = if dt.Length>3 then int(dt.[..3]) else 0
                Result = if res="1" then 2 elif res="2" then 0 else 1
            }
        let gmbds =
            {
                Bds = bdl.[..ply-1]|>List.map(fun b -> b|>Board.ToSimpleStr)
                Mvs = mvl.[..ply-1]
            }
        gminfo,gmbds

    let totaldict = new BrdMvGameInfos()
    for i = 1 to 3 do
        let gminfo,gmbds = GetGmBds i
        
        let wtd = 
            let perf,ct =
                if gminfo.Belo=0 then 0,0
                elif gminfo.Result=1 then //draw
                    gminfo.Belo,1
                elif gminfo.Result=2 then //win
                    gminfo.Belo+
            
            {TotElo = gminfo.Welo;EloCount = if gminfo.Welo=0 then 0 else 1;TotPerf = if gminfo.Result=1 && gminfo}
        //;TotPerf:int;PerfCount:int;TotYear:int;YearCount:int;TotScore:int;DrawCount:int;TotCount:int}
        //let btd = 
        //now need to go through the boarda and put in dictionary holding list of gminfos
        //for j = 0 to gmbds.Bds.Length-1 do
        //    let bd = gmbds.Bds.[j]
        //    let mv = gmbds.Mvs.[j]
        //    if totaldict.ContainsKey(bd) then
        //        let mvdct:MvGameInfos = totaldict.[bd]
        //        if mvdct.ContainsKey(mv) then
        //            mvdct.[mv]<-gminfo::mvdct.[mv]
        //        else 
        //            mvdct.[mv]<-[gminfo]
        //    else
        //        let mvdct = new MvGameInfos()
        //        mvdct.[mv]<-[gminfo]
        //        totaldict.[bd]<-mvdct
        ()
    
    
    
    0 // return an integer exit code
