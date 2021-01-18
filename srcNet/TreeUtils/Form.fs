namespace TreeUtils

open System.IO
open System.Drawing
open System.Windows.Forms
open FsChess.WinForms
open ScincFuncs
open FsChess

type private GameInfo = {Gmno:int;Welo:int;Belo:int;Year:int;Result:int}
type private TreeData = {TotElo:int64;EloCount:int64;TotPerf:int64;PerfCount:int64;TotYear:int64;YearCount:int64;TotScore:int64;DrawCount:int64;TotCount:int64}
type private MvTrees = System.Collections.Generic.Dictionary<string,TreeData>


module Form =
    let img nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("TreeUtils.Images." + nm)
        Image.FromStream(file)
    let ico nm =
        let thisExe = System.Reflection.Assembly.GetExecutingAssembly()
        let file = thisExe.GetManifestResourceStream("TreeUtils." + nm)
        new Icon(file)

    type FrmMain() as this =
        inherit Form(Text = "TreeUtils", Icon = ico "tree.ico", Width=800, Height=500, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, StartPosition = FormStartPosition.CenterScreen)
        let bfol = 
            let pth = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),"ScincNet\\bases")
            Directory.CreateDirectory(pth)|>ignore
            pth

        let mutable bd = Board.Start
        

        let sts = new WbStats(Dock=DockStyle.Fill)
        let crbtn = new Button(Text="Create Tree")
        let prg = new ProgressBar(Width=200)
        let lbl = new Label(Text="Ready",Width=200)

        let domvsel(mvstr) =
            let mv = mvstr|>Move.FromSan bd
            bd <- bd|>Board.Push mv
            sts.UpdateFen(bd)
        
        
        let docreate(e) =
            let totaldict = new System.Collections.Generic.Dictionary<string,MvTrees>()
            
            let getGmBds i =
                ScidGame.Load(uint(i))|>ignore
                let mutable mvs = new ResizeArray<string>()
                let mutable posns = new ResizeArray<string>()
                //TODO: decide how many
                let actual = ScidGame.GetMovesPosns(&mvs,&posns,20)
                let mutable welo = ""
                ScidGame.GetTag("WhiteElo",&welo)|>ignore
                let mutable belo = ""
                ScidGame.GetTag("BlackElo",&belo)|>ignore
                let mutable res = ""
                ScidGame.GetTag("Result",&res)|>ignore
                //{ "*",  "1-0",  "0-1",  "1/2-1/2" }
                let mutable dt = ""
                ScidGame.GetTag("Date",&dt)|>ignore
                let gminfo =
                    {
                        Gmno = i
                        Welo = if welo="" then 0 else int(welo)
                        Belo = if belo="" then 0 else int(belo)
                        Year = if dt.Length>3 && (not (dt.StartsWith("?"))) then int(dt.[..3]) else 0
                        Result = if res="1" then 2 elif res="2" then 0 else 1
                    }
                gminfo,posns,mvs

            let processGame i =
                let gminfo,posns,mvs = getGmBds i
                
                let wtd = 
                    let perf,ct =
                        if gminfo.Belo=0 then 0,0
                        elif gminfo.Result=1 then //draw
                            gminfo.Belo,1
                        elif gminfo.Result=2 then //win
                            (gminfo.Belo + 400),1
                        else (gminfo.Belo - 400),1
                    {TotElo = int64(gminfo.Welo);EloCount = (if gminfo.Welo=0 then 0L else 1L);TotPerf = int64(perf);
                        PerfCount = int64(ct);TotYear = int64(gminfo.Year);YearCount = (if gminfo.Year=0 then 0L else 1L);
                        TotScore = int64(gminfo.Result); DrawCount = (if gminfo.Result=1 then 1L else 0L); TotCount=1L}
                let btd = 
                    let perf,ct =
                            if gminfo.Welo=0 then 0,0
                            elif gminfo.Result=1 then //draw
                                gminfo.Welo,1
                            elif gminfo.Result=0 then //win
                                (gminfo.Welo + 400),1
                            else (gminfo.Welo - 400),1
                    {TotElo = int64(gminfo.Belo);EloCount = (if gminfo.Belo=0 then 0L else 1L);TotPerf = int64(perf);
                        PerfCount = int64(ct);TotYear = int64(gminfo.Year);YearCount = (if gminfo.Year=0 then 0L else 1L);
                        TotScore = int64(gminfo.Result); DrawCount = (if gminfo.Result=1 then 1L else 0L); TotCount=1L}
                //now need to go through the boarda and put in dictionary holding running totals
                for j = 0 to posns.Count-1 do
                    let bd = posns.[j]
                    let mv = mvs.[j]
                    let isw = bd.EndsWith("w")
                    if totaldict.ContainsKey(bd) then
                        let mvdct:MvTrees = totaldict.[bd]
                        if mvdct.ContainsKey(mv) then
                            let cmt =  mvdct.[mv]
                            let nmt = if isw then wtd else btd
                            mvdct.[mv]<-
                                {TotElo = cmt.TotElo+nmt.TotElo;EloCount = cmt.EloCount+nmt.EloCount;
                                    TotPerf = cmt.TotPerf+nmt.TotPerf;PerfCount = cmt.PerfCount+nmt.PerfCount;
                                    TotYear = cmt.TotYear+nmt.TotYear;YearCount = cmt.YearCount+nmt.YearCount;
                                    TotScore = cmt.TotScore+nmt.TotScore; DrawCount = cmt.DrawCount+nmt.DrawCount; 
                                    TotCount=cmt.TotCount+nmt.TotCount}
                        else 
                            mvdct.[mv]<-if isw then wtd else btd
                    else
                        let mvdct = new MvTrees()
                        mvdct.[mv]<-if isw then wtd else btd
                        totaldict.[bd]<-mvdct

            let processPos i (vl:MvTrees) =
                let sts = new stats();
                let mvsts = new ResizeArray<mvstats>()
                let totsts = new totstats()
                totsts.TotFreq<-1.0
                for mtr in vl do
                    let tr = mtr.Value
                    totsts.TotCount<-totsts.TotCount+tr.TotCount

                let mutable ect = 0L
                let mutable pct = 0L
                let mutable yct = 0L
                for mtr in vl do
                    let mv = mtr.Key
                    let tr = mtr.Value
                    let mvst = new mvstats()
                    mvst.Count<-tr.TotCount
                    mvst.Freq<-float(mvst.Count)/float(totsts.TotCount)
                    mvst.WhiteWins<-(tr.TotScore-tr.DrawCount)/2L
                    totsts.TotWhiteWins<-totsts.TotWhiteWins+mvst.WhiteWins
                    mvst.Draws<-tr.DrawCount
                    totsts.TotDraws<-totsts.TotDraws+mvst.Draws
                    mvst.BlackWins<-mvst.Count-mvst.WhiteWins-mvst.Draws
                    totsts.TotBlackWins<-totsts.TotBlackWins+mvst.BlackWins
                    mvst.Score<-float(tr.TotScore)/float(tr.TotCount*2L)
                    totsts.TotScore<-totsts.TotScore+float(tr.TotScore)/float(totsts.TotCount*2L)
                    mvst.DrawPc<-float(tr.DrawCount)/float(tr.TotCount)
                    totsts.TotDrawPc<-totsts.TotDrawPc+float(tr.DrawCount)/float(totsts.TotCount)
                    mvst.AvElo<-if tr.EloCount<=10L then 0L else tr.TotElo/tr.EloCount
                    totsts.TotAvElo<-totsts.TotAvElo+tr.TotElo
                    ect<-ect+tr.EloCount
                    mvst.Perf<-if tr.PerfCount<=10L then 0L else tr.TotPerf/tr.PerfCount
                    totsts.TotPerf<-totsts.TotPerf+tr.TotPerf
                    pct<-pct+tr.PerfCount
                    mvst.AvYear<-if tr.YearCount<=0L then 0L else tr.TotYear/tr.YearCount
                    totsts.TotAvYear<-totsts.TotAvYear+tr.TotYear
                    yct<-yct+tr.YearCount
                    mvst.Mvstr<-mv
                    mvsts.Add(mvst)

                totsts.TotAvElo<-if ect=0L then 0L else totsts.TotAvElo/ect
                totsts.TotPerf<-if pct=0L then 0L else totsts.TotPerf/pct
                totsts.TotAvYear<-if yct=0L then 0L else totsts.TotAvYear/yct
                //need to sort by count
                mvsts.Sort(fun a b -> int(b.Count-a.Count))
                sts.MvsStats<-mvsts
                sts.TotStats<-totsts
                if i%100=0 then 
                    prg.Value<-i
                    Application.DoEvents()
                sts
            
            let ndlg = new OpenFileDialog(Title="Open Database",Filter="Scid databases(*.si4)|*.si4",InitialDirectory=bfol)
            if ndlg.ShowDialog() = DialogResult.OK then
                let nm = Path.GetFileNameWithoutExtension(ndlg.FileName)
                let basename = Path.Combine(Path.GetDirectoryName(ndlg.FileName), nm)
                this.Enabled <-false
                lbl.Text <- "Opening base..."
                lbl.Refresh()
                Application.DoEvents()
                if (Base.Open(basename)<>1) then
                    MessageBox.Show("Error opening database " + basename)|>ignore
                elif (Base.Isreadonly()) then
                    MessageBox.Show("Error database " + basename + " is read only" )|>ignore
                else
                    let numgames = Base.NumGames()
                    totaldict.Clear()
                    prg.Minimum<-0
                    prg.Maximum<-numgames
                    lbl.Text <- "Processing " + numgames.ToString() + " games..."
                    lbl.Refresh()
                    Application.DoEvents()
                    for i = 1 to numgames do
                        processGame(i)
                        if i%100=0 then 
                            prg.Value<-i
                            Application.DoEvents()

                    lbl.Text <- "Closing base..."
                    lbl.Refresh()
                    Application.DoEvents()
                    Base.Close()|>ignore
                    //now create tree for each
                    let numpos = totaldict.Count
                    prg.Maximum<-numpos
                    lbl.Text <- "Creating arrays..."
                    lbl.Refresh()
                    Application.DoEvents()
                    let mutable posns = Array.zeroCreate numpos 
                    let mutable mvtrees = Array.zeroCreate numpos
                    totaldict.Keys.CopyTo(posns,0)
                    totaldict.Values.CopyTo(mvtrees,0)
                    totaldict.Clear()
                    lbl.Text <- "Processing " + numpos.ToString() + " positions..."
                    lbl.Refresh()
                    Application.DoEvents()
                    let stsarr = mvtrees|>Array.mapi processPos
                    prg.Value<-0
                    lbl.Text <- "Creating dictionary..."
                    lbl.Refresh()
                    Application.DoEvents()
                    let fol = basename + "_FILES"
                    lbl.Text <- "Saving dictionary..."
                    lbl.Refresh()
                    if posns.Length>100000 then StaticTree.CreateBig(fol)|>ignore else StaticTree.Create(fol)|>ignore
                    StaticTree.Save(posns,stsarr,fol)|>ignore
                    lbl.Text <- "Loading dictionary..."
                    lbl.Refresh()
                    Application.DoEvents()
                    lbl.Text <- "Initializing View..."
                    lbl.Refresh()
                    Application.DoEvents()
                    sts.InitStatic(basename)
                    lbl.Text <- "Loading View..."
                    lbl.Refresh()
                    Application.DoEvents()
                    sts.Refrsh()
                    lbl.Text <- "Ready"
                    lbl.Refresh()
                    Application.DoEvents()
                    this.Enabled <-true
                    
                    

        let bgpnl = new Panel(Dock=DockStyle.Fill,BorderStyle=BorderStyle.Fixed3D)
        let btmpnl = new Panel(Dock=DockStyle.Bottom,BorderStyle=BorderStyle.Fixed3D,Height=30)

        do
            bgpnl.Controls.Add(sts)
            this.Controls.Add(bgpnl)
            crbtn.Left <- int(10.0)
            crbtn.Top <- int(float(btmpnl.Height) * 0.5 - float(crbtn.Height) * 0.5)
            prg.Top <- int(float(btmpnl.Height) * 0.5 - float(prg.Height) * 0.5)
            prg.Left <- int(float(crbtn.Width) + 20.0)
            lbl.Top <- int(float(btmpnl.Height) * 0.5 - float(prg.Height) * 0.5)
            lbl.Left <- int(float(crbtn.Width) + float(prg.Width) + 40.0)
            btmpnl.Controls.Add(crbtn)
            btmpnl.Controls.Add(prg)
            btmpnl.Controls.Add(lbl)
            this.Controls.Add(btmpnl)
            crbtn.Click.Add(docreate)
            sts.MvSel |> Observable.add domvsel
           