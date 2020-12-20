namespace FsChess.WinForms
open System.Text.RegularExpressions
open System.IO
open System.Text

[<AutoOpen>]
module Util =
    let CasFlg i = enum<CstlFlgs> (i)
    let PcTp i = enum<PieceType> (i)
    let Pc i = enum<Piece> (i)
    let Plyr i = enum<Player> (i)
    let BitB i =  Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue<uint64,Bitboard> (i)
    let Ng i = enum<NAG> (i)

module Rank = 
    let Parse(c : char) :Rank = 
        let Rankdesclookup = RANK_NAMES|>List.reduce(+)
        let idx = Rankdesclookup.IndexOf(c.ToString().ToLower())
        if idx < 0 then failwith (c.ToString() + " is not a valid rank")
        else int16(idx) 

module File = 
    let Parse(c : char) = 
        let Filedesclookup = FILE_NAMES|>List.reduce(+)
        let idx = Filedesclookup.IndexOf(c.ToString().ToLower())
        if idx < 0 then failwith (c.ToString() + " is not a valid file")
        else int16(idx)

module Square = 
    let Parse(s : string) = 
        if s.Length <> 2 then failwith (s + " is not a valid position")
        else 
            let file = File.Parse(s.[0])
            let rank = Rank.Parse(s.[1])
            Sq(file,rank)

module GameResult = 
    
    let Parse(s : string) = 
         if s="1-0" then GameResult.WhiteWins
         elif s="0-1" then GameResult.BlackWins
         elif s="1/2-1/2" then GameResult.Draw
         else GameResult.Open
    
    let ToStr(result:GameResult) =
        match result with
        |GameResult.WhiteWins -> "1-0" 
        |GameResult.BlackWins -> "0-1" 
        |GameResult.Draw -> "1/2-1/2" 
        |_ -> "*" 

    let ToUnicode(result:GameResult) =
        match result with
        |GameResult.WhiteWins -> "1-0" 
        |GameResult.BlackWins -> "0-1" 
        |GameResult.Draw -> "½-½" 
        |_ -> "*" 

module DateUtil = 
    let (|?) (lhs:int option) rhs = (if lhs.IsNone then rhs else lhs.Value.ToString("00"))
    let (-?) (lhs:int option) rhs = (if lhs.IsNone then rhs else lhs.Value.ToString("0000"))
    
    let ToStr(gm:Game) =
        (gm.Year -? "????") + (".") +
        (gm.Month |? "??") + (".") +
        (gm.Day |? "??")

    let FromStr(dtstr:string) =
        let a = dtstr.Split([|'.'|])|>Array.map(fun s -> s.Trim())
        let y,m,d = if a.Length=3 then a.[0],a.[1],a.[2] else a.[0],"??","??"
        let yop = if y="????" then None else Some(int y)
        let mop = if m="??" then None else Some(int m)
        let dop = if d="??" then None else Some(int d)
        yop,mop,dop

module NagUtil = 
    let All = [0..6]@[10]@[14..19]|>List.map Ng

    let ToStr(nag:NAG) =
         match nag with
         | NAG.Good -> "!"
         | NAG.Poor -> "?"
         | NAG.VeryGood -> "!!"
         | NAG.VeryPoor -> "??"
         | NAG.Speculative -> "!?"
         | NAG.Questionable -> "?!"
         | NAG.Even -> "="
         | NAG.Wslight -> "⩲"
         | NAG.Bslight -> "⩱"
         | NAG.Wmoderate -> "±"
         | NAG.Bmoderate -> "∓"
         | NAG.Wdecisive -> "+−"
         | NAG.Bdecisive -> "−+"
         |_ -> ""

    let FromStr(str:string) =
         let stra = All|>List.map ToStr|>List.toArray
         let indx = stra|>Array.findIndex(fun s -> s=str)
         All.[indx]

    let ToHtm(nag:NAG) =
         match nag with
         | NAG.Good -> "&#33;"
         | NAG.Poor -> "&#63;"
         | NAG.VeryGood -> "&#33;&#33;"
         | NAG.VeryPoor -> "&#63;&#63;"
         | NAG.Speculative -> "&#33;&#63;"
         | NAG.Questionable -> "&#63;&#33;"
         | NAG.Even -> "&#61;"
         | NAG.Wslight -> "&#10866;"
         | NAG.Bslight -> "&#10865;"
         | NAG.Wmoderate -> "&#0177;"
         | NAG.Bmoderate -> "&#8723;"
         | NAG.Wdecisive -> "&#43;&minus;"
         | NAG.Bdecisive -> "&minus;&#43;"
         |_ -> ""

    let Desc(nag:NAG) =
         match nag with
         | NAG.Good -> "Good"
         | NAG.Poor -> "Poor"
         | NAG.VeryGood -> "Very Good"
         | NAG.VeryPoor -> "Very Poor"
         | NAG.Speculative -> "Speculative"
         | NAG.Questionable -> "Questionable"
         | NAG.Even -> "Even"
         | NAG.Wslight -> "W slight adv" 
         | NAG.Bslight -> "B slight adv"
         | NAG.Wmoderate -> "W mod adv"
         | NAG.Bmoderate -> "B mod adv"
         | NAG.Wdecisive -> "W dec adv"
         | NAG.Bdecisive -> "B dec adv"
         |_ -> "None"

module PieceType = 
    let Parse(c : char) = 
        match c with
        | 'P' -> PieceType.Pawn
        | 'N' -> PieceType.Knight
        | 'B' -> PieceType.Bishop
        | 'R' -> PieceType.Rook
        | 'Q' -> PieceType.Queen
        | 'K' -> PieceType.King
        | 'p' -> PieceType.Pawn
        | 'n' -> PieceType.Knight
        | 'b' -> PieceType.Bishop
        | 'r' -> PieceType.Rook
        | 'q' -> PieceType.Queen
        | 'k' -> PieceType.King
        | _ -> failwith (c.ToString() + " is not a valid piece")

module PgnWrite =

    let ResultString = GameResult.ToStr

    let Piece(pieceType: PieceType option) =
        if pieceType.IsNone then ""
        else 
            match pieceType.Value with
            |PieceType.Pawn -> ""
            |PieceType.Knight -> "N"
            |PieceType.Bishop -> "B"
            |PieceType.Rook -> "R"
            |PieceType.Queen -> "Q"
            |PieceType.King -> "K"
            |_ -> ""
            
    let MoveTarget(move:pMove) =
        if move.TargetSquare <> OUTOFBOUNDS then
            SQUARE_NAMES.[int(move.TargetSquare)]
        else ""

    let MoveOrigin(move:pMove) =
        let piece = Piece(move.Piece)
        let origf = if move.OriginFile.IsSome then FILE_NAMES.[int(move.OriginFile.Value)] else ""
        let origr = if move.OriginRank.IsSome then RANK_NAMES.[int(move.OriginRank.Value)] else ""
        piece + origf + origr    
    
    let CheckAndMateAnnotation(move:pMove) =
        if move.IsCheckMate then "#"
        elif move.IsDoubleCheck then "++"
        elif move.IsCheck then "+"
        else ""

    let Move(mv:pMove, writer:TextWriter) =
        match mv.Mtype with
        | Simple -> 
            let origin = MoveOrigin(mv)
            let target = MoveTarget(mv)
            writer.Write(origin)
            writer.Write(target)
            if mv.PromotedPiece.IsSome then
                writer.Write("=")
                writer.Write(Piece(mv.PromotedPiece))
            writer.Write(CheckAndMateAnnotation(mv))
        | Capture -> 
            let origin = MoveOrigin(mv)
            let target = MoveTarget(mv)
            writer.Write(origin)
            writer.Write("x")
            writer.Write(target)
            if mv.PromotedPiece.IsSome then
                writer.Write("=")
                writer.Write(Piece(mv.PromotedPiece))
            writer.Write(CheckAndMateAnnotation(mv))
        | CastleKingSide -> 
            writer.Write("O-O")
            writer.Write(CheckAndMateAnnotation(mv))
        | CastleQueenSide ->
            writer.Write("O-O-O")
            writer.Write(CheckAndMateAnnotation(mv))

    let MoveStr(mv:pMove) =
        let writer = new StringWriter()
        Move(mv,writer)
        writer.ToString()

    let rec MoveTextEntry(entry:MoveTextEntry, writer:TextWriter) =
        match entry with
        |HalfMoveEntry(mn,ic,mv) -> 
            if mn.IsSome then
                writer.Write(mn.Value)
                writer.Write(if ic then "... " else ". ")
            Move(mv, writer)
            writer.Write(" ")
        |CommentEntry(str) -> 
            writer.WriteLine()
            writer.Write("{" + str + "} ")
        |GameEndEntry(gr) -> writer.Write(ResultString(gr))
        |NAGEntry(cd) -> 
            writer.Write("$" + (cd|>int).ToString())
            writer.Write(" ")
        |RAVEntry(ml) -> 
            writer.WriteLine()
            writer.Write("(")
            MoveText(ml, writer)
            writer.WriteLine(")")
    
    and MoveText(ml:MoveTextEntry list, writer:TextWriter) =
        let doent i m =
            MoveTextEntry(m,writer)
            //if i<ml.Length-1 then writer.Write(" ")

        ml|>List.iteri doent
    
    let MoveTextEntryStr(entry:MoveTextEntry) =
        let writer = new StringWriter()
        MoveTextEntry(entry,writer)
        writer.ToString()

    let MoveTextStr(ml:MoveTextEntry list) =
        let writer = new StringWriter()
        MoveText(ml,writer)
        writer.ToString()

    let Tag(name:string, value:string, writer:TextWriter) =
        writer.Write("[")
        writer.Write(name + " \"")
        writer.Write(value)
        writer.WriteLine("\"]")

    let Game(game:Game, writer:TextWriter) =
        Tag("Event", game.Event, writer)
        Tag("Site", game.Site, writer)
        Tag("Date", game|>DateUtil.ToStr, writer)
        Tag("Round", game.Round, writer)
        Tag("White", game.WhitePlayer, writer)
        Tag("Black", game.BlackPlayer, writer)
        Tag("Result", ResultString(game.Result), writer)
        Tag("WhiteElo", game.WhiteElo, writer)
        Tag("BlackElo", game.BlackElo, writer)

        for info in game.AdditionalInfo do
            Tag(info.Key, info.Value, writer)

        writer.WriteLine();
        MoveText(game.MoveText, writer)
        writer.WriteLine();

    let GameStr(game:Game) =
        let writer = new StringWriter()
        Game(game,writer)
        writer.ToString()

module pMove =

    let CreateAll(mt,tgs,pc,orf,orr,pp,ic,id,im) =
        {Mtype=mt 
         TargetSquare=tgs
         Piece=pc
         OriginFile=orf
         OriginRank=orr
         PromotedPiece=pp
         IsCheck=ic
         IsDoubleCheck=id
         IsCheckMate=im}

    let CreateOrig(mt,tgs,pc,orf,orr) = CreateAll(mt,tgs,pc,orf,orr,None,false,false,false)

    let Create(mt,tgs,pc) = CreateOrig(mt,tgs,pc,None,None)

    let CreateCastle(mt) = CreateOrig(mt,OUTOFBOUNDS,None,None,None)
    
    let Parse(s : string) =
        //Active pattern to parse move string
        let (|SimpleMove|Castle|PawnCapture|AmbiguousFile|AmbiguousRank|Promotion|PromCapture|) s =
            if Regex.IsMatch(s, "^[BNRQK][a-h][1-8]$") then 
                SimpleMove(s.[0]|>PieceType.Parse, s.[1..]|>Square.Parse)
            elif Regex.IsMatch(s, "^[a-h][1-8]$") then SimpleMove(PieceType.Pawn, s|>Square.Parse)
            elif s = "O-O" then Castle('K')
            elif s = "O-O-O" then Castle('Q')
            elif Regex.IsMatch(s, "^[a-h][a-h][1-8]$") then 
                PawnCapture(s.[0]|>File.Parse, s.[1..]|>Square.Parse)
            elif Regex.IsMatch(s, "^[BNRQK][a-h][a-h][1-8]$") then 
                AmbiguousFile(s.[0]|>PieceType.Parse, s.[1]|>File.Parse, s.[2..]|>Square.Parse)
            elif Regex.IsMatch(s, "^[BNRQK][1-8][a-h][1-8]$") then 
                AmbiguousRank(s.[0]|>PieceType.Parse, s.[1]|>Rank.Parse, s.[2..]|>Square.Parse)
            elif Regex.IsMatch(s, "^[a-h][1-8][BNRQ]$") then 
                Promotion(s.[0..1]|>Square.Parse, s.[2]|>PieceType.Parse)
            elif Regex.IsMatch(s, "^[a-h][a-h][1-8][BNRQ]$") then 
                PromCapture(s.[0]|>File.Parse, s.[1..2]|>Square.Parse, s.[3]|>PieceType.Parse)
            else failwith ("invalid move: " + s)

        //general failure message
        let fl() =
            failwith ("not done yet, mv: " + s)

        let strip chars =
            String.collect (fun c -> 
                if Seq.exists ((=) c) chars then ""
                else string c)
          
        let m = s |> strip "+x#="|>fun x ->x.Replace("e.p.", "")
        
        let mv0 =
            match m with
            | SimpleMove(p, sq) -> 
                Create((if s.Contains("x") then MoveType.Capture else MoveType.Simple),sq,Some(p))
            | Castle(c) ->
                CreateCastle(if c='K' then MoveType.CastleKingSide else MoveType.CastleQueenSide)
            | PawnCapture(f, sq) -> 
                CreateOrig(MoveType.Capture,sq,Some(PieceType.Pawn),Some(f),None)
            | AmbiguousFile(p, f, sq) -> 
                CreateOrig((if s.Contains("x") then MoveType.Capture else MoveType.Simple),sq,Some(p),Some(f),None)
            | AmbiguousRank(p, r, sq) -> 
                CreateOrig((if s.Contains("x") then MoveType.Capture else MoveType.Simple),sq,Some(p),None,Some(r))
            | Promotion(sq, p) -> 
                CreateAll(MoveType.Simple,sq,Some(PieceType.Pawn),None,None,Some(p),false,false,false)
            | PromCapture(f, sq, p) -> 
                CreateAll(MoveType.Capture,sq,Some(PieceType.Pawn),Some(f),None,Some(p),false,false,false)
      
        let mv1 =
            if s.Contains("++") then {mv0 with IsDoubleCheck=true} 
            elif s.Contains("+") then {mv0 with IsCheck=true}
            elif s.Contains("#") then {mv0 with IsCheckMate=true}
            else mv0
        
        mv1

module MoveTextEntry =

    let Parse(s : string) =
        let mn =
            if System.Char.IsNumber(s.[0]) then
                let bits = s.Split([|'.'|])
                bits.[0]|>int|>Some
            else None
        
        let ic = s.Contains("...") 

        let mv =
            let bits = s.Trim().Split([|' ';'.'|])
            let mvtxt = bits.[bits.Length-1].Trim()
            pMove.Parse(mvtxt)

        HalfMoveEntry(mn,ic,mv)
    
module RegParse = 
    let AddTag (tagstr:string) (gm:Game) =
        let k,v = tagstr.Trim().Split([|'"'|])|>Array.map(fun s -> s.Trim())|>fun a -> a.[0],a.[1].Trim('"')
        match k with
        | "Event" -> {gm with Event = v}
        | "Site" -> {gm with Site = v}
        | "Date" -> 
            let yop,mop,dop = v|>DateUtil.FromStr
            {gm with Year = yop; Month = mop; Day = dop}
        | "Round" -> {gm with Round = v}
        | "White" -> {gm with WhitePlayer = v}
        | "Black" -> {gm with BlackPlayer = v}
        | "Result" -> {gm with Result = v|>GameResult.Parse}
        | "WhiteElo" -> {gm with WhiteElo = v}
        | "BlackElo" -> {gm with BlackElo = v}
        | "FEN" -> {gm with BoardSetup = v}
        | _ ->
            {gm with AdditionalInfo=gm.AdditionalInfo.Add(k,v)}
    
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
    
    let rec private NextGameRdr(sr : StreamReader) = 
        let nl = System.Environment.NewLine
        let rec proclin st cstr s gm = 
            if s = "" then 
                match st with
                |InMove ->
                    let mte = MoveTextEntry.Parse(cstr)
                    let ngm = {gm with MoveText=mte::gm.MoveText}
                    Unknown,"",ngm
                |InNAG ->
                    let mte = NAGEntry(cstr|>int|>Ng)
                    let ngm = {gm with MoveText=mte::gm.MoveText}
                    Unknown,"",ngm
                |InSingleLineComment ->
                    let mte = CommentEntry(cstr)
                    let ngm = {gm with MoveText=mte::gm.MoveText}
                    Unknown,"",ngm
                |InRes ->
                    let bits = cstr.Split([|'{'|])
                    let ngm =
                        if bits.Length=1 then
                            let mte = GameEndEntry(cstr|>GameResult.Parse)
                            {gm with MoveText=mte::gm.MoveText}
                        else
                            let mte = GameEndEntry(bits.[0].Trim()|>GameResult.Parse)
                            let gm1 = {gm with MoveText=mte::gm.MoveText}
                            let mte1 = CommentEntry(bits.[1].Trim([|'}'|]))
                            {gm1 with MoveText=mte1::gm1.MoveText}
                    FinishedOK,"",ngm
                |InComment(_) |InRAV(_) -> st, cstr+nl, gm
                |Unknown |InNum -> st, cstr, gm
                |InHeader |Invalid |FinishedOK |FinishedInvalid -> failwith "Invalid state at end of line"
            else 
                let hd = s.[0]
                let tl = s.[1..]
                match st with
                |InComment(cl) -> 
                    if hd='}' && cl=1 then
                        let mte = CommentEntry(cstr)
                        let ngm = {gm with MoveText=mte::gm.MoveText}
                        proclin Unknown "" tl ngm
                    elif hd='}' then
                        proclin (InComment(cl-1)) (cstr+hd.ToString()) tl gm
                    elif hd='{' then
                        proclin (InComment(cl+1)) (cstr+hd.ToString()) tl gm
                    else
                        proclin st (cstr+hd.ToString()) tl gm
                |InSingleLineComment ->
                    proclin st (cstr+hd.ToString()) tl gm
                |InRAV(cl) -> 
                    if hd=')' && cl=1 then
                        let byteArray = Encoding.ASCII.GetBytes(cstr)
                        let stream = new MemoryStream(byteArray)
                        let nsr = new StreamReader(stream)
                        let gmr = NextGameRdr(nsr)
                        let mte = RAVEntry(gmr.MoveText)
                        let ngm = {gm with MoveText=mte::gm.MoveText}
                        proclin Unknown "" tl ngm
                    elif hd=')' then
                        proclin (InRAV(cl-1)) (cstr+hd.ToString()) tl gm
                    elif hd='(' then
                        proclin (InRAV(cl+1)) (cstr+hd.ToString()) tl gm
                    else
                        proclin st (cstr+hd.ToString()) tl gm
                |InNAG -> 
                    if hd=' ' then
                        let mte = NAGEntry(cstr|>int|>Ng)
                        let ngm = {gm with MoveText=mte::gm.MoveText}
                        proclin Unknown "" tl ngm
                    else
                        proclin st (cstr+hd.ToString()) tl gm
                |InNum -> 
                    if System.Char.IsNumber(hd) || hd = '.' || hd = ' ' //&& tl.Length>0 && tl.StartsWith(".")
                    then
                        proclin st (cstr+hd.ToString()) tl gm
                    elif hd='/'||hd='-' then
                        proclin InRes (cstr+hd.ToString()) tl gm
                    else
                        proclin InMove (cstr+hd.ToString()) tl gm
                |InRes -> 
                    proclin st (cstr+hd.ToString()) tl gm
                |Invalid -> 
                    proclin st cstr tl gm
                |InHeader -> 
                    if hd=']' then
                        let ngm = gm|>AddTag cstr
                        proclin Unknown "" tl ngm
                    else
                        proclin st (cstr+hd.ToString()) tl gm
                |InMove -> 
                    if hd=' ' then
                        let mte = MoveTextEntry.Parse(cstr)
                        let ngm = {gm with MoveText=mte::gm.MoveText}
                        proclin Unknown "" tl ngm
                    else
                        proclin st (cstr+hd.ToString()) tl gm
                |FinishedOK |FinishedInvalid -> st, cstr, gm
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
                    proclin st cstr ns gm
    
        let rec getgm st cstr gm = 
            let lin = sr.ReadLine()
            if lin |> isNull then { gm with MoveText = (gm.MoveText |> List.rev) }
            else 
                let nst, ncstr, ngm = proclin st cstr lin gm
                if nst = FinishedOK then { ngm with MoveText = (ngm.MoveText |> List.rev) }
                elif nst = FinishedInvalid then GameEMP
                else getgm nst ncstr ngm
    
        let gm = getgm Unknown "" GameEMP
        gm
    
    let GameFromString(str : string) =
        let byteArray = Encoding.ASCII.GetBytes(str)
        let stream = new MemoryStream(byteArray)
        let sr = new StreamReader(stream)
        let gm = NextGameRdr(sr)
        gm    
