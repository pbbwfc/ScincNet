namespace Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestBase () =
    [<TestCleanup>]  
    member this.testClean() = 
        Base.Close()|>ignore
        
  
    [<TestInitialize>]  
    member this.testInit()   =
        Base.Open(@"D:\GitHub\ScincNet\Tests\data\test")|>ignore
        
    [<TestMethod>]
    member this.BaseAutoload () =
        let actual = Base.Autoload(true, 3u)
        Assert.AreEqual(3, actual)
        let actual = Base.Autoload(false, 3u)
        Assert.AreEqual(0, actual)

    [<TestMethod>]
     member this.BaseOpen () =
        let actual = Base.Open(@"D:\GitHub\ScincNet\Tests\data\test")
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
        let mutable name = "";
        let actual = Base.Filename(&name)
        Assert.AreEqual(0, actual)
        Assert.AreEqual(@"D:\GitHub\ScincNet\Tests\data\test",name)

    [<TestMethod>]
     member this.BaseInUse () =
        let actual = Base.InUse()
        Assert.AreEqual(true, actual)
        