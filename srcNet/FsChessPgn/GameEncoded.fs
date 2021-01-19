﻿namespace FsChessPgn

open FsChess

module GameEncoded =

    let Start = EncodedGameEMP

    let MoveCount(mtel:EncodedMoveTextEntry list) =
        let mc(mte:EncodedMoveTextEntry) =
            match mte with
            |EncodedHalfMoveEntry(_) -> 1
            |_ -> 0
        if mtel.IsEmpty then 0
        else
            mtel|>List.map mc|>List.reduce(+)
        
    let FullMoveCount(mtel:EncodedMoveTextEntry list) = MoveCount(mtel)/2

    let GetMoves(mtel:EncodedMoveTextEntry list) =
        let gm(mte:EncodedMoveTextEntry) =
            match mte with
            |EncodedHalfMoveEntry(_,_,mv) -> [mv]
            |_ -> []
        mtel|>List.map gm|>List.concat
    
    let AddTag (tagstr:string) (gm:EncodedGame) =
        let k,v = tagstr.Trim().Split([|'"'|])|>Array.map(fun s -> s.Trim())|>fun a -> a.[0],a.[1].Trim('"')
        match k with
        | "Event" -> {gm with Event = v}
        | "Site" -> {gm with Site = v}
        | "Date" -> 
            let yop,mop,dop = v|>DateUtil.FromStr
            {gm with Year = yop; Month = mop; Day = dop}
        | "Round" -> {gm with Round = v}
        | "White" -> {gm with WhitePlayer = v}
        | "Black" -> {gm with BlackPlayer = v}
        | "Result" -> {gm with Result = v|>GameResult.Parse}
        | "WhiteElo" -> {gm with WhiteElo = v}
        | "BlackElo" -> {gm with BlackElo = v}
        | "ECO" -> {gm with ECO = v}
        | "FEN" -> {gm with BoardSetup = v|>FEN.Parse|>Board.FromFEN|>Some}
        | _ ->
            {gm with AdditionalInfo=gm.AdditionalInfo.Add(k,v)}
    
    let AddMoveEntry (mte:EncodedMoveTextEntry) (gm:EncodedGame) =
        {gm with MoveText=gm.MoveText@[mte]}

    let RemoveMoveEntry (gm:EncodedGame) =
        let mtel = gm.MoveText
        let nmtel =
            if mtel.IsEmpty then mtel
            else
                mtel|>List.rev|>List.tail|>List.rev
        {gm with MoveText=nmtel}

    let Encode(gm:UnencodedGame) =
        let rec setemv (pmvl:MoveTextEntry list) mct prebd bd oemvl =
            if pmvl|>List.isEmpty then oemvl|>List.rev
            else
                let mte = pmvl.Head
                match mte with
                |HalfMoveEntry(mn,ic,mv,_) -> 
                    let emv = mv|>pMove.Encode bd mct
                    let nmte = EncodedHalfMoveEntry(mn,ic,emv)
                    let nmct = if bd.WhosTurn=Player.White then mct else mct+1
                    setemv pmvl.Tail nmct bd emv.PostBrd (nmte::oemvl)
                |RAVEntry(mtel) -> 
                    let nmct = if prebd.WhosTurn=Player.Black then mct-1 else mct
                    let nmtel = setemv mtel nmct prebd prebd []
                    let nmte = EncodedRAVEntry(nmtel)
                    setemv pmvl.Tail mct prebd bd (nmte::oemvl)
                |CommentEntry(c) -> setemv pmvl.Tail mct prebd bd (EncodedCommentEntry(c)::oemvl)
                |GameEndEntry(r) -> setemv pmvl.Tail mct prebd bd (EncodedGameEndEntry(r)::oemvl)
                |NAGEntry(n) -> setemv pmvl.Tail mct prebd bd (EncodedNAGEntry(n)::oemvl)
        
        let ibd = if gm.BoardSetup.IsSome then gm.BoardSetup.Value else Board.Start
        let nmt = setemv gm.MoveText 1 ibd ibd []
        let egm0 = EncodedGameEMP
        let egm1 = {egm0 with
                        WhitePlayer = gm.WhitePlayer
                        BlackPlayer = gm.BlackPlayer
                        Result = gm.Result
                        Year = gm.Year
                        Month = gm.Month
                        Day = gm.Day
                        Event = gm.Event
                        WhiteElo = gm.WhiteElo
                        BlackElo = gm. BlackElo
                        Round = gm.Round
                        Site = gm. Site
                        ECO = gm.ECO
                        BoardSetup = gm.BoardSetup
                        AdditionalInfo = gm.AdditionalInfo
                        MoveText = nmt}
        egm1

    let AddRav (gm:EncodedGame) (irs:int list) (mv:Move) = 
        let rec getadd mct ci nmte (imtel:EncodedMoveTextEntry list) (omtel:EncodedMoveTextEntry list) =
            if ci>omtel.Length then getadd mct ci nmte imtel.Tail (imtel.Head::omtel)
            elif imtel.IsEmpty then (EncodedRAVEntry([nmte])::omtel)|>List.rev,omtel.Length
            else
                //ignore first move
                let mte = imtel.Head
                if mct=0 then
                    match mte with
                    |EncodedHalfMoveEntry(_) -> getadd 1 ci nmte imtel.Tail (imtel.Head::omtel)
                    |_ -> getadd 0 ci nmte imtel.Tail (imtel.Head::omtel)
                else
                    match mte with
                    |EncodedGameEndEntry(_) -> ((EncodedRAVEntry([nmte])::omtel)|>List.rev)@imtel,omtel.Length 
                    |EncodedHalfMoveEntry(_,_,emv) -> 
                        //need to fix black move
                        let mn,isw = emv.Mno,emv.Isw
                        let fmte = if isw then mte else EncodedHalfMoveEntry(mn|>Some,true,emv)
                        (fmte::(EncodedRAVEntry([nmte])::omtel)|>List.rev)@imtel.Tail,omtel.Length
                    |_ -> getadd 1 ci nmte imtel.Tail (imtel.Head::omtel)
        if irs.Length=1 then
            //before moves
            if irs.Head= -1 then
                let emv = MoveEncoded.FromMove Board.Start 1 mv
                let nmte = EncodedHalfMoveEntry(1|>Some,false,emv)
                {gm with MoveText=gm.MoveText.Head::EncodedRAVEntry([nmte])::gm.MoveText.Tail},[1;0]
            else
                let cmv = gm.MoveText.[irs.Head]
                let bd,lmn,lisw =
                    match cmv with
                    |EncodedHalfMoveEntry(_,_,emv) -> emv.PostBrd, emv.Mno, emv.Isw
                    |_ -> failwith "should be a move"
                let mn = if lisw then lmn else lmn+1
                let emv = mv|>MoveEncoded.FromMove bd mn
                let nmte = EncodedHalfMoveEntry(mn|>Some,bd.WhosTurn=Player.Black,emv)

                let nmtel,ni = getadd 0 (irs.Head+1) nmte gm.MoveText []
                {gm with MoveText=nmtel},[ni;0]
        else
            let rec getcur indx (cirs:int list) (mtel:EncodedMoveTextEntry list) =
                if cirs.Length=1 && indx=cirs.Head then mtel.Head
                elif indx=cirs.Head then
                    let rv = mtel.Head
                    match rv with
                    |EncodedRAVEntry(nmtel) -> getcur 0 cirs.Tail nmtel
                    |_ -> failwith "should be RAV"
                else
                    let mte = mtel.Head
                    match mte with
                    |EncodedHalfMoveEntry(_) -> getcur (indx+1) cirs mtel.Tail
                    |_ -> getcur (indx+1) cirs mtel.Tail
            let cmv = getcur 0 irs gm.MoveText        
            let bd,lmn,lisw =
                match cmv with
                |EncodedHalfMoveEntry(_,_,emv) -> emv.PostBrd, emv.Mno, emv.Isw
                |_ -> failwith "should be a move"
            let mn = if lisw then lmn else lmn+1
            let emv = mv|>MoveEncoded.FromMove bd mn
            let nmte = EncodedHalfMoveEntry(mn|>Some,bd.WhosTurn=Player.Black,emv)
            let rec getnmtel (cirs:int list) (mtel:EncodedMoveTextEntry list) =
                if cirs.Length=1 then 
                    let nmtel,_ = getadd 0 (cirs.Head+1) nmte mtel []
                    nmtel
                else
                    let i = cirs.Head
                    let rav = mtel.[i]
                    match rav with
                    |EncodedRAVEntry(nmtel) ->
                        mtel.[..i-1]@[EncodedRAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
                    |_ -> failwith "should be RAV"
            let rec getnmirs (cirs:int list) (pirs:int list) (mtel:EncodedMoveTextEntry list) =
                if cirs.Length=1 then 
                    let _,ni = getadd 0 (cirs.Head+1) nmte mtel []
                    (0::ni::pirs)|>List.rev
                else
                    let i = cirs.Head
                    let rav = mtel.[i]
                    match rav with
                    |EncodedRAVEntry(nmtel) ->
                        getnmirs cirs.Tail (i::pirs) nmtel
                    |_ -> failwith "should be RAV"
            let nmtel = getnmtel irs gm.MoveText
            let nirs = getnmirs irs [] gm.MoveText
            {gm with MoveText=nmtel},nirs

    let DeleteRav (gm:EncodedGame) (iirs:int list)  =
        if iirs.Length=1 then
            {gm with MoveText=[]}
        else
            let irs = iirs.[..iirs.Length-2]
            if irs.Length=1 then
                let i = irs.Head
                let nmtel = 
                    if i=0 then gm.MoveText.Tail
                    else
                        //fix Black by removing mn and cont
                        let pmte = 
                            let hm = gm.MoveText.[i+1]
                            match hm with
                            |EncodedHalfMoveEntry(_,_,emv) ->
                                let isw = emv.Isw
                                if isw then hm else EncodedHalfMoveEntry(None,false,emv)
                            |_ -> hm
                        gm.MoveText.[..i-1]@[pmte]@gm.MoveText.[i+2..]
                {gm with MoveText=nmtel}
            else
                let rec getnmtel (cirs:int list) (mtel:EncodedMoveTextEntry list) =
                    if cirs.Length=1 then 
                        let i = cirs.Head
                        let nmtel = 
                            if i=0 then mtel.Tail
                            elif i=mtel.Length-1 then mtel.[..i-1]
                            else
                                //fix Black by removing mn and cont
                                let pmte = 
                                    let hm = gm.MoveText.[i+1]
                                    match hm with
                                    |EncodedHalfMoveEntry(_,_,emv) ->
                                        let isw = emv.Isw
                                        if isw then hm else EncodedHalfMoveEntry(None,false,emv)
                                    |_ -> hm
                                mtel.[..i-1]@[pmte]@mtel.[i+2..]
                        nmtel
                    else
                        let i = cirs.Head
                        let rav = mtel.[i]
                        match rav with
                        |EncodedRAVEntry(nmtel) ->
                            mtel.[..i-1]@[EncodedRAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
                        |_ -> failwith "should be RAV"
                let nmtel = getnmtel irs gm.MoveText
                {gm with MoveText=nmtel}

    let AddMv (gm:EncodedGame) (irs:int list) (mv:Move) = 
        let rec getext ci nmte (imtel:EncodedMoveTextEntry list) (omtel:EncodedMoveTextEntry list) =
            if ci>omtel.Length then getext ci nmte imtel.Tail (imtel.Head::omtel)
            elif imtel.IsEmpty then 
                //need to remove iscont and mn if black, if after a move
                match omtel.Head with
                |EncodedHalfMoveEntry(_) |EncodedNAGEntry(_) ->
                    match nmte with
                    |EncodedHalfMoveEntry(mn,_,emv) ->
                        let nmn = if emv.PostBrd.WhosTurn=Player.White then None else mn
                        (EncodedHalfMoveEntry(nmn,false,emv)::omtel)|>List.rev,omtel.Length
                    |_ -> failwith "can't reach here"
                |_ ->
                    (nmte::omtel)|>List.rev,omtel.Length
            else
                let mte = imtel.Head
                match mte with
                |EncodedGameEndEntry(_) -> 
                    //need to include before this
                    if omtel.Length=0 then
                        ((nmte::omtel)|>List.rev)@imtel,omtel.Length
                    else
                        //need to remove iscont if after a move
                        match omtel.Head with
                        |EncodedHalfMoveEntry(_) |EncodedNAGEntry(_) ->
                            match nmte with
                            |EncodedHalfMoveEntry(mn,_,emv) ->
                                let nmn = if emv.PostBrd.WhosTurn=Player.White then None else mn
                                ((EncodedHalfMoveEntry(nmn,false,emv)::omtel)|>List.rev)@imtel,omtel.Length
                            |_ -> failwith "can't reach here"
                        |_ ->
                            ((nmte::omtel)|>List.rev)@imtel,omtel.Length
                |_ -> getext ci nmte imtel.Tail (imtel.Head::omtel)
        if irs.Length=1 then
            //allow for empty list
            if gm.MoveText.IsEmpty then
                let bd = if gm.BoardSetup.IsSome then gm.BoardSetup.Value else Board.Start
                let mn = 1
                let emv = mv|>MoveEncoded.FromMove bd mn
                let nmte = EncodedHalfMoveEntry(mn|>Some,bd.WhosTurn=Player.Black,emv)
                {gm with MoveText=[nmte]},[0]
            //if not with a selected move
            elif irs.Head = -1 then
                let bd = if gm.BoardSetup.IsSome then gm.BoardSetup.Value else Board.Start
                let mn = 1
                let emv = mv|>MoveEncoded.FromMove bd mn
                let nmte = EncodedHalfMoveEntry(mn|>Some,bd.WhosTurn=Player.Black,emv)
                let nmtel,ni = getext (irs.Head+1) nmte gm.MoveText []
                {gm with MoveText=nmtel},[ni]
            else
                let cmv = gm.MoveText.[irs.Head]
                let bd,lmn,lisw =
                    match cmv with
                    |EncodedHalfMoveEntry(_,_,emv) -> emv.PostBrd, emv.Mno, emv.Isw
                    |_ -> failwith "should be a move"
                let mn = if lisw then lmn else lmn+1
                let emv = mv|>MoveEncoded.FromMove bd mn
                let nmte = EncodedHalfMoveEntry(mn|>Some,bd.WhosTurn=Player.Black,emv)
                let nmtel,ni = getext (irs.Head+1) nmte gm.MoveText []
                {gm with MoveText=nmtel},[ni]
        else
            let rec getcur indx (cirs:int list) (mtel:EncodedMoveTextEntry list) =
                if cirs.Length=1 && indx=cirs.Head then mtel.Head
                elif indx=cirs.Head then
                    let rv = mtel.Head
                    match rv with
                    |EncodedRAVEntry(nmtel) -> getcur 0 cirs.Tail nmtel
                    |_ -> failwith "should be RAV"
                else
                    let mte = mtel.Head
                    match mte with
                    |EncodedHalfMoveEntry(_) -> getcur (indx+1) cirs mtel.Tail
                    |_ -> getcur (indx+1) cirs mtel.Tail
            let cmv = getcur 0 irs gm.MoveText        
            let bd,lmn,lisw =
                match cmv with
                |EncodedHalfMoveEntry(_,_,emv) -> emv.PostBrd, emv.Mno, emv.Isw
                |_ -> failwith "should be a move"
            let mn = if lisw then lmn else lmn+1
            let emv = mv|>MoveEncoded.FromMove bd mn
            let nmte = EncodedHalfMoveEntry(mn|>Some,bd.WhosTurn=Player.Black,emv)
            let rec getnmtel (cirs:int list) (mtel:EncodedMoveTextEntry list) =
                if cirs.Length=1 then 
                    let nmtel,_ = getext (cirs.Head+1) nmte mtel []
                    nmtel
                else
                    let i = cirs.Head
                    let rav = mtel.[i]
                    match rav with
                    |EncodedRAVEntry(nmtel) ->
                        mtel.[..i-1]@[EncodedRAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
                    |_ -> failwith "should be RAV"
            let rec getnmirs (cirs:int list) (pirs:int list) (mtel:EncodedMoveTextEntry list) =
                if cirs.Length=1 then 
                    let _,ni = getext (cirs.Head+1) nmte mtel []
                    (ni::pirs)|>List.rev
                else
                    let i = cirs.Head
                    let rav = mtel.[i]
                    match rav with
                    |EncodedRAVEntry(nmtel) ->
                        getnmirs cirs.Tail (i::pirs) nmtel
                    |_ -> failwith "should be RAV"
            let nmtel = getnmtel irs gm.MoveText
            let nirs = getnmirs irs [] gm.MoveText
            {gm with MoveText=nmtel},nirs
    
    //let CommentBefore (gm:UnencodedGame) (irs:int list) (str:string) =
    //    let mte = CommentEntry(str)
    //    if irs.Length=1 then
    //        //allow for empty list
    //        if gm.MoveText.IsEmpty then
    //            {gm with MoveText=[mte]}
    //        else
    //            let i = irs.Head
    //            //fix Black by adding mn and cont
    //            let hm = gm.MoveText.[i]
    //            let nhm =
    //                match hm with
    //                |HalfMoveEntry(mn,ic,pmv,amv) ->
    //                    let mn,isw = amv.Value.Mno,amv.Value.Isw
    //                    if isw then hm else HalfMoveEntry(mn|>Some,true,pmv,amv)
    //                |_ -> failwith "should be a move"
    //            let nmtel = 
    //                if i=0 then mte::nhm::gm.MoveText.Tail
    //                else
    //                    gm.MoveText.[..i-1]@[mte;nhm]@gm.MoveText.[i+1..]
    //            {gm with MoveText=nmtel}
    //    else
    //        let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //            if cirs.Length=1 then 
    //                let i = cirs.Head
    //                //fix Black by adding mn and cont
    //                let hm = mtel.[i]
    //                let nhm =
    //                    match hm with
    //                    |HalfMoveEntry(mn,ic,pmv,amv) ->
    //                        let mn,isw = amv.Value.Mno,amv.Value.Isw
    //                        if isw then hm else HalfMoveEntry(mn|>Some,true,pmv,amv)
    //                    |_ -> failwith "should be a move"
    //                let nmtel = 
    //                    if i=0 then mte::nhm::mtel.Tail
    //                    else
    //                        //TODO fix Black by adding mn and cont
    //                        mtel.[..i-1]@[mte;nhm]@mtel.[i+1..]
    //                nmtel
    //            else
    //                let i = cirs.Head
    //                let rav = mtel.[i]
    //                match rav with
    //                |RAVEntry(nmtel) ->
    //                    mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //                |_ -> failwith "should be RAV"
    //        let nmtel = getnmtel irs gm.MoveText
    //        {gm with MoveText=nmtel}

    //let CommentAfter (gm:UnencodedGame) (irs:int list) (str:string) =
    //    let mte = CommentEntry(str)
    //    if irs.Length=1 then
    //        //allow for empty list
    //        if gm.MoveText.IsEmpty then
    //            {gm with MoveText=[mte]}
    //        else
    //            let i = irs.Head
    //            let nmtel = 
    //                if i=gm.MoveText.Length-1 then gm.MoveText@[mte]
    //                else
    //                    //fix Black by adding mn and cont
    //                    let pmte = 
    //                        let hm = gm.MoveText.[i+1]
    //                        match hm with
    //                        |HalfMoveEntry(mn,ic,pmv,amv) ->
    //                            let mn,isw = amv.Value.Mno,amv.Value.Isw
    //                            if isw then hm else HalfMoveEntry(mn|>Some,true,pmv,amv)
    //                        |_ -> hm
    //                    gm.MoveText.[..i]@[mte;pmte]@gm.MoveText.[i+2..]
    //            {gm with MoveText=nmtel}
    //    else
    //        let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //            if cirs.Length=1 then 
    //                let i = cirs.Head
    //                let nmtel = 
    //                    if i=mtel.Length-1 then mtel@[mte]
    //                    else
    //                        //fix Black by adding mn and cont
    //                        let pmte = 
    //                            let hm = mtel.[i+1]
    //                            match hm with
    //                            |HalfMoveEntry(mn,ic,pmv,amv) ->
    //                                let mn,isw = amv.Value.Mno,amv.Value.Isw
    //                                if isw then hm else HalfMoveEntry(mn|>Some,true,pmv,amv)
    //                            |_ -> hm
    //                        mtel.[..i]@[mte;pmte]@mtel.[i+2..]
    //                nmtel
    //            else
    //                let i = cirs.Head
    //                let rav = mtel.[i]
    //                match rav with
    //                |RAVEntry(nmtel) ->
    //                    mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //                |_ -> failwith "should be RAV"
    //        let nmtel = getnmtel irs gm.MoveText
    //        {gm with MoveText=nmtel}

    //let EditComment (gm:UnencodedGame) (irs:int list) (str:string) =
    //    let mte = CommentEntry(str)
    //    if irs.Length=1 then
    //        //allow for empty list
    //        if gm.MoveText.IsEmpty then
    //            {gm with MoveText=[mte]}
    //        else
    //            let i = irs.Head
    //            let nmtel = 
    //                if i=0 then mte::gm.MoveText.Tail
    //                else
    //                    gm.MoveText.[..i-1]@[mte]@gm.MoveText.[i+1..]
    //            {gm with MoveText=nmtel}
    //    else
    //        let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //            if cirs.Length=1 then 
    //                let i = cirs.Head
    //                let nmtel = 
    //                    if i=0 then mte::mtel.Tail
    //                    elif i=mtel.Length-1 then mtel.[..i-1]@[mte]
    //                    else
    //                        mtel.[..i-1]@[mte]@mtel.[i+1..]
    //                nmtel
    //            else
    //                let i = cirs.Head
    //                let rav = mtel.[i]
    //                match rav with
    //                |RAVEntry(nmtel) ->
    //                    mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //                |_ -> failwith "should be RAV"
    //        let nmtel = getnmtel irs gm.MoveText
    //        {gm with MoveText=nmtel}

    //let DeleteComment (gm:UnencodedGame) (irs:int list)  =
    //    if irs.Length=1 then
    //        let i = irs.Head
    //        let nmtel = 
    //            if i=0 then gm.MoveText.Tail
    //            else
    //                //fix Black by removing mn and cont
    //                let pmte = 
    //                    let hm = gm.MoveText.[i+1]
    //                    match hm with
    //                    |HalfMoveEntry(_,_,pmv,amv) ->
    //                        let isw = amv.Value.Isw
    //                        if isw then hm else HalfMoveEntry(None,false,pmv,amv)
    //                    |_ -> hm
    //                gm.MoveText.[..i-1]@[pmte]@gm.MoveText.[i+2..]
    //        {gm with MoveText=nmtel}
    //    else
    //        let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //            if cirs.Length=1 then 
    //                let i = cirs.Head
    //                let nmtel = 
    //                    if i=0 then mtel.Tail
    //                    elif i=mtel.Length-1 then mtel.[..i-1]
    //                    else
    //                        //fix Black by removing mn and cont
    //                        let pmte = 
    //                            let hm = mtel.[i+1]
    //                            match hm with
    //                            |HalfMoveEntry(_,_,pmv,amv) ->
    //                                let isw = amv.Value.Isw
    //                                if isw then hm else HalfMoveEntry(None,false,pmv,amv)
    //                            |_ -> hm
    //                        mtel.[..i-1]@[pmte]@mtel.[i+2..]
    //                nmtel
    //            else
    //                let i = cirs.Head
    //                let rav = mtel.[i]
    //                match rav with
    //                |RAVEntry(nmtel) ->
    //                    mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //                |_ -> failwith "should be RAV"
    //        let nmtel = getnmtel irs gm.MoveText
    //        {gm with MoveText=nmtel}
    
    //let AddNag (gm:UnencodedGame) (irs:int list) (ng:NAG) =
    //    let mte = NAGEntry(ng)
    //    if irs.Length=1 then
    //        //allow for empty list
    //        if gm.MoveText.IsEmpty then
    //            {gm with MoveText=[mte]}
    //        else
    //            let i = irs.Head
    //            let nmtel = 
    //                if i=gm.MoveText.Length-1 then gm.MoveText@[mte]
    //                else
    //                    gm.MoveText.[..i]@[mte]@gm.MoveText.[i+1..]
    //            {gm with MoveText=nmtel}
    //    else
    //        let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //            if cirs.Length=1 then 
    //                let i = cirs.Head
    //                let nmtel = 
    //                    if i=mtel.Length-1 then mtel@[mte]
    //                    else
    //                        mtel.[..i]@[mte]@mtel.[i+1..]
    //                nmtel
    //            else
    //                let i = cirs.Head
    //                let rav = mtel.[i]
    //                match rav with
    //                |RAVEntry(nmtel) ->
    //                    mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //                |_ -> failwith "should be RAV"
    //        let nmtel = getnmtel irs gm.MoveText
    //        {gm with MoveText=nmtel}

    //let EditNag (gm:UnencodedGame) (irs:int list) (ng:NAG) =
    //    let mte = NAGEntry(ng)
    //    if irs.Length=1 then
    //        //allow for empty list
    //        if gm.MoveText.IsEmpty then
    //            {gm with MoveText=[mte]}
    //        else
    //            let i = irs.Head
    //            let nmtel = 
    //                if i=0 then mte::gm.MoveText.Tail
    //                else
    //                    gm.MoveText.[..i-1]@[mte]@gm.MoveText.[i+1..]
    //            {gm with MoveText=nmtel}
    //    else
    //        let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //            if cirs.Length=1 then 
    //                let i = cirs.Head
    //                let nmtel = 
    //                    if i=0 then mte::mtel.Tail
    //                    elif i=mtel.Length-1 then mtel.[..i-1]@[mte]
    //                    else
    //                        mtel.[..i-1]@[mte]@mtel.[i+1..]
    //                nmtel
    //            else
    //                let i = cirs.Head
    //                let rav = mtel.[i]
    //                match rav with
    //                |RAVEntry(nmtel) ->
    //                    mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //                |_ -> failwith "should be RAV"
    //        let nmtel = getnmtel irs gm.MoveText
    //        {gm with MoveText=nmtel}

    //let DeleteNag (gm:UnencodedGame) (irs:int list)  =
    //    if irs.Length=1 then
    //        let i = irs.Head
    //        let nmtel = 
    //            if i=0 then gm.MoveText.Tail
    //            else
    //                gm.MoveText.[..i-1]@gm.MoveText.[i+1..]
    //        {gm with MoveText=nmtel}
    //    else
    //        let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //            if cirs.Length=1 then 
    //                let i = cirs.Head
    //                let nmtel = 
    //                    if i=0 then mtel.Tail
    //                    elif i=mtel.Length-1 then mtel.[..i-1]
    //                    else
    //                        mtel.[..i-1]@mtel.[i+1..]
    //                nmtel
    //            else
    //                let i = cirs.Head
    //                let rav = mtel.[i]
    //                match rav with
    //                |RAVEntry(nmtel) ->
    //                    mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //                |_ -> failwith "should be RAV"
    //        let nmtel = getnmtel irs gm.MoveText
    //        {gm with MoveText=nmtel}

    //let GetBoard (bd:Brd) (indx:int,gm:UnencodedGame) =
    //    let rec getbd cbd (ipmvl:pMove list) =
    //        if ipmvl.IsEmpty then false,""
    //        else
    //            let pmv = ipmvl.Head
    //            let mv = pmv|>pMove.ToMove cbd
    //            //now check if a pawn move which is not on search board
    //            let pc = mv|>Move.MovingPiece
    //            if pc=Piece.WPawn then
    //                let sq = mv|>Move.From
    //                let rnk = sq|>Square.ToRank
    //                if rnk=Rank2 && bd.[sq]=Piece.WPawn then false,""
    //                else
    //                    let nbd = cbd|>Board.MoveApply mv
    //                    if nbd.PieceAt=bd.PieceAt then
    //                        if ipmvl.Tail.IsEmpty then false,""
    //                        else
    //                            let npmv = ipmvl.Tail.Head
    //                            let mvstr = npmv|>PgnWrite.MoveStr
    //                            true,mvstr
    //                    else getbd nbd ipmvl.Tail
    //            elif pc=Piece.BPawn then
    //                let sq = mv|>Move.From
    //                let rnk = sq|>Square.ToRank
    //                if rnk=Rank7 && bd.[sq]=Piece.BPawn then false,""
    //                else
    //                    let nbd = cbd|>Board.MoveApply mv
    //                    if nbd.PieceAt=bd.PieceAt then
    //                        if ipmvl.Tail.IsEmpty then false,""
    //                        else
    //                            let npmv = ipmvl.Tail.Head
    //                            let mvstr = npmv|>PgnWrite.MoveStr
    //                            true,mvstr
    //                    else getbd nbd ipmvl.Tail
    //            else
    //                let nbd = cbd|>Board.MoveApply mv
    //                if nbd.PieceAt=bd.PieceAt then
    //                    if ipmvl.Tail.IsEmpty then false,""
    //                    else
    //                        let npmv = ipmvl.Tail.Head
    //                        let mvstr = npmv|>PgnWrite.MoveStr
    //                        true,mvstr
    //                else getbd nbd ipmvl.Tail
        
    //    let initbd = if gm.BoardSetup.IsSome then gm.BoardSetup.Value else Board.Start
    //    let fnd,mvstr = getbd initbd (gm.MoveText|>GetMoves)
    //    if fnd then Some(indx,gm,mvstr) else None

    //let Strip (gm:UnencodedGame) (iirs:int list)  =
    //    let rec getnmtel (cirs:int list) (mtel:MoveTextEntry list) =
    //        if cirs.Length=1 then 
    //            let i = cirs.Head
    //            mtel.[..i]
    //        else
    //            let i = cirs.Head
    //            let rav = mtel.[i]
    //            match rav with
    //            |RAVEntry(nmtel) ->
    //                mtel.[..i-1]@[RAVEntry(getnmtel cirs.Tail nmtel)]@mtel.[i+1..]
    //            |_ -> failwith "should be RAV"
    //    let nmtel = getnmtel iirs gm.MoveText
    //    {gm with MoveText=nmtel}
