namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestEco1 () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let eco = tfol + "scid.eco"

    [<TestMethod>]
     member this.EcoRead () =
        let actual = Eco.Read(eco)
        Assert.AreEqual(10360, actual)

[<TestClass>]
type TestEco2 () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    let ecodb = tfol + "eco"
    let eco = tfol + "scid.eco"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(ecodb + ".si4") then
            File.Delete(ecodb + ".si4")
            File.Delete(ecodb + ".sg4")
            File.Delete(ecodb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        File.Copy(testdb + ".si4", ecodb + ".si4")
        File.Copy(testdb + ".sg4", ecodb + ".sg4")
        File.Copy(testdb + ".sn4", ecodb + ".sn4")
        Base.Open(ecodb)|>ignore
        Eco.Read(eco)|>ignore

    [<TestMethod>]
     member this.EcoBase () =
        let mutable msgs = ""
        let actual = Eco.Base(&msgs)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("Classified 6 games", msgs)

    [<TestMethod>]
     member this.EcoScidGame () =
        ScidGame.Load(1u)|>ignore
        let mutable eco = ""
        let actual = Eco.ScidGame(&eco)
        Assert.AreEqual(0, actual)
        Assert.AreEqual("E01", eco)
