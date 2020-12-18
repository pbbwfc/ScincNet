namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestScidGame () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let gmdb = tfol + "game"
    let formatStr = "g* w* b* r* m* d* e* W* B* n* s* D* V* C* A* o* O* U* S* c* E* F* "
    
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
    member this.ScidGameSetTag () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.SetTag("White","White Name")
        ScidGame.Save(1u)|>ignore
        Assert.AreEqual(0, actual)

    [<TestMethod>]
    member this.ScidGameList () =
        let mutable glist = ""
        let actual = ScidGame.List(&glist,1u,1u,formatStr)
        Assert.AreEqual(0, actual)
        let exp = "1 A Kalaiyalahan P Brooks 0-1 52 2016.10.17 V Wimbledon 1             ? Wimbledon     1  4 E01 1.d4 e6 2.c4    don 2016.10.17 2:3 \n"
        Assert.AreEqual(exp, glist)

        