namespace FsChess.WinForms

open System.Drawing
open System.Windows.Forms
open FsChess
open System.IO

module Eng = 
    ///send message to engine
    let Send(command:string, prc : System.Diagnostics.Process) = 
        prc.StandardInput.WriteLine(command)
    
    ///set up engine
    let ComputeAnswer(fen, depth, prc) = 
        Send("ucinewgame", prc)
        Send("setoption name Threads value " + (System.Environment.ProcessorCount - 1).ToString(), prc)
        Send("position startpos", prc)
        Send("position fen " + fen + " ", prc)
        Send("go depth " + depth.ToString(), prc)
    
    ///set up process
    let SetUpPrc (prc : System.Diagnostics.Process) = 
        prc.StartInfo.CreateNoWindow <- true
        prc.StartInfo.FileName <- "stockfish.exe"
        prc.StartInfo.WorkingDirectory <- Path.GetDirectoryName
                                              (System.Reflection.Assembly.GetExecutingAssembly().Location)
        prc.StartInfo.RedirectStandardOutput <- true
        prc.StartInfo.UseShellExecute <- false
        prc.StartInfo.RedirectStandardInput <- true
        prc.StartInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
        prc.Start() |> ignore
        prc.BeginOutputReadLine()
    
    ///Gets the Score and Line from a message
    let GetScrLn(msg:string,bd:Brd) =
        if msg.StartsWith("info") then
            let ln = 
                let st = msg.LastIndexOf("pv")
                let ucis = msg.Substring(st+2)
                //need to change to SAN format
                let bits = ucis.Trim().Split([|' '|])//|>Convert.UcisToSans bd mno
                bits.[0]|>FsChessPgn.MoveUtil.fromUci bd|>FsChessPgn.MoveUtil.toPgn bd
                
                //let sanstr = ucis|>Convert.UcisToSans bd mno

                //sanstr
            let scr =
                let st = msg.LastIndexOf("cp")
                let ss = msg.Substring(st+2,10).Trim()
                let bits = ss.Split([|' '|])
                let cp = float(bits.[0])/100.0
                cp.ToString("0.00")

            scr,ln
        else
            "0.00",msg


type State() =
    let mutable isanl = false
    let mutable procp = new System.Diagnostics.Process()
    //anl
    let apchngEvt = new Event<_>()
    let ahpchngEvt = new Event<_>()
    let apmsgEvt = new Event<_>()

    //anl
    member x.AnlpStart(cbd) = 
        procp <- new System.Diagnostics.Process()
        
        //p_out
        let pOut (e : System.Diagnostics.DataReceivedEventArgs) = 
            if not (e.Data = null || e.Data = "") then 
                let msg = e.Data.ToString().Trim()
                if not (msg.StartsWith("info") && not (msg.Contains(" cp "))) then 
                    let scr,ln = (msg,cbd)|>Eng.GetScrLn
                    (scr,ln) |> apmsgEvt.Trigger
        procp.OutputDataReceived.Add(pOut)
        //Start process
        Eng.SetUpPrc(procp)
        // call calcs
        // need to send game position moves as UCI
        let fen = cbd|>Board.ToStr
        Eng.ComputeAnswer(fen, 99, procp)
        isanl <- true
        isanl |> apchngEvt.Trigger
        ("Stockfish is calculating...") |> ahpchngEvt.Trigger

    member x.AnlpStop() = 
        if procp <> null then procp.Kill()
        isanl <- false
        isanl |> apchngEvt.Trigger
        "Stockfish has stopped" |> ahpchngEvt.Trigger
    
    //anl
    member x.AnlpChng = apchngEvt.Publish
    member x.AnlpHeadChng = ahpchngEvt.Publish
    member x.AnlpMsg = apmsgEvt.Publish




[<AutoOpen>]
module PnlAnlLib =
    type PnlAnl() as this =
        inherit Panel(Width = 400)
        let sst = new State()
        let pnlt = new Panel(Dock = DockStyle.Top, Height = 30)
        let albl = 
            new Label(Dock = DockStyle.Top, Text = "Engine Stopped", 
                      TextAlign = ContentAlignment.BottomLeft)
        let sbtn = 
            new System.Windows.Forms.Button(Text = "Start", 
                                            Dock = DockStyle.Right)
        let dg = 
            new WebBrowser(AllowWebBrowserDrop = false,IsWebBrowserContextMenuEnabled = false,WebBrowserShortcutsEnabled = false,Dock=DockStyle.Fill)

        let nl = System.Environment.NewLine
        let hdr = 
            "<html><body>" + nl +
            "<table style=\"width:100%;border-collapse: collapse;\">" + nl +
            "<tr><th style=\"text-align: left;\">Score</th><th style=\"text-align: left;\">Line</th>" + nl

        let ftr = 
            "</table>" + nl +
            "</body></html>" + nl
  
        //set up analysis
        let setAnl start = 
            
            let setanl() = 
                if start then 
                    dg.DocumentText <- hdr + ftr
            if (this.InvokeRequired) then 
                try 
                    this.Invoke(MethodInvoker(fun () -> setanl())) |> ignore
                with _ -> ()
            else setanl()
    
        //set header
        let setHdr msg = 
            if (this.InvokeRequired) then 
                try 
                    this.Invoke(MethodInvoker(fun () -> albl.Text <- msg)) 
                    |> ignore
                with _ -> ()
            else albl.Text <- msg
    
        //add Message
        let addMsg (scr,ln) = 
            let addmsg() = 
                let nr = "<tr><td style='width: 50px;'>" + scr.ToString() + "</td><td>" + ln + "</td></tr>" + nl
                
                dg.DocumentText <- hdr + nr + ftr
                
            if (this.InvokeRequired) then 
                try 
                    this.Invoke(MethodInvoker(fun () -> addmsg())) |> ignore
                with _ -> ()
            else addmsg()

        //start or stop
        let startstop() =
            if sbtn.Text="Start" then
                sbtn.Text<-"Stop"
                sst.AnlpStart(FsChess.Board.Start)
            else
                sbtn.Text<-"Start"
                sst.AnlpStop()
    
        do 
            this.Controls.Add(dg)
            pnlt.Controls.Add(albl)
            pnlt.Controls.Add(sbtn)
            this.Controls.Add(pnlt)
            setAnl(true)
            //events
            sst.AnlpChng |> Observable.add setAnl
            sst.AnlpHeadChng |> Observable.add setHdr
            sst.AnlpMsg |> Observable.add addMsg
            sbtn.Click.Add(fun _ -> startstop())
 
