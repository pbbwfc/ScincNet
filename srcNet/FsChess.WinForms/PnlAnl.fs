namespace FsChess.WinForms

open System.Drawing
open System.Windows.Forms

[<AutoOpen>]
module PnlAnlLib =
    type PnlAnl() as this =
        inherit Panel(Width = 400)
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
                let nr = "<tr><td style='width: 50px;'>" + scr + "</td><td>" + ln + "</td></tr>" + nl
                
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
                //sst.AnlpStart()
            else
                sbtn.Text<-"Start"
                //sst.AnlpStop()
    
        do 
            this.Controls.Add(dg)
            pnlt.Controls.Add(albl)
            pnlt.Controls.Add(sbtn)
            this.Controls.Add(pnlt)
            setAnl(true)
            //events
            //sst.AnlpChng |> Observable.add setAnl
            //sst.AnlpHeadChng |> Observable.add setHdr
            //sst.AnlpMsg |> Observable.add addMsg
            sbtn.Click.Add(fun _ -> startstop())
 
