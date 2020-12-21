namespace FsChess.WinForms

open System.Windows.Forms

[<AutoOpen>]
module WbStatsLib =
    type MvStats =
        {
            Mvstr : string
            Count : int
            Pc : float
            WhiteWins : int 
            Draws : int 
            BlackWins :int
            Score : float
            DrawPc : float
            AvElo : int
            Perf : int
            AvYear : int
            ECO : string
        }
    
    type BrdStats = 
        {
            Mvstats : MvStats list
            TotCount : int
            Pc : float
            TotWhiteWins : int 
            TotDraws : int 
            TotBlackWins :int
            TotScore : float
            TotDrawPc : float
            TotAvElo : int
            TotPerf : int
            TotAvYear : int
        }

    let BrdStatsEMP = 
        {
            Mvstats = []
            TotCount = 0
            Pc = 0.0
            TotWhiteWins = 0 
            TotDraws = 0
            TotBlackWins = 0
            TotScore = 0.0
            TotDrawPc = 0.0
            TotAvElo = 0
            TotPerf = 0
            TotAvYear = 0
        }

    type WbStats() as stats =
        inherit WebBrowser(AllowWebBrowserDrop = false,IsWebBrowserContextMenuEnabled = false,WebBrowserShortcutsEnabled = false)

        //mutables
        let mutable cbdst = BrdStatsEMP

        //events
        let mvselEvt = new Event<_>()

        //functions
        let nl = System.Environment.NewLine
        let hdr = 
            "<html><body>" + nl +
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
        
        let mvsttag i (mvst:MvStats) =  
            "<tr id=\"" + i.ToString() + "\"><td>" + mvst.Mvstr + "</td><td>" + mvst.Count.ToString() + "</td>" + 
            "<td>" + mvst.Pc.ToString("##0.0%") + "</td><td>" + (getdiv mvst.WhiteWins mvst.Draws mvst.BlackWins) + "</td>" + 
            "<td>" + mvst.Score.ToString("##0.0%") + "</td><td>" + mvst.DrawPc.ToString("##0.0%") +
            "</td><td>" + mvst.AvElo.ToString() +
            "</td></tr>" + nl

        let bdsttags() = 
            let mvsts = cbdst.Mvstats
            if mvsts.IsEmpty then hdr+ftr
            else
                hdr +
                "<tr><th style=\"text-align: left;\">Move</th><th style=\"text-align: left;\">Count</th>" +
                "<th style=\"text-align: left;\">Percent</th><th style=\"text-align: left;\">Results</th>" + 
                "<th style=\"text-align: left;\">Score</th><th style=\"text-align: left;\">DrawPc</th>" +
                "<th style=\"text-align: left;\">AvElo</th>" +
                "</tr>" + nl + 
                (mvsts|>List.mapi mvsttag|>List.reduce(+)) +
                "<tr><td style=\"border-top: 1px solid black;\"></td><td style=\"border-top: 1px solid black;\">" + 
                cbdst.TotCount.ToString() + "</td><td style=\"border-top: 1px solid black;\">100.0%</td>" +
                "<td style=\"border-top: 1px solid black;\">" + (getdiv cbdst.TotWhiteWins cbdst.TotDraws cbdst.TotBlackWins) + "</td><td style=\"border-top: 1px solid black;\">" + 
                cbdst.TotScore.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" + 
                cbdst.TotDrawPc.ToString("##0.0%") + "</td><td style=\"border-top: 1px solid black;\">" +  
                cbdst.TotAvElo.ToString() +
                "</td></tr>" + nl
                + ftr


        let onclick(el:HtmlElement) = 
            let i = el.Id|>int
            let san = cbdst.Mvstats.[i].Mvstr
            san|>mvselEvt.Trigger

        let setclicks e = 
             for el in stats.Document.GetElementsByTagName("tr") do
                 el.Click.Add(fun _ -> onclick(el))

        do
            stats.DocumentText <- bdsttags()
            stats.DocumentCompleted.Add(setclicks)
            stats.ObjectForScripting <- stats


        ///Sets the Stats to be displayed
        member stats.SetStats(sts:BrdStats) = 
            cbdst <- sts
            stats.DocumentText <- bdsttags()
  
        ///Calculates the Stats to be displayed
        //member stats.CalcStats(fgms:(int * Game * string) list) = 
        //    cbdst <- fgms|>Stats.Get
        //    stats.DocumentText <- bdsttags()

        //Refresh the stats after board change
        member stats.Refrsh() =
            let mutable statsstr = "" 
            if ScincFuncs.Tree.Search(&statsstr)=0 then
                let lns = statsstr.Split('\n')
                let mvlns = lns.[0..lns.Length-2]
                let totln = lns.[lns.Length-1]
                let doln (ln:string) =
                    let bits = ln.Split('|')
                    let mv = bits.[1].Trim()
                    let nm = int(bits.[2].Trim())
                    let freq = float(bits.[3].Trim())/100.0
                    let scr = float(bits.[4].Trim())/100.0
                    let pctDraws = float(bits.[5].Trim())/100.0
                    let avgElo = int(bits.[6].Trim())
                    let perf = int(bits.[7].Trim())
                    let avgYear = int(bits.[8].Trim())
                    let ecoStr = bits.[9].Trim()
                    let ww = nm * int(100.0 * scr - 50.0 * pctDraws)
                    let dw = nm * int(100.0 * pctDraws)
                    let bw = nm * int(100.0 - 100.0 * scr - 50.0 * pctDraws)
                    { Mvstr = mv; Count = nm; Pc = freq; WhiteWins = ww; Draws = dw; BlackWins = bw; Score = scr; DrawPc = pctDraws; AvElo = avgElo; Perf = perf; AvYear = avgYear; ECO = ecoStr }
                let tots = totln.Split('|')
                let totnm = int(tots.[0].Trim())
                let totscr = float(tots.[1].Trim())/100.0
                let totpctDraws = float(tots.[2].Trim())/100.0
                let totavgElo = int(tots.[3].Trim())
                let totperf = int(tots.[4].Trim())
                let totavgYear = int(tots.[5].Trim())
                let mvsts = mvlns|>Array.map doln|>List.ofArray
                let ww = totnm * int(100.0 * totscr - 50.0 * totpctDraws)
                let dw = totnm * int(100.0 * totpctDraws)
                let bw = totnm * int(100.0 - 100.0 * totscr - 50.0 * totpctDraws)
                cbdst <- {Mvstats=mvsts;TotCount=totnm;Pc=100.0;TotWhiteWins=ww;TotDraws=dw;TotBlackWins=bw;TotScore=totscr;TotDrawPc=totpctDraws;TotAvElo=totavgElo;TotPerf=totperf;TotAvYear=totavgYear}
                stats.DocumentText <- bdsttags()
      
        //publish
        ///Provides the selected move in SAN format
        member __.MvSel = mvselEvt.Publish
