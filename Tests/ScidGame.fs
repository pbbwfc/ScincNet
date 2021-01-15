namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs
open FsChess

[<TestClass>]
type TestScidGame () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let gmdb = tfol + "game"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(gmdb + ".si4") then
            File.Delete(gmdb + ".si4")
            File.Delete(gmdb + ".sg4")
            File.Delete(gmdb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        if not (File.Exists(gmdb + ".si4")) then
            File.Copy(testdb + ".si4", gmdb + ".si4")
            File.Copy(testdb + ".sg4", gmdb + ".sg4")
            File.Copy(testdb + ".sn4", gmdb + ".sn4")
        Base.Open(gmdb)|>ignore
        //ScidGame.Load(1u)|>ignore

    [<TestMethod>]
    member this.ScidGameLoad () =
        let actual = ScidGame.Load(1u)
        ScidGame.Save(1u,Base.Current())|>ignore
        Assert.AreEqual(0, actual)
  
    [<TestMethod>]
    member this.ScidGameSave () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.Save(1u,Base.Current())
        Assert.AreEqual(0, actual)

    [<TestMethod>]
    member this.ScidGameStripComments () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.StripComments()
        ScidGame.Save(1u,Base.Current())|>ignore
        Assert.AreEqual(0, actual)

    [<TestMethod>]
    member this.ScidGameGetTag () =
        ScidGame.Load(1u)|>ignore
        let mutable wnm = "" 
        let actual = ScidGame.GetTag("White",&wnm)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("A Kalaiyalahan",wnm)

    [<TestMethod>]
    member this.ScidGameSetTag () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.SetTag("White","White Name")
        ScidGame.Save(1u,Base.Current())|>ignore
        Assert.AreEqual(0, actual)

    [<TestMethod>]
    member this.ScidGameGetFen () =
        ScidGame.Load(1u)|>ignore
        let mutable fen = "" 
        let actual = ScidGame.GetFen(&fen)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("", fen)
        ScidGame.Load(2u)|>ignore
        let actual = ScidGame.GetFen(&fen)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("r1bqkb1r/pppn1ppp/4pn2/3p4/2PP4/5NP1/PP2PP1P/RNBQKB1R w KQkq - 1 5", fen)
    
    [<TestMethod>]
    member this.ScidGameHasNonStandardStart () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.HasNonStandardStart()
        Assert.AreEqual(false, actual)
        ScidGame.Load(2u)|>ignore
        let actual = ScidGame.HasNonStandardStart()
        Assert.AreEqual(true, actual)
        
    [<TestMethod>]
    member this.ScidGameGetMoves () =
        ScidGame.Load(1u)|>ignore
        let mutable mvs = new ResizeArray<string>() 
        let actual = ScidGame.GetMoves(&mvs,-1)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("d4", mvs.[0])
        Assert.AreEqual(104, mvs.Count)
        ScidGame.Load(2u)|>ignore
        mvs.Clear()
        let actual = ScidGame.GetMoves(&mvs,-1)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("Bg2", mvs.[0])
        Assert.AreEqual(96, mvs.Count)
        mvs.Clear()
        let actual = ScidGame.GetMoves(&mvs,10)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("Bg2", mvs.[0])
        Assert.AreEqual(10, mvs.Count)
        
    [<TestMethod>]
    member this.GetMovesPosns () =
        ScidGame.Load(1u)|>ignore
        let mutable mvs = new ResizeArray<string>()
        let mutable posns = new ResizeArray<string>()
        let actual = ScidGame.GetMovesPosns(&mvs,&posns,-1)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("d4", mvs.[0])
        Assert.AreEqual(104, mvs.Count)
        Assert.AreEqual("RNBQKBNRPPPPPPPP................................pppppppprnbqkbnr w", posns.[0])
        Assert.AreEqual("RNBQKBNRPPP.PPPP...........P................p...pppp.ppprnbqkbnr w", posns.[2])
        Assert.AreEqual(104, posns.Count)
        let mutable bd = FsChess.Board.Start
        let mutable bdstr = FsChess.Board.ToSimpleStr(bd)
        Assert.AreEqual(bdstr, posns.[0])
        for i=1 to mvs.Count-1 do
            bd <- Board.PushSAN mvs.[i-1] bd
            bdstr <- FsChess.Board.ToSimpleStr(bd)
            Assert.AreEqual(bdstr, posns.[i])
        ScidGame.Load(2u)|>ignore
        mvs.Clear()
        posns.Clear()
        let actual = ScidGame.GetMovesPosns(&mvs,&posns,-1)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("Bg2", mvs.[0])
        Assert.AreEqual(96, mvs.Count)
        Assert.AreEqual("RNBQKB.RPP..PP.P.....NP...PP.......p........pn..pppn.pppr.bqkb.r w", posns.[0])
        Assert.AreEqual(96, posns.Count)
        bd <- FsChess.Board.FromSimpleStr(posns.[0])
        bdstr <- FsChess.Board.ToSimpleStr(bd)
        Assert.AreEqual(bdstr, posns.[0])
        for i=1 to mvs.Count-1 do
            bd <- Board.PushSAN mvs.[i-1] bd
            bdstr <- FsChess.Board.ToSimpleStr(bd)
            Assert.AreEqual(bdstr, posns.[i])
        mvs.Clear()
        posns.Clear()
        let actual = ScidGame.GetMovesPosns(&mvs,&posns,10)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("Bg2", mvs.[0])
        Assert.AreEqual(10, mvs.Count)
        //random sample
        let mvs2 = "e4 e6 d4 d5 exd5 exd5 Nf3 Bg4 h3 Bh5 Qe2+ Qe7 Be3".Split(' ')
        mvs.Clear()
        mvs2|>Seq.iter(fun mv->mvs.Add(mv))
        posns.Clear()
        bd <- FsChess.Board.Start
        for i=1 to mvs.Count-1 do
            bd <- Board.PushSAN mvs.[i-1] bd
            bdstr <- FsChess.Board.ToSimpleStr(bd)

        
    [<TestMethod>]
    member this.ScidGameList () =
        let mutable gmsl = new ResizeArray<ScincFuncs.gmui>()
        let actual = ScidGame.List(&gmsl,1u,1u)
        Assert.AreEqual(0, actual)
        // "1|A Kalaiyalahan|P Brooks|0-1|52|2016.10.17|V Wimbledon 1|     |     |?|Wimbledon| || 1| 4|E01|1.d4 e6 2.c4|| |don|2016.10.17|2:3"
        Assert.AreEqual(1,gmsl.[0].Num)
        Assert.AreEqual("A Kalaiyalahan",gmsl.[0].White)
        Assert.AreEqual("P Brooks",gmsl.[0].Black)
        Assert.AreEqual("0-1",gmsl.[0].Result)
        Assert.AreEqual(52,gmsl.[0].Length)
        Assert.AreEqual("2016.10.17",gmsl.[0].Date)
        Assert.AreEqual("V Wimbledon 1",gmsl.[0].Event)
        Assert.AreEqual(0,gmsl.[0].W_Elo)
        Assert.AreEqual(0,gmsl.[0].B_Elo)
        Assert.AreEqual(0,gmsl.[0].Round)
        Assert.AreEqual("Wimbledon",gmsl.[0].Site)
        Assert.AreEqual("",gmsl.[0].Deleted)
        Assert.AreEqual(0,gmsl.[0].Variations)
        Assert.AreEqual(1,gmsl.[0].Comments)
        Assert.AreEqual(4,gmsl.[0].Annos)
        Assert.AreEqual("E01",gmsl.[0].ECO)
        Assert.AreEqual("1.d4 e6 2.c4",gmsl.[0].Opening)
        Assert.AreEqual("",gmsl.[0].Flags)
        Assert.AreEqual("",gmsl.[0].Start)
        
    [<TestMethod>]
    member this.ScidGamePgn () =
        ScidGame.Load(1u)|>ignore
        let mutable pgn = ""
        let actual = ScidGame.Pgn(&pgn)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("[Event",pgn.Substring(0,6))

        