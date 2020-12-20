namespace FsChess.WinForms

[<AutoOpen>]
module Types = 
    
    type PieceType = 
        | EMPTY = 0
        | Pawn = 1
        | Knight = 2
        | Bishop = 3
        | Rook = 4
        | Queen = 5
        | King = 6
    
    type Piece = 
        | WPawn = 1
        | WKnight = 2
        | WBishop = 3
        | WRook = 4
        | WQueen = 5
        | WKing = 6
        | BPawn = 9
        | BKnight = 10
        | BBishop = 11
        | BRook = 12
        | BQueen = 13
        | BKing = 14
        | EMPTY = 0
    
    type Player = 
        | White = 0
        | Black = 1
    
    type GameResult = 
        | Draw = 0
        | WhiteWins = 1
        | BlackWins = -1
        | Open = 9
    
    type File = int16
    let FILE_NAMES = ["a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"]
    
    type Rank = int16
    let RANK_NAMES = ["8"; "7"; "6"; "5"; "4"; "3"; "2"; "1"]
    
    type Square = int16
    let OUTOFBOUNDS:Square = 64s
    let SQUARE_NAMES = [for r in RANK_NAMES do for f in FILE_NAMES -> f+r]
    let Sq(f:File,r:Rank) :Square = r * 8s + f
    
    type MoveType =
        | Simple
        | Capture
        | CastleKingSide
        | CastleQueenSide

    type NAG =
        |Null = 0
        |Good = 1
        |Poor = 2
        |VeryGood = 3
        |VeryPoor = 4
        |Speculative = 5
        |Questionable = 6
        |Even = 10
        |Wslight = 14
        |Bslight = 15
        |Wmoderate = 16
        |Bmoderate =17
        |Wdecisive = 18
        |Bdecisive = 19

    type pMove = 
        {
         Mtype:MoveType 
         TargetSquare:Square 
         Piece: PieceType option
         OriginFile:File option
         OriginRank:Rank option
         PromotedPiece: PieceType option
         IsCheck:bool
         IsDoubleCheck:bool
         IsCheckMate:bool
         }

    type MoveTextEntry =
        |HalfMoveEntry of int option * bool * pMove
        |CommentEntry of string
        |GameEndEntry of GameResult
        |NAGEntry of NAG
        |RAVEntry of MoveTextEntry list
 
    type Game =
        {
            Event : string
            Site : string
            Year : int option
            Month : int option
            Day : int option
            Round :string
            WhitePlayer : string
            BlackPlayer : string
            Result : GameResult
            WhiteElo : string
            BlackElo : string
            BoardSetup : string
            AdditionalInfo : Map<string,string>
            MoveText : MoveTextEntry list
        }

    let GameEMP =
        {
            Event = "?"
            Site = "?"
            Year = None
            Month = None
            Day = None
            Round = "?"
            WhitePlayer = "?"
            BlackPlayer = "?"
            Result = GameResult.Open
            WhiteElo = "-"
            BlackElo = "-"
            BoardSetup = ""
            AdditionalInfo = Map.empty
            MoveText = []
        }
