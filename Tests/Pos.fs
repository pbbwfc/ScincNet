namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestPos () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let posdb = tfol + "pos"
    let eco = tfol + "scid.eco"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(posdb + ".si4") then
            File.Delete(posdb + ".si4")
            File.Delete(posdb + ".sg4")
            File.Delete(posdb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        if not (File.Exists(posdb + ".si4")) then
            File.Copy(testdb + ".si4", posdb + ".si4")
            File.Copy(testdb + ".sg4", posdb + ".sg4")
            File.Copy(testdb + ".sn4", posdb + ".sn4")
        Base.Open(posdb)|>ignore

    [<TestMethod>]
     member this.PosBoard () =
        let mutable bdstr = ""
        let actual = Pos.Board(&bdstr)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("RNBQKBNRPPPPPPPP................................pppppppprnbqkbnr w", bdstr)
