namespace FsChess.WinForms

open System.Windows.Forms
open System.IO
open FsChess

[<AutoOpen>]
module WbStatsLib =
    type WbStats() as stats =
        inherit WebBrowser(AllowWebBrowserDrop = false,IsWebBrowserContextMenuEnabled = false,WebBrowserShortcutsEnabled = false)

        //mutables
        let mutable mvsts = new ResizeArray<ScincFuncs.mvstats>()
        let mutable tsts = new ScincFuncs.totstats()
        let mutable fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
        let mutable mvsts1 = new ResizeArray<mvstats>()
        let mutable tsts1 = new totstats()
        let mutable bdstr = "RNBQKBNRPPPPPPPP................................pppppppprnbqkbnr w"
        let mutable isstatic = false
        
        let mutable isw = true
        let mutable basenm = ""
        let mutable basenum = -1
        let mutable blkld = false
        let mutable whtld = false
        let sans = Array.create 99 ""

        //events
        let mvselEvt = new Event<_>()

        //functions
        let nl = System.Environment.NewLine
        let hdr() = 
            let hdrstr = 
                if isstatic then
                    "Static Tree - " + Path.GetFileNameWithoutExtension(basenm)
                else "Tree - " + basenm      
            "<html>" + nl +
            "<head>" + nl +
            "<style>" + nl +
            ".cell.isboth" + nl +
            "{" + nl +
            "background-color:grey;color:white;" + nl +
            "}" + nl +
            ".cell.isbrep" + nl +
            "{" + nl +
            "background-color:black;color:white;" + nl +
            "}" + nl +
            ".cell.iswrep" + nl +
            "{" + nl +
            "background-color:white;" + nl +
            "}" + nl +
            "td" + nl +
            "{" + nl +
            "background-color:lightgray;" + nl +
            "}" + nl +
            "</style>" + nl +
            "</head>" + nl +
            "<body>" + nl +
            "<h4>" + hdrstr + "</h4>" + nl +
            "<table style=\"width:100%;border-collapse: collapse;\">" + nl

        let ftr = 
            "</table>" + nl +
            "</body></html>" + nl

        let getdiv ww dr bw =
            let sww = if ww+dr+bw=0L then "0" else ((80L*ww)/(ww+dr+bw)).ToString()
            let sdr = if ww+dr+bw=0L then "0" else ((80L*dr)/(ww+dr+bw)).ToString()
            let sbw = if ww+dr+bw=0L then "0" else ((80L*bw)/(ww+dr+bw)).ToString()
            let wwd = "<span style=\"border-top: 1px solid black;border-left: 1px solid black;border-bottom: 1px solid black;background-color:white;height:18px;width:" + sww + "px\"></span>" 
            let drd = "<span style=\"border-top: 1px solid black;border-bottom: 1px solid black;background-color:gray;height:18px;width:" + sdr + "px\"></span>"
            let bwd = "<span style=\"border-top: 1px solid black;border-right: 1px solid black;border-bottom: 1px solid black;background-color:black;height:18px;width:" + sbw + "px\"></span>"
            wwd + drd + bwd
        
        let mvstags() =
            if blkld then Repertoire.LoadBlack()
            if whtld then Repertoire.LoadWhite()
            let bopts = 
                if isw && blkld then
                    let ro = (Repertoire.Black()|>fst) 
                    if ro.ContainsKey(fen) then ro.[fen] else []
                else []
            let bmov =
                if (not isw) && blkld then
                    let rm = (Repertoire.Black()|>snd) 
                    if rm.ContainsKey(fen) then rm.[fen]|>Some else None
                else None
            let wopts = 
                if (not isw) && whtld then
                    let ro = (Repertoire.White()|>fst) 
                    if ro.ContainsKey(fen) then ro.[fen] else []
                else []
            let wmov =
                if isw && whtld then
                    let rm = (Repertoire.White()|>snd) 
                    if rm.ContainsKey(fen) then rm.[fen]|>Some else None
                else None

            let mvsttag i (mvst:ScincFuncs.mvstats) isbrep iswrep nag comments =  
                sans.[i] <- mvst.Mvstr
                let tdstyle = 
                    if isbrep&&iswrep then "<td class=\"isboth\">" 
                    elif isbrep then "<td class=\"isbrep\">"
                    elif iswrep then "<td class=\"iswrep\">"
                    else "<td>"
                
                "<tr id=\"" + i.ToString() + "\">" + tdstyle + mvst.Mvstr + nag + "</td><td>" + mvst.Count.ToString() + "</td>" + 
                "<td>" + mvst.Freq.ToString("##0.0%") + "</td><td>" + (getdiv (int64(mvst.WhiteWins)) (int64(mvst.Draws)) (int64(mvst.BlackWins))) + "</td>" + 
                "<td>" + mvst.Score.ToString("##0.0%") + "</td><td>" + mvst.DrawPc.ToString("##0.0%") +
                "</td><td>" + mvst.AvElo.ToString() + "</td><td>" + mvst.Perf.ToString() +
                "</td><td>" + mvst.AvYear.ToString() + "</td><td>" + mvst.ECO +
                "</td>" + tdstyle + comments + "</td></tr>" + nl
            let addrep i (mvst:ScincFuncs.mvstats) =
                if isw then
                    //check if move is in bopts
                    let isbrep,bnag,bcomment = 
                        if Repertoire.OptsHaveSan mvst.Mvstr bopts then
                            let rol = bopts|>List.filter(fun r -> r.San=mvst.Mvstr)
                            let ro = rol.Head
                            true,ro.Nag,ro.Comm
                        else false,NAG.Null,""
                    let iswrep,wnag,wcomment = 
                        if wmov.IsSome && wmov.Value.San=mvst.Mvstr then
                            true,wmov.Value.Nag,wmov.Value.Comm
                        else false,NAG.Null,""
                    let nagstr = 
                        if isbrep&&iswrep then
                            if wnag=NAG.Null then (bnag|>Game.NAGStr) else (wnag|>Game.NAGStr)
                        elif iswrep then (wnag|>Game.NAGStr)
                        else (bnag|>Game.NAGStr)
                    let comment = 
                        if isbrep&&iswrep then
                            if wcomment="" then bcomment else wcomment
                        elif iswrep then wcomment
                        else bcomment
                    mvsttag i mvst isbrep iswrep nagstr comment
                else
                    let isbrep,bnag,bcomment = 
                        if bmov.IsSome && bmov.Value.San=mvst.Mvstr then
                            true,bmov.Value.Nag,bmov.Value.Comm
                        else false,NAG.Null,""
                    let iswrep,wnag,wcomment = 
                        if Repertoire.OptsHaveSan mvst.Mvstr wopts then
                            let rol = wopts|>List.filter(fun r -> r.San=mvst.Mvstr)
                            let ro = rol.Head
                            true,ro.Nag,ro.Comm
                        else false,NAG.Null,""
                    let nagstr = 
                        if isbrep&&iswrep then
                            if wnag=NAG.Null then (bnag|>Game.NAGStr) else (wnag|>Game.NAGStr)
                        elif iswrep then (wnag|>Game.NAGStr)
                        else (bnag|>Game.NAGStr)
                    let comment = 
                        if isbrep&&iswrep then
                            if wcomment="" then bcomment else wcomment
                        elif iswrep then wcomment
                        else bcomment
                    mvsttag i mvst isbrep iswrep nagstr comment
            
            let notinmvsts (ro:RepOpt) =
                let filt = mvsts|>Seq.filter(fun m -> m.Mvstr = ro.San)|>Seq.toList
                filt.Length=0
            
            let doextras (fi) =
                let doextraw i ro =
                    sans.[i+fi] <- ro.San 
                    
                    "<tr id=\"" + (i+fi).ToString() + "\">" + "<td class=\"iswrep\">" + ro.San + (ro.Nag|>Game.NAGStr) + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" + "</td><td class=\"iswrep\">" + ro.Comm +
                    "</td></tr>" + nl
                let doextrab i ro =
                    sans.[i+fi] <- ro.San

                    "<tr id=\"" + (i+fi).ToString() + "\">" + "<td class=\"isbrep\">" + ro.San + (ro.Nag|>Game.NAGStr) + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" + "</td><td class=\"isbrep\">" + ro.Comm +
                    "</td></tr>" + nl
                let extraws,extrabs = 
                    if isw then
                        //filter bopts not in mvsts
                        let exbs = bopts|>List.filter notinmvsts
                        //now add white if not included
                        let exws = if wmov.IsSome && notinmvsts(wmov.Value) then [wmov.Value] else []
                        exws,exbs
                    else
                        //filter wopts not in mvsts
                        let exws = wopts|>List.filter notinmvsts
                        //now add black if not included
                        let exbs = if bmov.IsSome && notinmvsts(bmov.Value) then [bmov.Value] else []
                        exws,exbs
                let exwrws = if extraws.Length>0 then (extraws|>List.mapi doextraw|>List.reduce(+)) + nl else ""
                let exbrws = if extrabs.Length>0 then (extrabs|>List.mapi doextrab|>List.reduce(+)) + nl else ""
                exwrws + exbrws
            
            let mnrws = if mvsts.Count>0 then mvsts|>Seq.mapi addrep|>Seq.reduce(+) else ""
            mnrws + doextras(mvsts.Count)
            
        let bdsttags() = 
            if mvsts.Count=0 && (not blkld) && (not whtld) then hdr()+ftr
            else
                hdr() +
                "<tr><th style=\"text-align: left;\">Move</th><th style=\"text-align: left;\">Count</th>" +
                "<th style=\"text-align: left;\">Percent</th><th style=\"text-align: left;\">Results</th>" + 
                "<th style=\"text-align: left;\">Score</th><th style=\"text-align: left;\">DrawPc</th>" +
                "<th style=\"text-align: left;\">AvElo</th>" + "<th style=\"text-align: left;\">Perf</th>" +
                "<th style=\"text-align: left;\">AvYear</th>" + "<th style=\"text-align: left;\">ECO</th>" +
                "<th style=\"text-align: left;\">Comment</th>" + "</tr>" + nl + 
                (mvstags()) +
                "<tr><td style=\"border-top: 1px solid black;\"></td><td style=\"border-top: 1px solid black;\">" + 
                tsts.TotCount.ToString() + "</td><td style=\"border-top: 1px solid black;\">" + tsts.TotFreq.ToString("##0.0%") + "</td>" +
                "<td style=\"border-top: 1px solid black;\">" + (getdiv (int64(tsts.TotWhiteWins)) (int64(tsts.TotDraws)) (int64(tsts.TotBlackWins))) + "</td><td style=\"border-top: 1px solid black;\">" + 
                tsts.TotScore.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" + 
                tsts.TotDrawPc.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" +  
                tsts.TotAvElo.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                tsts.TotPerf.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                tsts.TotAvYear.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                "</td><td style=\"border-top: 1px solid black;\">" + "</td></tr>" + nl
                + ftr

        let mvstags1() =
            if blkld then Repertoire.LoadBlack()
            if whtld then Repertoire.LoadWhite()
            let bopts = 
                if isw && blkld then
                    let ro = (Repertoire.Black()|>fst) 
                    if ro.ContainsKey(fen) then ro.[fen] else []
                else []
            let bmov =
                if (not isw) && blkld then
                    let rm = (Repertoire.Black()|>snd) 
                    if rm.ContainsKey(fen) then rm.[fen]|>Some else None
                else None
            let wopts = 
                if (not isw) && whtld then
                    let ro = (Repertoire.White()|>fst) 
                    if ro.ContainsKey(fen) then ro.[fen] else []
                else []
            let wmov =
                if isw && whtld then
                    let rm = (Repertoire.White()|>snd) 
                    if rm.ContainsKey(fen) then rm.[fen]|>Some else None
                else None

            let mvsttag i (mvst:mvstats) isbrep iswrep nag comments =  
                sans.[i] <- mvst.Mvstr
                let tdstyle = 
                    if isbrep&&iswrep then "<td class=\"isboth\">" 
                    elif isbrep then "<td class=\"isbrep\">"
                    elif iswrep then "<td class=\"iswrep\">"
                    else "<td>"
                
                "<tr id=\"" + i.ToString() + "\">" + tdstyle + mvst.Mvstr + nag + "</td><td>" + mvst.Count.ToString() + "</td>" + 
                "<td>" + mvst.Freq.ToString("##0.0%") + "</td><td>" + (getdiv mvst.WhiteWins mvst.Draws mvst.BlackWins) + "</td>" + 
                "<td>" + mvst.Score.ToString("##0.0%") + "</td><td>" + mvst.DrawPc.ToString("##0.0%") +
                "</td><td>" + mvst.AvElo.ToString() + "</td><td>" + mvst.Perf.ToString() +
                "</td><td>" + mvst.AvYear.ToString() + 
                "</td>" + tdstyle + comments + "</td></tr>" + nl
            let addrep i (mvst:mvstats) =
                if isw then
                    //check if move is in bopts
                    let isbrep,bnag,bcomment = 
                        if Repertoire.OptsHaveSan mvst.Mvstr bopts then
                            let rol = bopts|>List.filter(fun r -> r.San=mvst.Mvstr)
                            let ro = rol.Head
                            true,ro.Nag,ro.Comm
                        else false,NAG.Null,""
                    let iswrep,wnag,wcomment = 
                        if wmov.IsSome && wmov.Value.San=mvst.Mvstr then
                            true,wmov.Value.Nag,wmov.Value.Comm
                        else false,NAG.Null,""
                    let nagstr = 
                        if isbrep&&iswrep then
                            if wnag=NAG.Null then (bnag|>Game.NAGStr) else (wnag|>Game.NAGStr)
                        elif iswrep then (wnag|>Game.NAGStr)
                        else (bnag|>Game.NAGStr)
                    let comment = 
                        if isbrep&&iswrep then
                            if wcomment="" then bcomment else wcomment
                        elif iswrep then wcomment
                        else bcomment
                    mvsttag i mvst isbrep iswrep nagstr comment
                else
                    let isbrep,bnag,bcomment = 
                        if bmov.IsSome && bmov.Value.San=mvst.Mvstr then
                            true,bmov.Value.Nag,bmov.Value.Comm
                        else false,NAG.Null,""
                    let iswrep,wnag,wcomment = 
                        if Repertoire.OptsHaveSan mvst.Mvstr wopts then
                            let rol = wopts|>List.filter(fun r -> r.San=mvst.Mvstr)
                            let ro = rol.Head
                            true,ro.Nag,ro.Comm
                        else false,NAG.Null,""
                    let nagstr = 
                        if isbrep&&iswrep then
                            if wnag=NAG.Null then (bnag|>Game.NAGStr) else (wnag|>Game.NAGStr)
                        elif iswrep then (wnag|>Game.NAGStr)
                        else (bnag|>Game.NAGStr)
                    let comment = 
                        if isbrep&&iswrep then
                            if wcomment="" then bcomment else wcomment
                        elif iswrep then wcomment
                        else bcomment
                    mvsttag i mvst isbrep iswrep nagstr comment
            
            let notinmvsts (ro:RepOpt) =
                let filt = mvsts1|>Seq.filter(fun m -> m.Mvstr = ro.San)|>Seq.toList
                filt.Length=0
            
            let doextras (fi) =
                let doextraw i ro =
                    sans.[i+fi] <- ro.San 
                    
                    "<tr id=\"" + (i+fi).ToString() + "\">" + "<td class=\"iswrep\">" + ro.San + (ro.Nag|>Game.NAGStr) + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" + "</td><td class=\"iswrep\">" + ro.Comm +
                    "</td></tr>" + nl
                let doextrab i ro =
                    sans.[i+fi] <- ro.San

                    "<tr id=\"" + (i+fi).ToString() + "\">" + "<td class=\"isbrep\">" + ro.San + (ro.Nag|>Game.NAGStr) + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" + "</td>" + 
                    "<td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" +
                    "</td><td>" + "</td><td>" + "</td><td class=\"isbrep\">" + ro.Comm +
                    "</td></tr>" + nl
                let extraws,extrabs = 
                    if isw then
                        //filter bopts not in mvsts
                        let exbs = bopts|>List.filter notinmvsts
                        //now add white if not included
                        let exws = if wmov.IsSome && notinmvsts(wmov.Value) then [wmov.Value] else []
                        exws,exbs
                    else
                        //filter wopts not in mvsts
                        let exws = wopts|>List.filter notinmvsts
                        //now add black if not included
                        let exbs = if bmov.IsSome && notinmvsts(bmov.Value) then [bmov.Value] else []
                        exws,exbs
                let exwrws = if extraws.Length>0 then (extraws|>List.mapi doextraw|>List.reduce(+)) + nl else ""
                let exbrws = if extrabs.Length>0 then (extrabs|>List.mapi doextrab|>List.reduce(+)) + nl else ""
                exwrws + exbrws
            
            let mnrws = if mvsts1.Count>0 then mvsts1|>Seq.mapi addrep|>Seq.reduce(+) else ""
            mnrws + doextras(mvsts1.Count)
            
        let bdsttags1() = 
            if mvsts1.Count=0 && (not blkld) && (not whtld) then hdr()+ftr
            else
                hdr() +
                "<tr><th style=\"text-align: left;\">Move</th><th style=\"text-align: left;\">Count</th>" +
                "<th style=\"text-align: left;\">Percent</th><th style=\"text-align: left;\">Results</th>" + 
                "<th style=\"text-align: left;\">Score</th><th style=\"text-align: left;\">DrawPc</th>" +
                "<th style=\"text-align: left;\">AvElo</th>" + "<th style=\"text-align: left;\">Perf</th>" +
                "<th style=\"text-align: left;\">AvYear</th>" +
                "<th style=\"text-align: left;\">Comment</th>" + "</tr>" + nl + 
                (mvstags1()) +
                "<tr><td style=\"border-top: 1px solid black;\"></td><td style=\"border-top: 1px solid black;\">" + 
                tsts1.TotCount.ToString() + "</td><td style=\"border-top: 1px solid black;\">" + tsts1.TotFreq.ToString("##0.0%") + "</td>" +
                "<td style=\"border-top: 1px solid black;\">" + (getdiv tsts1.TotWhiteWins tsts1.TotDraws tsts1.TotBlackWins) + "</td><td style=\"border-top: 1px solid black;\">" + 
                tsts1.TotScore.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" + 
                tsts1.TotDrawPc.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" +  
                tsts1.TotAvElo.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                tsts1.TotPerf.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                tsts1.TotAvYear.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                "</td><td style=\"border-top: 1px solid black;\">" + "</td></tr>" + nl
                + ftr


        
        let onclick(el:HtmlElement) = 
            let i = el.Id|>int
            let san = sans.[i]
            san|>mvselEvt.Trigger

        let setclicks e = 
             for el in stats.Document.GetElementsByTagName("tr") do
                 el.Click.Add(fun _ -> onclick(el))

        do
            stats.DocumentText <- bdsttags()
            stats.DocumentCompleted.Add(setclicks)
            stats.ObjectForScripting <- stats

        ///Refresh the stats after board change
        member stats.Refrsh() =
            if isstatic then
                let sts = StaticTree.Read(bdstr,basenm + "_FILES")
                mvsts1<-sts.MvsStats
                tsts1 <-sts.TotStats
                stats.DocumentText <- bdsttags1()
            else
                tsts <- new ScincFuncs.totstats()
                mvsts.Clear()
                if basenum<> -1 && ScincFuncs.Tree.Search(&mvsts, &tsts, fen, basenum)=0 then
                    stats.DocumentText <- bdsttags()

        member stats.UpdateFen(bd:Brd) =
            isw <- bd.WhosTurn=Player.White
            fen <- bd|>Board.ToStr
            bdstr <- bd|>Board.ToSimpleStr
            stats.Refrsh()

        member stats.Init(nm:string, num:int) =
            basenm <- nm
            basenum <- num
            isstatic <- false
        
        member stats.InitStatic(nm:string) =
            basenm <- nm
            basenum <- 99
            isstatic <- true

        member stats.Close() =
            basenm <- ""
            basenum <- -1
            fen <- "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
            bdstr <- "RNBQKBNRPPPPPPPP................................pppppppprnbqkbnr w"
            isw <- true
            isstatic <- false
            mvsts.Clear()
            stats.DocumentText <- bdsttags()
        
        member stats.BaseNum() =
            basenum

        member stats.LoadWhiteRep(shw) =
            whtld <- shw
            stats.DocumentText <- bdsttags()
        
        member stats.LoadBlackRep(shb) =
            blkld <- shb
            stats.DocumentText <- bdsttags()
        
        //publish
        ///Provides the selected move in SAN format
        member __.MvSel = mvselEvt.Publish
