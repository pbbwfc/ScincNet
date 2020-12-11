namespace Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestBase () =
    [<TestCleanup>]  
    member this.testClean() = 
        let cls = Base.Close()
        ()
  
    [<TestInitialize>]  
    member this.testInit()   =
        let actual = Base.Open(@"D:\GitHub\ScincNet\Tests\data\test")
        ()

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
