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

        let bd = new PnlBoard(Dock=DockStyle.Fill)
        let pgn = new PnlPgn(Dock=DockStyle.Fill)
        let sts = new WbStats(Dock=DockStyle.Fill)
        let gmtbs = new TcGames(Dock=DockStyle.Fill)
        let anl = new TcAnl(Dock=DockStyle.Fill)
        let ts = new ToolStrip()
        let ms = new MenuStrip()
        let saveb = new ToolStripButton(Image = img "sav.png", ImageTransparentColor = Color.Magenta, DisplayStyle = ToolStripItemDisplayStyle.Image, Text = "&Save", Enabled = false)
        let savem = new ToolStripMenuItem(Image = img "sav.png", ImageTransparentColor = Color.Magenta, ShortcutKeys = (Keys.Control|||Keys.S), Text = "&Save", Enabled = false)
        let closeb = new ToolStripButton(Image = img "cls.png", ImageTransparentColor = Color.Magenta, DisplayStyle = ToolStripItemDisplayStyle.Image, Text = "&Close", Enabled = false)
        let closem = new ToolStripMenuItem(Image = img "cls.png", ImageTransparentColor = Color.Magenta, ShortcutKeys = (Keys.Control|||Keys.W), Text = "&Close", Enabled = false)
                
        let updateMenuStates() =
            //TODO - do updates such as recents
            closeb.Enabled<-gmtbs.TabCount>1
            closem.Enabled<-gmtbs.TabCount>1
            ()

        let updateTitle() =
            let mutable fname = ""
            if ScincFuncs.Base.Getfilename(&fname) = 0 then
                this.Text <- "ScincNet - " + Path.GetFileNameWithoutExtension(fname)
        
        let refreshWindows() =
            updateMenuStates()
            updateTitle()

        let donew() =
            if ScincFuncs.Base.CountFree()=0 then
                MessageBox.Show("Too many databases open; close one first","Scinc Error")|>ignore
            else
                let ndlg = new SaveFileDialog(Title="Create New Database",Filter="Scid databases(*.si4)|*.si4",AddExtension=true,OverwritePrompt=false)
                if ndlg.ShowDialog() = DialogResult.OK then
                    //create database
                    let nm = Path.GetFileNameWithoutExtension(ndlg.FileName)
                    let fn = Path.Combine(Path.GetDirectoryName(ndlg.FileName), nm)
                    if ScincFuncs.Base.Create(fn)<0 then
                        MessageBox.Show("Unable to create database: " + fn,"Scinc Error")|>ignore
                    else
                        Recents.add fn
                        refreshWindows()
                        pgn.Refrsh(0)
                        if sts.BaseNum()= -1 then sts.Init(nm,ScincFuncs.Base.Current())

        let doopen(ifn:string,dotree:bool) = 
            if ScincFuncs.Base.CountFree()=0 then
                MessageBox.Show("Too many databases open; close one first","Scinc Error")|>ignore
            else
                let ndlg = new OpenFileDialog(Title="Open Database",Filter="Scid databases(*.si4)|*.si4")
                if ifn="" && ndlg.ShowDialog() = DialogResult.OK then
                    //open database
                    let nm = Path.GetFileNameWithoutExtension(ndlg.FileName)
                    let fn = Path.Combine(Path.GetDirectoryName(ndlg.FileName), nm)
                    if ScincFuncs.Base.Open(fn)<0 then
                        MessageBox.Show("Unable to open database: " + fn,"Scinc Error")|>ignore
                    else
                        let current = ScincFuncs.Base.Current()
                        let auto = ScincFuncs.Base.Autoloadgame(true,uint32(current))
                        ScincFuncs.ScidGame.Load(uint32(auto))|>ignore
                        Recents.add fn
                        gmtbs.AddTab()
                        refreshWindows()
                        pgn.Refrsh(auto)
                        if sts.BaseNum()= -1||dotree then sts.Init(nm,ScincFuncs.Base.Current())
                elif ifn<>"" then
                    //open database
                    let nm = Path.GetFileNameWithoutExtension(ifn)
                    let fn = ifn
                    if ScincFuncs.Base.Open(fn)<0 then
                        MessageBox.Show("Unable to open database: " + fn,"Scinc Error")|>ignore
                    else
                        let current = ScincFuncs.Base.Current()
                        let auto = ScincFuncs.Base.Autoloadgame(true,uint32(current))
                        ScincFuncs.ScidGame.Load(uint32(auto))|>ignore
                        Recents.add fn
                        gmtbs.AddTab()
                        refreshWindows()
                        pgn.Refrsh(auto)
                        if sts.BaseNum()= -1||dotree then sts.Init(nm,ScincFuncs.Base.Current())

        let dosave() =
            pgn.SaveGame()
            //need to seload gms and selct the right row
            let gnum = pgn.GetGameNumber()
            gmtbs.Refrsh(bd.GetBoard())
            gmtbs.SelNum(gnum)

        let doclose() = 
            //offer to save game if has changed
            if saveb.Enabled then
                pgn.PromptSaveGame()
            //clear tree if holds current base
            if sts.BaseNum()=ScincFuncs.Base.Current() then
                sts.Close()
            //now close tab
            gmtbs.Close()
            //assume this will switch tabs and then call dotbselect below?

        let doexit() =
            //offer to save game if has changed
            if saveb.Enabled then
                pgn.PromptSaveGame()
            this.Close()

        let donewg() =
            //clear pgn and set gnum to 0
            pgn.NewGame()
 
        let dobdchg(nbd) =
            bd.SetBoard(nbd)
            sts.UpdateFen(nbd)
            gmtbs.Refrsh(nbd)
            anl.SetBoard(nbd)

        let dogmchg(ischg) =
            //set save menus
            saveb.Enabled<-ischg
            savem.Enabled<-ischg

        let domvsel(mvstr) =
            let board = bd.GetBoard()
            let mv = mvstr|>FsChess.Move.FromSan board
            bd.DoMove(mvstr)
            pgn.DoMove(mv)
            let nbd = bd.GetBoard()
            anl.SetBoard(nbd)
            sts.UpdateFen(nbd)
            gmtbs.Refrsh(nbd)

        let domvmade(mv) =
            pgn.DoMove(mv)
            let nbd = bd.GetBoard()
            anl.SetBoard(nbd)
            sts.UpdateFen(nbd)
            gmtbs.Refrsh(nbd)

        let dogmsel(rw) =
             pgn.SwitchGame(rw)

        let dotbselect(e:TabControlEventArgs) =
            let index = e.TabPageIndex
            //todo - need to set current
            let basenum = if index = 0 then 9 else index
            ScincFuncs.Base.Switch(basenum)|>ignore
            let auto = ScincFuncs.Base.Autoloadgame(true,uint32(basenum))
            ScincFuncs.ScidGame.Load(uint(auto))|>ignore
            pgn.Refrsh(auto)
            let nbd = FsChess.Board.Start
            bd.SetBoard(nbd)
            sts.UpdateFen(nbd)
            gmtbs.Refrsh(nbd)
            anl.SetBoard(nbd)
        
        let createts() = 
            // new
            let newb = new ToolStripButton(Image = img "new.png", ImageTransparentColor = Color.Magenta, DisplayStyle = ToolStripItemDisplayStyle.Image, Text = "&New")
            newb.Click.Add(fun _ -> donew())
            ts.Items.Add(newb)|>ignore
            // open
            let openb = new ToolStripButton(Image = img "opn.png", ImageTransparentColor = Color.Magenta, DisplayStyle = ToolStripItemDisplayStyle.Image, Text = "&Open")
            openb.Click.Add(fun _ -> doopen("",false))
            ts.Items.Add(openb)|>ignore
            // close
            closeb.Click.Add(fun _ -> doclose())
            ts.Items.Add(closeb)|>ignore
            // save
            saveb.Click.Add(fun _ -> dosave())
            ts.Items.Add(saveb)|>ignore

        let createms() = 
            // file menu
            let filem = new ToolStripMenuItem(Text = "&File")
            // file new
            let newm = new ToolStripMenuItem(Image = img "new.png", ImageTransparentColor = Color.Magenta, ShortcutKeys = (Keys.Control|||Keys.N), Text = "&New")
            newm.Click.Add(fun _ -> donew())
            filem.DropDownItems.Add(newm)|>ignore
            // file open
            let openm = new ToolStripMenuItem(Image = img "opn.png", ImageTransparentColor = Color.Magenta, ShortcutKeys = (Keys.Control|||Keys.O), Text = "&Open")
            openm.Click.Add(fun _ -> doopen("",false))
            filem.DropDownItems.Add(openm)|>ignore
            // file close
            closem.Click.Add(fun _ -> doclose())
            filem.DropDownItems.Add(closem)|>ignore
            // file open as tree
            let opentreem = new ToolStripMenuItem(Text = "Open as &Tree")
            opentreem.Click.Add(fun _ -> doopen("",true))
            filem.DropDownItems.Add(opentreem)|>ignore
            // recents
            let recm = new ToolStripMenuItem(Text = "Recent")
            let rectreem = new ToolStripMenuItem(Text = "Recent as Tree")
            filem.DropDownItems.Add(recm)|>ignore
            filem.DropDownItems.Add(rectreem)|>ignore
            let addrec (rc:string) =
                let mn = new ToolStripMenuItem(Text = Path.GetFileNameWithoutExtension(rc))
                mn.Click.Add(fun _ -> doopen(rc,false))
                recm.DropDownItems.Add(mn)|>ignore
                let mn1 = new ToolStripMenuItem(Text = Path.GetFileNameWithoutExtension(rc))
                mn1.Click.Add(fun _ -> doopen(rc,true))
                rectreem.DropDownItems.Add(mn1)|>ignore
            let rcs = 
                Recents.get()
                Recents.dbs
            rcs|>Seq.iter addrec
            // file exit
            let exitm = new ToolStripMenuItem(Text = "Exit")
            exitm.Click.Add(fun _ -> doexit())
            filem.DropDownItems.Add(exitm)|>ignore
            
            // game menu
            let gamem = new ToolStripMenuItem(Text = "&Game")
            // game new
            let newgm = new ToolStripMenuItem(Text = "New")
            newgm.Click.Add(fun _ -> donewg())
            gamem.DropDownItems.Add(newgm)|>ignore
            // game save
            savem.Click.Add(fun _ -> dosave())
            gamem.DropDownItems.Add(savem)|>ignore
            // game edit headers
            let edithm = new ToolStripMenuItem(Text = "Edit Headers")
            edithm.Click.Add(fun _ -> pgn.EditHeaders())
            gamem.DropDownItems.Add(edithm)|>ignore
          
            ms.Items.Add(filem)|>ignore
            ms.Items.Add(gamem)|>ignore

        let bgpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let lfpnl = new Panel(Dock=DockStyle.Left,BorderStyle=BorderStyle.Fixed3D,Width=400)
        let rtpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let lftpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=400)
        let lfbpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let rttpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=350)
        let rtmpnl = new Panel(Dock=DockStyle.Top,BorderStyle=BorderStyle.Fixed3D,Height=100)
        let rtbpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
 
        do
            ScincFuncs.Eco.Read("scid.eco")|>ignore
            gmtbs|>rtbpnl.Controls.Add
            rtbpnl|>rtpnl.Controls.Add
            anl|>rtmpnl.Controls.Add
            rtmpnl|>rtpnl.Controls.Add
            sts|>rttpnl.Controls.Add
            rttpnl|>rtpnl.Controls.Add
            rtpnl|>bgpnl.Controls.Add
            pgn|>lfbpnl.Controls.Add
            lfbpnl|>lfpnl.Controls.Add
            bd|>lftpnl.Controls.Add
            lftpnl|>lfpnl.Controls.Add
            lfpnl|>bgpnl.Controls.Add
            bgpnl|>this.Controls.Add
            createts()
            ts|>this.Controls.Add
            createms()
            ms|>this.Controls.Add
            //Events
            pgn.BdChng  |> Observable.add dobdchg 
            pgn.GmChng |> Observable.add dogmchg
            sts.MvSel |> Observable.add domvsel
            bd.MvMade |> Observable.add domvmade
            gmtbs.GmSel |> Observable.add dogmsel
            gmtbs.Selected |>Observable.add dotbselect

   