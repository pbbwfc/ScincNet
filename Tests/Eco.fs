namespace Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open ScincFuncs

[<TestClass>]
type TestEco () =
    let tfol = @"D:\GitHub\ScincNet\Tests\data\"
    let eco = tfol + "scid.eco"

    [<TestMethod>]
     member this.EcoRead () =
        let actual = Eco.Read(eco)
        Assert.AreEqual(10360, actual)

