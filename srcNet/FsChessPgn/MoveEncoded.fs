namespace FsChessPgn

open FsChess
open System.Text.RegularExpressions

module MoveEncoded =
    
    let FromMove (bd:Brd) mno (mv:Move) =
        let pmv = MoveUtil.topMove bd mv
        {
            San = pmv.San
            Mno = mno
            Isw = bd.WhosTurn=Player.White
            Mv = mv
            PostBrd = bd|>Board.MoveApply mv
        }
