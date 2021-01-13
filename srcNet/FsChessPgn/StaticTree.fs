namespace FsChessPgn


open System
open ScincFuncs
open System.Linq
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
type private MvTrees = Collections.Generic.Dictionary<string,TreeData>
type private BrdMvGameInfos = Collections.Generic.Dictionary<string,MvTrees>
type private BrdStats = Collections.Generic.Dictionary<string,ScincFuncs.stats>

module StaticTree =
    let nl = System.Environment.NewLine

    let mutable ply = 20
    let mutable private totaldict = BrdMvGameInfos()
    let mutable private stsdict = BrdStats()
    
    let private GameRdr(lns:string list,ply:int) = 
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

    //load gamebds
    let private GetGmBds i =
        ScidGame.Load(uint(i))|>ignore
        let mutable pgn = ""
        ScidGame.Pgn(&pgn)|>ignore

        let lns = pgn.Split('\n')|>List.ofArray
    
        let mvl,bdl = GameRdr(lns,ply)
        let nply = mvl.Length-1
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
                Bds = bdl.[..nply]|>List.map(fun b -> b|>Board.ToSimpleStr)
                Mvs = mvl.[..nply]
            }
        gminfo,gmbds

    let Init() =
        totaldict <- new BrdMvGameInfos()
        stsdict <- new BrdStats()

    let ProcessGame i =
        let gminfo,gmbds = GetGmBds i
    
        let wtd = 
            let perf,ct =
                if gminfo.Belo=0 then 0,0
                elif gminfo.Result=1 then //draw
                    gminfo.Belo,1
                elif gminfo.Result=2 then //win
                    (gminfo.Belo + 400),1
                else (gminfo.Belo - 400),1
            {TotElo = gminfo.Welo;EloCount = (if gminfo.Welo=0 then 0 else 1);TotPerf = perf;
             PerfCount = ct;TotYear = gminfo.Year;YearCount = (if gminfo.Year=0 then 0 else 1);
             TotScore = gminfo.Result; DrawCount = (if gminfo.Result=1 then 1 else 0); TotCount=1}
        let btd = 
            let perf,ct =
                 if gminfo.Welo=0 then 0,0
                 elif gminfo.Result=1 then //draw
                     gminfo.Welo,1
                 elif gminfo.Result=0 then //win
                     (gminfo.Welo + 400),1
                 else (gminfo.Welo - 400),1
            {TotElo = gminfo.Belo;EloCount = (if gminfo.Belo=0 then 0 else 1);TotPerf = perf;
             PerfCount = ct;TotYear = gminfo.Year;YearCount = (if gminfo.Year=0 then 0 else 1);
             TotScore = gminfo.Result; DrawCount = (if gminfo.Result=1 then 1 else 0); TotCount=1}
        //now need to go through the boarda and put in dictionary holding running totals
        for j = 0 to gmbds.Bds.Length-1 do
            let bd = gmbds.Bds.[j]
            let mv = gmbds.Mvs.[j]
            let isw = bd.EndsWith("w")
            if totaldict.ContainsKey(bd) then
                let mvdct:MvTrees = totaldict.[bd]
                if mvdct.ContainsKey(mv) then
                    let cmt =  mvdct.[mv]
                    let nmt = if isw then wtd else btd
                    mvdct.[mv]<-
                        {TotElo = cmt.TotElo+nmt.TotElo;EloCount = cmt.EloCount+nmt.EloCount;
                         TotPerf = cmt.TotPerf+nmt.TotPerf;PerfCount = cmt.PerfCount+nmt.PerfCount;
                         TotYear = cmt.TotYear+nmt.TotYear;YearCount = cmt.YearCount+nmt.YearCount;
                         TotScore = cmt.TotScore+nmt.TotScore; DrawCount = cmt.DrawCount+nmt.DrawCount; 
                         TotCount=cmt.TotCount+nmt.TotCount}
                else 
                    mvdct.[mv]<-if isw then wtd else btd
            else
                let mvdct = new MvTrees()
                mvdct.[mv]<-if isw then wtd else btd
                totaldict.[bd]<-mvdct

    let NumPos() = totaldict.Count

    let ProcessPos i =
        let pair = totaldict.ElementAt(i-1)
        let key = pair.Key
        let vl = pair.Value
        let sts = new stats();
        let mvsts = new ResizeArray<ScincFuncs.mvstats>()
        let totsts = new ScincFuncs.totstats()
        totsts.TotFreq<-1.0
        for mtr in vl do
            let tr = mtr.Value
            totsts.TotCount<-totsts.TotCount+tr.TotCount

        let mutable ect = 0
        let mutable pct = 0
        let mutable yct = 0
        for mtr in vl do
            let mv = mtr.Key
            let tr = mtr.Value
            let mvst = new ScincFuncs.mvstats()
            mvst.Count<-tr.TotCount
            mvst.Freq<-float(mvst.Count)/float(totsts.TotCount)
            mvst.WhiteWins<-(tr.TotScore-tr.DrawCount)/2
            totsts.TotWhiteWins<-totsts.TotWhiteWins+mvst.WhiteWins
            mvst.Draws<-tr.DrawCount
            totsts.TotDraws<-totsts.TotDraws+mvst.Draws
            mvst.BlackWins<-mvst.Count-mvst.WhiteWins-mvst.Draws
            totsts.TotBlackWins<-totsts.TotBlackWins+mvst.BlackWins
            mvst.Score<-float(tr.TotScore)/float(tr.TotCount*2)
            totsts.TotScore<-totsts.TotScore+float(tr.TotScore)/float(totsts.TotCount*2)
            mvst.DrawPc<-float(tr.DrawCount)/float(tr.TotCount)
            totsts.TotDrawPc<-totsts.TotDrawPc+float(tr.DrawCount)/float(totsts.TotCount)
            mvst.AvElo<-if tr.EloCount<=10 then 0 else tr.TotElo/tr.EloCount
            totsts.TotAvElo<-totsts.TotAvElo+tr.TotElo
            ect<-ect+tr.EloCount
            mvst.Perf<-if tr.PerfCount<=10 then 0 else tr.TotPerf/tr.PerfCount
            totsts.TotPerf<-totsts.TotPerf+tr.TotPerf
            pct<-pct+tr.PerfCount
            mvst.AvYear<-if tr.YearCount<=0 then 0 else tr.TotYear/tr.YearCount
            totsts.TotAvYear<-totsts.TotAvYear+tr.TotYear
            yct<-yct+tr.YearCount
            mvst.Mvstr<-mv
            mvsts.Add(mvst)

        totsts.TotAvElo<-if ect=0 then 0 else totsts.TotAvElo/ect
        totsts.TotPerf<-if pct=0 then 0 else totsts.TotPerf/pct
        totsts.TotAvYear<-if yct=0 then 0 else totsts.TotAvYear/yct
        //need to sort by count
        mvsts.Sort(fun a b -> b.Count-a.Count)
        sts.MvsStats<-mvsts
        sts.TotStats<-totsts
        stsdict.[key]<-sts

    let GetStats(posstr:string) = stsdict.[posstr]
