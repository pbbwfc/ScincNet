namespace FsChess.WinForms

open System.Windows.Forms
open FsChess

[<AutoOpen>]
module WbStatsLib =
    type WbStats() as stats =
        inherit WebBrowser(AllowWebBrowserDrop = false,IsWebBrowserContextMenuEnabled = false,WebBrowserShortcutsEnabled = false)

        //mutables
        let mutable mvsts = new ResizeArray<ScincFuncs.mvstats>()
        let mutable tsts = new ScincFuncs.totstats()
        let mutable fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
        let mutable basenm = ""
        let mutable basenum = -1

        //events
        let mvselEvt = new Event<_>()

        //functions
        let nl = System.Environment.NewLine
        let hdr() = 
            "<html><body>" + nl +
            "<h4>Tree - " + basenm + "</h4>" + nl +
            "<table style=\"width:100%;border-collapse: collapse;\">" + nl

        let ftr = 
            "</table>" + nl +
            "</body></html>" + nl

        let getdiv ww dr bw =
            let sww = if ww+dr+bw=0 then "0" else ((80*ww)/(ww+dr+bw)).ToString()
            let sdr = if ww+dr+bw=0 then "0" else ((80*dr)/(ww+dr+bw)).ToString()
            let sbw = if ww+dr+bw=0 then "0" else ((80*bw)/(ww+dr+bw)).ToString()
            let wwd = "<span style=\"border-top: 1px solid black;border-left: 1px solid black;border-bottom: 1px solid black;background-color:white;height:18px;width:" + sww + "px\"></span>" 
            let drd = "<span style=\"border-top: 1px solid black;border-bottom: 1px solid black;background-color:gray;height:18px;width:" + sdr + "px\"></span>"
            let bwd = "<span style=\"border-top: 1px solid black;border-right: 1px solid black;border-bottom: 1px solid black;background-color:black;height:18px;width:" + sbw + "px\"></span>"
            wwd + drd + bwd
        
        let mvsttag i (mvst:ScincFuncs.mvstats) =  
            "<tr id=\"" + i.ToString() + "\"><td>" + mvst.Mvstr + "</td><td>" + mvst.Count.ToString() + "</td>" + 
            "<td>" + mvst.Freq.ToString("##0.0%") + "</td><td>" + (getdiv mvst.WhiteWins mvst.Draws mvst.BlackWins) + "</td>" + 
            "<td>" + mvst.Score.ToString("##0.0%") + "</td><td>" + mvst.DrawPc.ToString("##0.0%") +
            "</td><td>" + mvst.AvElo.ToString() + "</td><td>" + mvst.Perf.ToString() +
            "</td><td>" + mvst.AvYear.ToString() + "</td><td>" + mvst.ECO +
            "</td></tr>" + nl

        let bdsttags() = 
            if mvsts.Count=0 then hdr()+ftr
            else
                hdr() +
                "<tr><th style=\"text-align: left;\">Move</th><th style=\"text-align: left;\">Count</th>" +
                "<th style=\"text-align: left;\">Percent</th><th style=\"text-align: left;\">Results</th>" + 
                "<th style=\"text-align: left;\">Score</th><th style=\"text-align: left;\">DrawPc</th>" +
                "<th style=\"text-align: left;\">AvElo</th>" + "<th style=\"text-align: left;\">Perf</th>" +
                "<th style=\"text-align: left;\">AvYear</th>" + "<th style=\"text-align: left;\">ECO</th>" +
                "</tr>" + nl + 
                (mvsts|>Seq.mapi mvsttag|>Seq.reduce(+)) +
                "<tr><td style=\"border-top: 1px solid black;\"></td><td style=\"border-top: 1px solid black;\">" + 
                tsts.TotCount.ToString() + "</td><td style=\"border-top: 1px solid black;\">" + tsts.TotFreq.ToString("##0.0%") + "</td>" +
                "<td style=\"border-top: 1px solid black;\">" + (getdiv tsts.TotWhiteWins tsts.TotDraws tsts.TotBlackWins) + "</td><td style=\"border-top: 1px solid black;\">" + 
                tsts.TotScore.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" + 
                tsts.TotDrawPc.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" +  
                tsts.TotAvElo.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                tsts.TotPerf.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                tsts.TotAvYear.ToString() + "</td><td style=\"border-top: 1px solid black;\">" +
                "</td></tr>" + nl
                + ftr

        let onclick(el:HtmlElement) = 
            let i = el.Id|>int
            let san = mvsts.[i].Mvstr
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
            tsts <- new ScincFuncs.totstats()
            mvsts.Clear()
            if ScincFuncs.Tree.Search(&mvsts, &tsts, fen, basenum)=0 then
                stats.DocumentText <- bdsttags()

        member stats.UpdateFen(bd:Brd) =
            fen <- bd|>Board.ToStr
            stats.Refrsh()

        member ststat.Init(nm:string, num:int) =
            basenm <- nm
            basenum <- num
            stats.Refrsh()

        member ststat.Close() =
            basenm <- ""
            basenum <- -1
            fen <- "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
            mvsts.Clear()
            stats.DocumentText <- bdsttags()
        
        member ststat.BaseNum() =
            basenum
        
        //publish
        ///Provides the selected move in SAN format
        member __.MvSel = mvselEvt.Publish
