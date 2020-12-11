namespace Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestClass () =

    [<TestMethod>]
    member this.BaseAutoload () =
        let actual = Base.Autoload(true, 3u)
        Assert.AreEqual(1, actual)
        let actual = Base.Autoload(false, 3u)
        Assert.AreEqual(-1, actual)

    [<TestMethod>]
     member this.BaseOpen () =
         let actual = Base.Open(@"D:\GitHub\ScincNet\Tests\data\test")
         Assert.AreEqual(1, actual)
