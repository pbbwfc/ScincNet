namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

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
        File.Copy(testdb + ".si4", gmdb + ".si4")
        File.Copy(testdb + ".sg4", gmdb + ".sg4")
        File.Copy(testdb + ".sn4", gmdb + ".sn4")
        Base.Open(gmdb)|>ignore
        //ScidGame.Load(1u)|>ignore

    [<TestMethod>]
    member this.ScidGameLoad () =
        let actual = ScidGame.Load(1u)
        ScidGame.Save(1u)|>ignore
        Assert.AreEqual(0, actual)
  
    [<TestMethod>]
    member this.ScidGameSave () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.Save(1u)
        Assert.AreEqual(0, actual)

    [<TestMethod>]
    member this.ScidGameStripComments () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.StripComments()
        ScidGame.Save(1u)|>ignore
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
        ScidGame.Save(1u)|>ignore
        Assert.AreEqual(0, actual)

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
        Assert.AreEqual("don",gmsl.[0].Country)
        Assert.AreEqual("2016.10.17",gmsl.[0].EventDate)
        
    [<TestMethod>]
    member this.ScidGamePgn () =
        ScidGame.Load(1u)|>ignore
        let mutable pgn = ""
        let actual = ScidGame.Pgn(&pgn)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("[Event",pgn.Substring(0,6))

        