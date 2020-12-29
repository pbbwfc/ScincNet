namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestCompact () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let cmpdb = tfol + "cmp"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(cmpdb + ".si4") then
            File.Delete(cmpdb + ".si4")
            File.Delete(cmpdb + ".sg4")
            File.Delete(cmpdb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        if not (File.Exists(cmpdb + ".si4")) then
            File.Copy(testdb + ".si4", cmpdb + ".si4")
            File.Copy(testdb + ".sg4", cmpdb + ".sg4")
            File.Copy(testdb + ".sn4", cmpdb + ".sn4")
        Base.Open(cmpdb)|>ignore
        //now delete a game
        ScidGame.Delete(4u)|>ignore


    [<TestMethod>]
     member this.CompactGames () =
        //count games before
        Assert.AreEqual(6, Base.NumGames())
        let actual = Compact.Games()
        Assert.AreEqual(0, actual)
        //count games after
        Assert.AreEqual(5, Base.NumGames())
        