namespace Tests

open System.IO
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestBase1 () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let testdb = tfol + "test"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Autoloadgame(false, 1u)|>ignore
        Base.Close()|>ignore
  
    [<TestInitialize>]  
    member this.testInit()   =
        Base.Open(testdb)|>ignore
        
    [<TestMethod>]
    member this.BaseAutoload () =
        let actual = Base.Autoloadgame(true, 3u)
        Assert.AreEqual(1, actual)
        let actual = Base.Autoloadgame(false, 3u)
        Assert.AreEqual(0, actual)

    [<TestMethod>]
     member this.BaseOpen () =
        let actual = Base.Open(testdb)
        Assert.AreEqual(-1, actual)
         
    [<TestMethod>]
     member this.BaseClose () =
        let actual = Base.Close()
        Assert.AreEqual(0, actual)

    [<TestMethod>]
     member this.BaseIsreadonly () =
        let actual = Base.Isreadonly()
        Assert.AreEqual(false, actual)

    [<TestMethod>]
     member this.BaseNumGames () =
        let actual = Base.NumGames()
        Assert.AreEqual(6, actual)

    [<TestMethod>]
     member this.BaseFilenames () =
        let actual,nm = Base.Getfilename()
        Assert.AreEqual(0, actual)
        Assert.AreEqual(testdb,nm)

    [<TestMethod>]
     member this.BaseInUse () =
        let actual = Base.InUse()
        Assert.AreEqual(true, actual)

    [<TestMethod>]
     member this.BaseCountFree () =
        let actual = Base.CountFree()
        Assert.AreEqual(7, actual)


[<TestClass>]
type TestBase2 () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let dumdb = tfol + "dummy"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(dumdb + ".si4") then
            File.Delete(dumdb + ".si4")
            File.Delete(dumdb + ".sg4")
            File.Delete(dumdb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        ()

    [<TestMethod>]
     member this.BaseCreate () =
        let actual = Base.Create(dumdb)
        Assert.AreEqual(1, actual)

[<TestClass>]
type TestBase3 () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let impdb = tfol + "imp"
    let pgn = tfol + "test.pgn"
    
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        if File.Exists(impdb + ".si4") then
            File.Delete(impdb + ".si4")
            File.Delete(impdb + ".sg4")
            File.Delete(impdb + ".sn4")
  
    [<TestInitialize>]  
    member this.testInit()   =
        Base.Create(impdb)|>ignore

    [<TestMethod>]
     member this.BaseImport () =
        let mutable num = 0
        let mutable msgs = ""
        let actual = Base.Import(&num,&msgs,pgn)
        Assert.AreEqual(0, actual)
        Assert.AreEqual(2, num)
        Assert.AreEqual("", msgs)
          