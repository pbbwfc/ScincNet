namespace Tests

open FsUnit.MsTest
open Microsoft.VisualStudio.TestTools.UnitTesting
open FsChess

[<TestClass>]
type Board () =

    let s0 = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
    let s1 = "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp6/2N1P2P/1PPP1P2/R1BQKBNR w KQkq - 0 1"
    let s2 = "rnbqkbn1/1pppppp1/r7/p6p/4P3/2PP1N2/PP3PPP/RNBQKB1R b KQkq - 0 1"
    let s3 = "r1bq1bnr/p1pP1kp1/n3p2p/5pP1/Pp6/2N4P/1PPP1P2/R1BQKBNR w KQkq - 0 1"
    
    [<TestMethod>]
    member this.``Load start to Posn``() = 
        let ans = Board.FromStr s0
        ans|>Board.ToStr|> should equal s0

    [<TestMethod>]
    member this.``Load pos1 to Posn``() = 
        let ans = Board.FromStr s1
        ans|>Board.ToStr|> should equal s1

    [<TestMethod>]
    member this.``Load pos2 to Posn``() = 
        let ans = Board.FromStr s2
        ans|>Board.ToStr|> should equal s2

    [<TestMethod>]
    member this.``Get Castle Kingside`` () =
      let pos = Board.FromStr "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp6/2N1P2P/1PPP1P2/R1BQK2R w KQkq - 0 1"
      let ans = Move.FromSan pos "O-O"
      ans|>Move.ToSan pos|> should equal "O-O"
      ans|>Move.From |> should equal 60s
      ans|>Move.To |> should equal 62s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp6/2N1P2P/1PPP1P2/R1BQ1RK1 b kq - 1 1"

    [<TestMethod>]
    member this.``Get Castle Queenside`` () =
      let pos = Board.FromStr "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp6/2N1P2P/1PPP1P2/R3KBNR w KQkq - 0 1"
      let ans = Move.FromSan pos "O-O-O"
      ans|>Move.ToSan pos|> should equal "O-O-O"
      ans|>Move.From |> should equal 60s
      ans|>Move.To |> should equal 58s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp6/2N1P2P/1PPP1P2/2KR1BNR b kq - 1 1"

    [<TestMethod>]
    member this.``Get Pawn Move`` () =
      let pos = Board.FromStr s1
      let ans = Move.FromSan pos "d4"
      ans|>Move.ToSan pos|> should equal "d4"
      ans|>Move.From |> should equal 51s
      ans|>Move.To |> should equal 35s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp1P4/2N1P2P/1PP2P2/R1BQKBNR b KQkq d3 0 1"

    [<TestMethod>]
    member this.``Get Knight Move`` () =
      let pos = Board.FromStr s1
      let ans = Move.FromSan pos "Nf3"
      ans|>Move.ToSan pos|> should equal "Nf3"
      ans|>Move.From |> should equal 62s
      ans|>Move.To |> should equal 45s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp6/2N1PN1P/1PPP1P2/R1BQKB1R b KQkq - 1 1"

    [<TestMethod>]
    member this.``Get Knight Move 2`` () =
      let pos = Board.FromStr "rn2k1nr/pp3ppp/1qp5/3p1b2/3P4/1QN1P1P1/PP2BPP1/R3K1NR b KQkq - 0 1"
      let ans = Move.FromSan pos "Na6"
      ans|>Move.ToSan pos|> should equal "Na6"
      ans|>Move.From |> should equal 1s
      ans|>Move.To |> should equal 16s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "r3k1nr/pp3ppp/nqp5/3p1b2/3P4/1QN1P1P1/PP2BPP1/R3K1NR w KQkq - 1 2"

    [<TestMethod>]
    member this.``Get Rook Move`` () =
      let pos = Board.FromStr "r6r/1p1b1kp1/p1b1pp1p/q7/2p1PB2/2N3Q1/PPP2PPP/3R1RK1 w KQkq - 1 1"
      let ans = Move.FromSan pos "Rd2"
      ans|>Move.ToSan pos|> should equal "Rd2"
      ans|>Move.From |> should equal 59s
      ans|>Move.To |> should equal 51s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "r6r/1p1b1kp1/p1b1pp1p/q7/2p1PB2/2N3Q1/PPPR1PPP/5RK1 b KQkq - 2 1"

    [<TestMethod>]
    member this.``Get Knight Capture`` () =
      let pos = Board.FromStr s1
      let ans = Move.FromSan pos "Nxd5"
      ans|>Move.ToSan pos|> should equal "Nxd5"
      ans|>Move.From |> should equal 42s
      ans|>Move.To |> should equal 27s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2p/3N1pP1/Pp6/4P2P/1PPP1P2/R1BQKBNR b KQkq - 0 1"

    [<TestMethod>]
    member this.``Get Rook Capture`` () =
      let pos = Board.FromStr "3rr3/1p3kp1/p4p1p/q1b1pP2/1bp1P3/2N1B1Q1/PPPR2PP/3R2K1 w KQkq - 1 1"
      let ans = Move.FromSan pos "Rxd8"
      ans|>Move.ToSan pos|> should equal "Rxd8"
      ans|>Move.From |> should equal 51s
      ans|>Move.To |> should equal 3s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "3Rr3/1p3kp1/p4p1p/q1b1pP2/1bp1P3/2N1B1Q1/PPP3PP/3R2K1 b KQkq - 0 1"

    [<TestMethod>]
    member this.``Get Queen Capture`` () =
      let pos = Board.FromStr "1Q3b2/3Nq1kp/6p1/3Q1p2/3P4/4P1PP/2r3r1/1R2R1K1 w KQkq - 1 1"
      let ans = Move.FromSan pos "Qxg2"
      ans|>Move.ToSan pos|> should equal "Qxg2"
      ans|>Move.From |> should equal 27s
      ans|>Move.To |> should equal 54s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "1Q3b2/3Nq1kp/6p1/5p2/3P4/4P1PP/2r3Q1/1R2R1K1 b KQkq - 0 1"

    [<TestMethod>]
    member this.``Get Pawn Capture`` () =
      let pos = Board.FromStr s1
      let ans = Move.FromSan pos "gxh6"
      ans|>Move.ToSan pos|> should equal "gxh6"
      ans|>Move.From |> should equal 30s
      ans|>Move.To |> should equal 23s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2P/3p1p2/Pp6/2N1P2P/1PPP1P2/R1BQKBNR b KQkq - 0 1"

    [<TestMethod>]
    member this.``Get Pawn Capture ep`` () =
      let pos = Board.FromStr "rnbqkbnr/pppp2pp/8/4ppP1/8/8/PPPPPP1P/RNBQKBNR w KQkq f6 0 3"
      let ans = Move.FromSan pos "gxf6e.p."
      ans|>Move.ToSan pos|> should equal "gxf6"
      ans|>Move.From |> should equal 30s
      ans|>Move.To |> should equal 21s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/pppp2pp/5P2/4p3/8/8/PPPPPP1P/RNBQKBNR b KQkq - 0 3"

    [<TestMethod>]
    member this.``Get ambiguous file`` () =
      let pos = Board.FromStr s1
      let ans = Move.FromSan pos "Nge2"
      ans|>Move.ToSan pos|> should equal "Nge2"
      ans|>Move.From |> should equal 62s
      ans|>Move.To |> should equal 52s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2p/3p1pP1/Pp6/2N1P2P/1PPPNP2/R1BQKB1R b KQkq - 1 1"

    [<TestMethod>]
    member this.``Get ambiguous Rook Move`` () =
      let pos = Board.FromStr "r3b2r/1p1b1kp1/p3pp1p/q7/2p1PB2/2N3Q1/PPP2PPP/R4RK1 w KQkq - 1 1"
      let ans = Move.FromSan pos "Rad1"
      ans|>Move.ToSan pos|> should equal "Rad1"
      ans|>Move.From |> should equal 56s
      ans|>Move.To |> should equal 59s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "r3b2r/1p1b1kp1/p3pp1p/q7/2p1PB2/2N3Q1/PPP2PPP/3R1RK1 b Kkq - 2 1"

    [<TestMethod>]
    member this.``Get ambiguous rank`` () =
      let pos = Board.FromStr s2
      let ans = Move.FromSan pos "R6a7"
      ans|>Move.ToSan pos|> should equal "R6a7"
      ans|>Move.From |> should equal 16s
      ans|>Move.To |> should equal 8s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbn1/rpppppp1/8/p6p/4P3/2PP1N2/PP3PPP/RNBQKB1R w KQkq - 1 2"

    [<TestMethod>]
    member this.``Get promotion`` () =
      let pos = Board.FromStr "8/6P1/7K/8/1p6/8/Pk6/8 w KQkq - 1 1"
      let ans = Move.FromSan pos "g8=Q"
      ans|>Move.ToSan pos|> should equal "g8=Q"
      ans|>Move.From |> should equal 14s
      ans|>Move.To |> should equal 6s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "6Q1/8/7K/8/1p6/8/Pk6/8 b KQkq - 0 1"

    [<TestMethod>]
    member this.``Get promotion capture`` () =
      let pos = Board.FromStr s3
      let ans = Move.FromSan pos "dxc8=Q"
      ans|>Move.ToSan pos|> should equal "dxc8=Q"
      ans|>Move.From |> should equal 11s
      ans|>Move.To |> should equal 2s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "r1Qq1bnr/p1p2kp1/n3p2p/5pP1/Pp6/2N4P/1PPP1P2/R1BQKBNR b KQkq - 0 1"

    [<TestMethod>]
    member this.``Get check`` () =
      let pos = Board.FromStr s1
      let ans = Move.FromSan pos "Bb5+"
      ans|>Move.ToSan pos|> should equal "Bb5+"
      ans|>Move.From |> should equal 61s
      ans|>Move.To |> should equal 25s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqkbnr/p1p3p1/4p2p/1B1p1pP1/Pp6/2N1P2P/1PPP1P2/R1BQK1NR b KQkq - 1 1"

    [<TestMethod>]
    member this.``Get Queen check`` () =
      let pos = Board.FromStr "8/8/Q7/1r6/5P1P/Pk4P1/6BK/3qq3 b KQkq - 1 1"
      let ans = Move.FromSan pos "Qg1+"
      ans|>Move.ToSan pos|> should equal "Qg1+"
      ans|>Move.From |> should equal 60s
      ans|>Move.To |> should equal 62s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "8/8/Q7/1r6/5P1P/Pk4P1/6BK/3q2q1 w KQkq - 2 2"

    [<TestMethod>]
    member this.``Get where restricted by check`` () =
      let pos = Board.FromStr "rnbqk2r/pp1p1ppp/4pn2/2p5/1bPP4/2N1P3/PP3PPP/R1BQKBNR w KQkq - 1 1"
      let ans = Move.FromSan pos "Ne2"
      ans|>Move.ToSan pos|> should equal "Nge2"
      ans|>Move.From |> should equal 62s
      ans|>Move.To |> should equal 52s
      let npos = pos|>Board.Push ans
      npos|>Board.ToStr|>should equal "rnbqk2r/pp1p1ppp/4pn2/2p5/1bPP4/2N1P3/PP2NPPP/R1BQKB1R b KQkq - 2 1"

