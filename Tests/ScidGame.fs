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
    member this.ScidGameSetTag () =
        ScidGame.Load(1u)|>ignore
        let actual = ScidGame.SetTag("White","White Name")
        ScidGame.Save(1u)|>ignore
        Assert.AreEqual(0, actual)
        