namespace ScincNet

open System.IO
open System.Drawing
open System.Windows.Forms
open FsChess.WinForms

module Form =
    let img nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("ScincNet.Images." + nm)
        Image.FromStream(file)
    let ico nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("ScintNet.Icons." + nm)
        new Icon(file)

    type FrmMain() as this =
        inherit Form(Text = "ScincNet", WindowState = FormWindowState.Maximized, IsMdiContainer = true)

        let mutable gms = []
        let mutable ct = 0
     
        let bd = new PnlBoard(Dock=DockStyle.Fill)
        let pgn = new WbPgn(Dock=DockStyle.Fill)
        let sts = new WbStats(Dock=DockStyle.Fill)
        let gms = new DgvGames(Dock=DockStyle.Fill)
        let anl = new PnlAnl(Dock=DockStyle.Fill)
        let ts = new ToolStrip()
        let ms = new MenuStrip()
        let gmlbl = new Label(Text="Game: White v. Black",Width=400,TextAlign=ContentAlignment.MiddleLeft,Font = new Font(new FontFamily("Arial"), 12.0f))
        let fllbl = new Label(Text="Filter: 99999999/99999999",Width=400,TextAlign=ContentAlignment.MiddleLeft,Font = new Font(new FontFamily("Arial"), 12.0f))
        

        let updateMenuStates() =
            //TODO - do updates such as recents
            ()

        let updateTitle() =
            let mutable fname = ""
            if ScincFuncs.Base.Getfilename(&fname) = 0 then
                this.Text <- "ScincNet - " + Path.GetFileNameWithoutExtension(fname)
        
        let refreshWindows() =
            gms.Reload()
            updateMenuStates()
            updateTitle()

        let updateBoard() =
            pgn.Refrsh()
            //bd.Refrsh()
            //update gmlbl
            let mutable wnm = ""
            ScincFuncs.ScidGame.GetTag("White",&wnm)|>ignore
            let mutable bnm = ""
            ScincFuncs.ScidGame.GetTag("Black",&bnm)|>ignore
            gmlbl.Text <- "Game: " + wnm + " v. " + bnm
            //update fllbl
            let numgms = ScincFuncs.Base.NumGames()
            let fnum = ScincFuncs.Filt.Count()
            fllbl.Text <- "Filter: " + fnum.ToString() + "/" + numgms.ToString()
            //update stats
            ScincFuncs.Eco.Read("scid.eco")|>ignore
            sts.Refrsh()
            
        let donew() =
            if ScincFuncs.Base.CountFree()=0 then
                MessageBox.Show("Too many databases open; close one first","Scinc Error")|>ignore
            else
                let ndlg = new SaveFileDialog(Title="Create New Database",Filter="Scid databases(*.si4)|*.si4",AddExtension=true,OverwritePrompt=false)
                if ndlg.ShowDialog() = DialogResult.OK then
                    //create database
                    let fn = Path.Combine(Path.GetDirectoryName(ndlg.FileName), Path.GetFileNameWithoutExtension(ndlg.FileName))
                    if ScincFuncs.Base.Create(fn)<0 then
                        MessageBox.Show("Unable to create database: " + fn,"Scinc Error")|>ignore
                    else
                        Recents.add fn
                        refreshWindows()
                        updateBoard()
                        ()
                else
                    ()

        let doopen() = 
            if ScincFuncs.Base.CountFree()=0 then
                MessageBox.Show("Too many databases open; close one first","Scinc Error")|>ignore
            else
                let ndlg = new OpenFileDialog(Title="Open Database",Filter="Scid databases(*.si4)|*.si4")
                if ndlg.ShowDialog() = DialogResult.OK then
                    //open database
                    let fn = Path.Combine(Path.GetDirectoryName(ndlg.FileName), Path.GetFileNameWithoutExtension(ndlg.FileName))
                    if ScincFuncs.Base.Open(fn)<0 then
                        MessageBox.Show("Unable to open database: " + fn,"Scinc Error")|>ignore
                    else
                        let current = ScincFuncs.Base.Current()
                        let auto = ScincFuncs.Base.Autoloadgame(true,uint32(current))
                        ScincFuncs.ScidGame.Load(uint32(auto))|>ignore
                        Recents.add fn
                        refreshWindows()
                        updateBoard()
                        ()
                else
                    ()


        let createts() = 
            // new
            let newb = new ToolStripButton(Image = img "new.png", ImageTransparentColor = Color.Magenta, DisplayStyle = ToolStripItemDisplayStyle.Image, Text = "&New")
            newb.Click.Add(fun _ -> donew())
            ts.Items.Add(newb)|>ignore
            // open
            let openb = new ToolStripButton(Image = img "opn.png", ImageTransparentColor = Color.Magenta, DisplayStyle = ToolStripItemDisplayStyle.Image, Text = "&Open")
            openb.Click.Add(fun _ -> doopen())
            ts.Items.Add(openb)|>ignore

        let createms() = 
            // file menu
            let filem = new ToolStripMenuItem(Text = "&File")
            // file new
            let newm = new ToolStripMenuItem(Image = img "new.png", ImageTransparentColor = Color.Magenta, ShortcutKeys = (Keys.Control|||Keys.N), Text = "&New")
            newm.Click.Add(fun _ -> donew())
            filem.DropDownItems.Add(newm)|>ignore
            // file open
            let openm = new ToolStripMenuItem(Image = img "opn.png", ImageTransparentColor = Color.Magenta, ShortcutKeys = (Keys.Control|||Keys.O), Text = "&Open")
            openm.Click.Add(fun _ -> doopen())
            filem.DropDownItems.Add(openm)|>ignore
            
            
            
            
            ms.Items.Add(filem)|>ignore



        let vpnl = new FlowLayoutPanel(Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown)

        let bgpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let lfpnl = new Panel(Dock=DockStyle.Left,BorderStyle=BorderStyle.Fixed3D,Width=400)
        let rtpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let lftpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=400)
        let lfmpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=50)
        let lfbpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let rttpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=350)
        let rtmpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=100)
        let rtbpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
 
        do
            gms|>rtbpnl.Controls.Add
            rtbpnl|>rtpnl.Controls.Add
            anl|>rtmpnl.Controls.Add
            rtmpnl|>rtpnl.Controls.Add
            sts|>rttpnl.Controls.Add
            rttpnl|>rtpnl.Controls.Add
            rtpnl|>bgpnl.Controls.Add
            pgn|>lfbpnl.Controls.Add
            lfbpnl|>lfpnl.Controls.Add
            gmlbl|>vpnl.Controls.Add
            fllbl|>vpnl.Controls.Add
            vpnl|>lfmpnl.Controls.Add
            lfmpnl|>lfpnl.Controls.Add
            bd|>lftpnl.Controls.Add
            lftpnl|>lfpnl.Controls.Add
            lfpnl|>bgpnl.Controls.Add
            bgpnl|>this.Controls.Add
            createts()
            ts|>this.Controls.Add
            createms()
            ms|>this.Controls.Add
    