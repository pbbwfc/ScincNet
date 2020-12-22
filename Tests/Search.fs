namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestSearch () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let seadb = tfol + "sea"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(seadb + ".si4") then
            File.Delete(seadb + ".si4")
            File.Delete(seadb + ".sg4")
            File.Delete(seadb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        if not (File.Exists(seadb + ".si4")) then
            File.Copy(testdb + ".si4", seadb + ".si4")
            File.Copy(testdb + ".sg4", seadb + ".sg4")
            File.Copy(testdb + ".sn4", seadb + ".sn4")
        Base.Open(seadb)|>ignore

    [<TestMethod>]
     member this.SearchBoard () =
        let ct0 = Filt.Count()
        Assert.AreEqual(6, ct0)
        let actual = Search.Board("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
        Assert.AreEqual(0, actual)
        let ct1 = Filt.Count()
        Assert.AreEqual(5, ct1)
        let actual = Search.Board("r2qkb1r/p2n1ppp/b1p1pn2/1p1p4/2PP4/1P3NP1/P3PPBP/RNBQ1RK1 w kq - 1 8")
        Assert.AreEqual(0, actual)
        let ct1 = Filt.Count()
        Assert.AreEqual(6, ct1)
        