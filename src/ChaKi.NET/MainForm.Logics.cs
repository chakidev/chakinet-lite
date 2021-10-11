using ChaKi.Common.Widgets;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using ChaKi.Entity.Settings;
using ChaKi.GUICommon;
using ChaKi.Panels;
using ChaKi.Service.Annotations;
using ChaKi.Service.Collocation;
using ChaKi.Service.Export;
using ChaKi.Service.Readers;
using ChaKi.Service.Search;
using ChaKi.ToolDialogs;
using ChaKi.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using MessageBox = ChaKi.Common.Widgets.MessageBox;

namespace ChaKi
{
    partial class MainForm
    {
        public void OnBeginSearch()
        {
            KwicSearch(null, SearchSequenceOperator.None);
        }

        public void OnBeginSearchNarrow()
        {
            if (this.historyGuidePanel.Current != null)
            {
                KwicSearch(this.historyGuidePanel.Current, SearchSequenceOperator.And);
            }
        }

        public void OnBeginSearchAppend()
        {
            if (this.historyGuidePanel.Current != null)
            {
                KwicSearch(this.historyGuidePanel.Current, SearchSequenceOperator.Or);
            }
        }

        private void KwicSearch(SearchHistory parent, SearchSequenceOperator op)
        {
            // 条件をセーブ
            SaveSearchCond(null);

            // 検索条件を作成
            SearchConditions conds = this.condPanel.CreateConditions();
            m_Model.CurrentSearchConditions = conds;
            conds.Operator = op;

            // 元連結条件がない場合は空のものを新規作成する
            SearchHistory hist = null;
            SearchConditionsSequence condSeq = null;
            switch (op)
            {
                case SearchSequenceOperator.None:
                    condSeq = new SearchConditionsSequence();
                    condSeq.AddCond(conds);
                    hist = SearchHistory.Create(condSeq);
                    break;
                case SearchSequenceOperator.And:
                    condSeq = new SearchConditionsSequence(parent.CondSeq);
                    condSeq.AddCond(conds);
                    hist = SearchHistory.Create(condSeq);
                    break;
                case SearchSequenceOperator.Or:
                    condSeq = parent.CondSeq;
                    condSeq.AddCond(conds);
                    hist = parent;
                    parent = null;
                    break;
            }
            if (condSeq == null || hist == null)
            {
                return;
            }

            // ServiceCommandを作成
            IServiceCommand cmd = null;
            switch (conds.ActiveSearch)
            {
                case SearchType.SentenceSearch:
                    cmd = new SentenceListService(hist, parent);
                    break;
                case SearchType.StringSearch:
                    cmd = new StringSearchService(hist, parent);
                    break;
                case SearchType.TagSearch:
                    cmd = new TagSearchService(hist, parent);
                    break;
                case SearchType.DepSearch:
                    cmd = new DepSearchService(hist, parent);
                    break;
            }
            if (cmd != null)
            {
                // Viewに対するModelを更新する
                this.commandPanel.SetModel(hist.Progress);
                IChaKiView view = this.ChangeView(conds.ActiveSearch);
                view.SetModel(hist);

                // ServiceCommandに基づき、ヒストリを新規作成して追加
                if (parent != null)
                {
                    parent.AddChild(hist);
                }
                else
                {
                    if (op != SearchSequenceOperator.Or)
                    {
                        SearchHistory.Root.AddChild(hist);
                    }
                }

                // ServiceCommandを実行キューに入れる
                this.commandPanel.QueueCommand(cmd);
            }

            // Searchコマンド実行時にFilter条件パネルのAutoIncrementを試みる.
            this.condPanel.PerformFilterAutoIncrement();
        }

        /// <summary>
        /// KwicSearchを既存のHistoryノードに対して行う。
        /// 既存の検索結果は一旦クリアされ、検索結果で上書きされる。
        /// 同じ条件でサーチし直してKwicListを更新する場合に用いる。
        /// </summary>
        /// <param name="hist">既存の検索ヒストリ（既に検索条件と検索結果を含む）</param>
        private void KwicSearchAgain(SearchHistory hist)
        {
            if (hist == null || hist.CondSeq == null || hist.CondSeq.Count == 0)
            {
                return;
            }
            SearchHistory parent = hist.Parent;

            // ServiceCommandを作成
            IServiceCommand cmd = null;
            switch (hist.CondSeq.Last.ActiveSearch)
            {
                case SearchType.SentenceSearch:
                    cmd = new SentenceListService(hist, parent);
                    break;
                case SearchType.StringSearch:
                    cmd = new StringSearchService(hist, parent);
                    break;
                case SearchType.TagSearch:
                    cmd = new TagSearchService(hist, parent);
                    break;
                case SearchType.DepSearch:
                    cmd = new DepSearchService(hist, parent);
                    break;
            }
            if (cmd != null)
            {
                int selLine = this.kwicView.SelectedLine;
                cmd.Completed = cmd.Aborted = delegate (object o, EventArgs e) { this.kwicView.SelectedLine = selLine; };

                // 検索結果をクリア
                hist.DeleteAll();
                // Viewに対するModelを更新する
                this.commandPanel.SetModel(hist.Progress);
                IChaKiView view = this.ChangeView(hist.CondSeq.Last.ActiveSearch);
                view.SetModel(hist);

                // ServiceCommandを実行キューに入れる
                this.commandPanel.QueueCommand(cmd);
            }
        }

        public void OnBeginWordList()
        {
            // 条件をセーブ
            SaveSearchCond(null);

            // ServiceCommandを作成
            IServiceCommand cmd = null;
            SearchConditions conds = this.condPanel.CreateConditions();
            m_Model.CurrentSearchConditions = conds;
            SearchConditionsSequence condSeq = new SearchConditionsSequence(conds);
            SearchHistory hist = null;
            int lexSize = 1;
            int pivotPos = -1;
            switch (this.condPanel.SelectedTab)
            {
                case ConditionsPanelType.CP_TAG:
                    conds.ActiveSearch = SearchType.TagWordList;
                    hist = SearchHistory.Create(condSeq);
                    cmd = new WordListService(hist, null, SearchType.TagWordList);
                    lexSize = hist.CondSeq.Last.TagCond.Count;  // Lexeme Boxの数
                    pivotPos = hist.CondSeq.Last.TagCond.GetPivotPos();
                    break;
                case ConditionsPanelType.CP_DEP:
                    conds.ActiveSearch = SearchType.DepWordList;
                    hist = SearchHistory.Create(condSeq);
                    cmd = new WordListService(hist, null, SearchType.DepWordList);
                    hist.CondSeq.Last.DepCond.GetLexemeCondParams(out lexSize, out pivotPos);
                    break;
            }
            if (cmd != null)
            {
                // 検索終了時のアクションを設定
                cmd.Completed = cmd.Aborted = new EventHandler(delegate (object o, EventArgs e)
                {
                    Invoke(new Action<WordListView>(delegate (WordListView v) { v.FinalizeDisplay(); }), this.wordListView);
                });

                // Viewに対するModelを更新する
                WordListView view = this.ChangeView(conds.ActiveSearch) as WordListView;
                view.SetModel(hist.LexemeList, conds.SentenceCond.Corpora, lexSize, pivotPos);
                this.commandPanel.SetModel(hist.Progress);

                // ServiceCommandに基づき、ヒストリを新規作成して追加
                SearchHistory.Root.AddChild(hist);

                // ServiceCommandを実行キューに入れる
                this.commandPanel.QueueCommand(cmd);
            }
        }

        void OnBeginCollocation()
        {
            // 条件をセーブ
            SaveSearchCond(null);

            // Historyのカレントノードまたは直近のancestorがKwicListであることを確認
            SearchHistory curHistNode = this.historyGuidePanel.GetLastKwicListNode();
            if (curHistNode == null)
            {
                MessageBox.Show("Needs a Kwic Result List.");
                return;
            }

            // Collocation条件を作成
            SearchConditions conds = this.condPanel.CreateConditions();
            m_Model.CurrentSearchConditions = conds;
            SearchConditionsSequence condSeq = new SearchConditionsSequence(conds);
            SearchHistory hist = null;

            conds.ActiveSearch = SearchType.Collocation;
            hist = SearchHistory.Create(condSeq);

            // CollocationServiceを作成して実行（IServiceCommandではない。キューイングも行わない）
            ICollocationService svc = new CollocationService(curHistNode.KwicList, hist.CollList, condSeq);
            try
            {
                if (conds.CollCond.CollType == CollocationType.FSM)
                {
                    // FSMの場合はProgress Dialogを出して、Collocation処理の中断を可能とする.
                    CollocationProgressDialog dlg = new CollocationProgressDialog();
                    dlg.Service = svc;
                    dlg.ShowDialog();
                }
                else
                {
                    svc.Exec();
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while executing collocation:", ex);
                edlg.ShowDialog();
            }

            // Viewに対するModelを更新する
            IChaKiView view = this.ChangeView(conds.ActiveSearch);
            view.SetModel(hist.CollList);

            // ServiceCommandに基づき、ヒストリを新規作成して追加
            curHistNode.AddChild(hist);
        }

        /// <summary>
        /// WordViewから指定された語の生起を検索する
        /// </summary>
        /// <param name="parent"></param>
        private void KwicSearchWordOccurence(SearchHistory parent, List<LexemeCondition> addCond)
        {
            SearchType orgType = parent.CondSeq.Last.ActiveSearch;
            SearchType newType = SearchType.Undefined;
            if (orgType == SearchType.TagWordList)
            {
                // Tag Searchパネルを表示
                newType = SearchType.TagSearch;
                this.condPanel.SelectedTab = ConditionsPanelType.CP_TAG;
                // Tag Search条件を編集する（指定語の条件addCondにより狭める）
                this.condPanel.MergeTagSearchCondition(addCond);
            }
            else if (orgType == SearchType.DepWordList)
            {
                // Dep Searchパネルを表示
                newType = SearchType.DepSearch;
                this.condPanel.SelectedTab = ConditionsPanelType.CP_DEP;
                // Dep Search条件を編集する（指定語の条件addCondにより狭める）
                this.condPanel.MergeDepSearchCondition(addCond);
            }
            else
            {
                return;
            }

            // 条件をセーブ
            SaveSearchCond(null);

            // 元連結条件がない場合は空のものを新規作成する
            SearchConditionsSequence condSeq = new SearchConditionsSequence(parent.CondSeq);

            // ServiceCommandを作成
            SearchConditions conds = this.condPanel.CreateConditions();
            m_Model.CurrentSearchConditions = conds;
            conds.ActiveSearch = newType;
            condSeq.Add(conds);
            SearchHistory hist = SearchHistory.Create(condSeq);
            IServiceCommand cmd = null;
            if (newType == SearchType.TagSearch)
            {
                cmd = new TagSearchService(hist, parent);
            }
            else if (newType == SearchType.DepSearch)
            {
                cmd = new DepSearchService(hist, parent);
            }
            if (cmd != null)
            {
                // Viewに対するModelを更新する
                this.commandPanel.SetModel(hist.Progress);
                IChaKiView view = this.ChangeView(conds.ActiveSearch);
                view.SetModel(hist);

                // ServiceCommandに基づき、ヒストリを新規作成して追加
                parent.AddChild(hist);

                // ServiceCommandを実行キューに入れる
                this.commandPanel.QueueCommand(cmd);
            }
        }

        /// <summary>
        /// idlistで指定されたIDの文を検索表示する.
        /// </summary>
        /// <param name="idlist"></param>
        public void KwicSearchSentenceOccurence(SearchHistory parent, List<int> idlist)
        {
            SearchConditionsSequence condSeq = new SearchConditionsSequence(parent.CondSeq);
            SearchConditions conds = new SearchConditions() { ActiveSearch = SearchType.SentenceSearch };
            Corpus c = condSeq.Last.SentenceCond.Corpora.Count == 1 ? condSeq.Last.SentenceCond.Corpora[0] : null;
            if (c == null)
            {
                throw new Exception("Number of Corpus must be 1.");
            }
            conds.SentenceCond = new SentenceSearchCondition(condSeq.Last.SentenceCond);
            conds.SentenceCond.Ids.Add(c, idlist);
            m_Model.CurrentSearchConditions = conds;
            condSeq.Add(conds);

            // Sentence Searchパネルを表示
            this.condPanel.SelectedTab = ConditionsPanelType.CP_CORPUS;
            SearchHistory hist = SearchHistory.Create(condSeq);
            IServiceCommand cmd = new SentenceListService(hist, parent);
            if (cmd != null)
            {
                // Viewに対するModelを更新する
                this.commandPanel.SetModel(hist.Progress);
                IChaKiView view = this.ChangeView(conds.ActiveSearch);
                view.SetModel(hist);

                // ServiceCommandに基づき、ヒストリを新規作成して追加
                parent.AddChild(hist);

                // ServiceCommandを実行キューに入れる
                this.commandPanel.QueueCommand(cmd);
            }
        }

        public SearchHistory LoadFile(string filename)
        {
            try
            {
                string ext = Path.GetExtension(filename).ToUpper();
                if (ext == ".CHAKI")
                {
                    ChaKiReader rdr = new ChaKiReader();
                    rdr.ConfirmationCallback = s => MessageBox.Show(s, "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes;
                    return rdr.Read(filename);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while loading:", ex);
                edlg.ShowDialog();
                return null;
            }
        }

        public bool Save(string filename, SearchHistory hist)
        {
            if (hist == null)
            {
                hist = this.historyGuidePanel.Current;
                if (hist == null)
                {
                    return false;
                }
            }
            try
            {
                string ext = Path.GetExtension(filename).ToUpper();
                if (ext == ".CHAKI")
                {
                    SaveChakiFormat(filename, hist);
                }
                else if (ext == ".XML")
                {
                    SaveXmlFormat(filename, hist);
                }
                else if (ext == ".CABOCHA")
                {
                    SaveCabochaFormat(filename, hist);
                }
                else if (ext == ".TXT")
                {
                    SaveTextFormat(filename, hist);
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while Saving:", ex);
                edlg.ShowDialog();
                return false;
            }
            return true;
        }

        private void SaveChakiFormat(string filename, SearchHistory hist)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                using (XmlWriter wr = XmlWriter.Create(fs, settings))
                {
                    wr.WriteStartElement("Chaki");
                    wr.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    wr.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                    wr.WriteAttributeString("version", ChaKiReader.CURRENT_VERSION);
                    XmlSerializer ser = new XmlSerializer(typeof(SearchHistory));
                    ser.Serialize(wr, hist);
                    wr.WriteEndElement();
                }
            }
        }

        private void SaveXmlFormat(string filename, SearchHistory hist)
        {
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                using (TextWriter fs = new StreamWriter(filename, false))
                using (XmlWriter wr = XmlWriter.Create(fs, new XmlWriterSettings() { Indent = true }))
                {
                    IExportService svc = new ExportServiceXml(wr);
                    wr.WriteStartElement("ChakiExport");
                    wr.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    wr.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                    wr.WriteAttributeString("version", "1");
                    wr.WriteStartElement("Sentences");
                    svc.Export(hist.KwicList.Records, GetCurrentProjectId());
                    wr.WriteEndElement();
                    wr.WriteEndElement();
                }
            }
            finally
            {
                this.Cursor = oldCur;
            }
        }

        private void SaveCabochaFormat(string filename, SearchHistory hist)
        {
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                using (TextWriter wr = new StreamWriter(filename, false))
                {
                    IExportService svc = new ExportServiceCabocha(wr);
                    svc.Export(hist.KwicList.Records, GetCurrentProjectId());
                }
                this.Cursor = oldCur;
            }
            finally
            {
                this.Cursor = oldCur;
            }
        }

        private void SaveTextFormat(string filename, SearchHistory hist)
        {
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                using (TextWriter wr = new StreamWriter(filename, false))
                {
                    IExportService svc = new ExportServiceText(wr);
                    svc.Export(hist.KwicList.Records, GetCurrentProjectId());
                }
                this.Cursor = oldCur;
            }
            finally
            {
                this.Cursor = oldCur;
            }
        }

        private void ExportToExcel()
        {
            IExportService svc = null;
            try
            {
                List<KwicItem> kwic = this.historyGuidePanel.Current.KwicList.Records;
                ExportSetting setting = UserSettings.GetInstance().ExportSetting;
                if (setting.ExportType == ExportType.Excel)
                {
                    svc = new ExportServiceExcel(setting);
                    svc.Export(kwic, GetCurrentProjectId());
                }
                else
                {
                    string path = Path.GetTempFileName();
                    path = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".CSV";
                    using (TextWriter wr = new StreamWriter(path))
                    {
                        svc = new ExportServiceCSV(setting, wr);
                        svc.Export(kwic, GetCurrentProjectId());
                    }
                    // 書き出したファイルを外部エディタでオープンする
                    Process.Start(GUISetting.Instance.ExternalEditorPath, path);
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while Exporting:", ex);
                edlg.ShowDialog();
            }
            finally
            {
                if (svc != null) svc.Dispose();
            }
        }

        private void ExportGridToExcel(DataGridView dg)
        {
            IExportService svc = null;
            GridHeaderAccessor rowHeaderAccessor;
            GridWithTotal gt = dg as GridWithTotal;
            if (gt != null)
            {
                rowHeaderAccessor = r => { return GridWithTotal.RowHeader(r); };
            }
            else
            {
                rowHeaderAccessor = r => { return WordListView.RowHeader(r); };
            }

            try
            {
                ExportSetting setting = UserSettings.GetInstance().ExportSetting;
                if (setting.ExportType == ExportType.Excel)
                {
                    svc = new ExportServiceExcel(setting);
                    svc.ExportGrid(
                        dg.Rows.Count,
                        dg.Columns.Count,
                        (r, c) => { return dg[c, r].Value; },
                        rowHeaderAccessor,
                        c => { return dg.Columns[c].HeaderText; });
                }
                else if (setting.ExportType == ExportType.CSV)
                {
                    string path = Path.GetTempFileName();
                    path = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".CSV";
                    using (TextWriter wr = new StreamWriter(path, false, new UTF8Encoding(false)))
                    {
                        svc = new ExportServiceCSV(setting, wr);
                        svc.ExportGrid(
                            dg.Rows.Count,
                            dg.Columns.Count,
                            (r, c) => { return dg[c, r].Value; },
                            rowHeaderAccessor,
                            c => { return dg.Columns[c].HeaderText; });
                    }
                    // 書き出したファイルを外部エディタでオープンする
                    Process.Start(GUISetting.Instance.ExternalEditorPath, path);
                }
                else if (setting.ExportType == ExportType.R)
                {
                    var path = Path.GetTempFileName();
                    path = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".d";
                    using (var wr = new StreamWriter(path, false, new UTF8Encoding(false)))
                    {
                        svc = new ExportServiceR(setting, wr);
                        svc.ExportGrid(
                            dg.Rows.Count,
                            dg.Columns.Count,
                            (r, c) => dg[c, r].Value,
                            rowHeaderAccessor,
                            c => dg.Columns[c].HeaderText,
                            (from DataGridViewBand c in dg.Columns select c.Tag as string).ToArray());
                    }
                    // 書き出したファイルを外部エディタでオープンする
                    Process.Start(GUISetting.Instance.ExternalEditorPath, path);
                }
                else if (setting.ExportType == ExportType.Rda)
                {
                    var path = Path.GetTempFileName();
                    path = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".rda";
                    using (var wr = new StreamWriter(path, false, new UTF8Encoding(false)))
                    {
                        svc = new ExportServiceRdata(setting, wr);
                        svc.ExportGrid(
                            dg.Rows.Count,
                            dg.Columns.Count,
                            (r, c) => dg[c, r].Value,
                            rowHeaderAccessor,
                            c => dg.Columns[c].HeaderText,
                            (from DataGridViewBand c in dg.Columns select c.Tag as string).ToArray());
                    }
                    // 書き出したファイルをExplorerでオープンする
                    Process.Start("explorer.exe", Path.GetTempPath());
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while Exporting:", ex);
                edlg.ShowDialog();
            }
            finally
            {
                if (svc != null) svc.Dispose();
            }
        }

        /// <summary>
        /// 検索条件パネルに存在する現在の条件をファイルにセーブする
        /// </summary>
        /// <param name="filename">ファイルのパス名。nullならデフォルトの"LastSearch.xml"が使用される。</param>
        public void SaveSearchCond(string filename)
        {
            if (filename == null)
            {
                filename = Program.SettingDir + @"\LastSearch.xml";
            }
            SearchConditions cond = this.condPanel.GetConditions();
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                XmlSerializer ser = new XmlSerializer(typeof(SearchConditions));
                ser.Serialize(fs, cond);
            }
        }

        /// <summary>
        /// ファイルから検索条件をロードし、検索条件パネルをupdateする
        /// </summary>
        /// <param name="filename">ファイルのパス名。nullならデフォルトの"LastSearch.xml"が使用される。</param>
        public void LoadSearchCond(string filename)
        {
            if (filename == null)
            {
                filename = Program.SettingDir + @"\LastSearch.xml";
            }
            SearchConditions cond = this.condPanel.GetConditions();
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                XmlSerializer ser = new XmlSerializer(typeof(SearchConditions));
                cond = (SearchConditions)ser.Deserialize(fs);
            }
            this.condPanel.ChangeConditions(cond);
            m_Model.CurrentSearchConditions = cond;
        }

        protected void OnSaveConfig(XmlTextWriter xmlOut)
        {
            // DockState設定ファイルへの追加出力
            xmlOut.WriteStartElement("WindowLocation");
            xmlOut.WriteAttributeString("X", String.Format("{0}", this.Bounds.X));
            xmlOut.WriteAttributeString("Y", String.Format("{0}", this.Bounds.Y));
            xmlOut.WriteAttributeString("Width", String.Format("{0}", this.Bounds.Width));
            xmlOut.WriteAttributeString("Height", String.Format("{0}", this.Bounds.Height));
            xmlOut.WriteEndElement();
        }

        protected void OnLoadConfig(XmlTextReader xmlIn)
        {
            // DockState設定ファイルを読み込んだ時点での追加情報の処理
            if (xmlIn.Name == "WindowLocation")
            {
                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;
                bool b = true;
                b = b && Int32.TryParse(xmlIn.GetAttribute("X"), out x);
                b = b && Int32.TryParse(xmlIn.GetAttribute("Y"), out y);
                b = b && Int32.TryParse(xmlIn.GetAttribute("Width"), out width);
                b = b && Int32.TryParse(xmlIn.GetAttribute("Height"), out height);
                if (b)
                {
                    this.Bounds = NormalizeByPrimaryScreenSize(x, y, width, height, new Rectangle(0, 0, 800, 500));
                }

                xmlIn.Read();
            }
        }

        private static Rectangle NormalizeByPrimaryScreenSize(int x, int y, int width, int height, Rectangle defaultRect)
        {
            int maxWidth = Screen.PrimaryScreen.Bounds.Width;
            int maxHeight = Screen.PrimaryScreen.Bounds.Height;
            var bounds = new Rectangle(x, y, width, height);
            if (x < 0 || y < 0 || x > maxWidth - 50 || y > maxHeight - 50
                || width < 100 || height < 100 || width > maxWidth || height > maxHeight)
            {
                bounds = defaultRect;
            }
            return bounds;
        }

        private static Point NormalizeLocationByPrimaryScreenSize(int x, int y, Point defaultLoc)
        {
            int maxWidth = Screen.PrimaryScreen.Bounds.Width;
            int maxHeight = Screen.PrimaryScreen.Bounds.Height;
            var loc = new Point(x, y);
            if (x < 0 || y < 0 || x > maxWidth - 50 || y > maxHeight - 50)
            {
                loc = defaultLoc;
            }
            return loc;
        }

        /// <summary>
        /// 1. そのヒストリを元に条件パネルを再構成する。
        /// 2. Kwic/WordList/Collocationのビューを切り替える。
        /// 3. ビューの中身をクリアする。
        /// 4. ヒストリがセーブデータへのパスを持っていればViewにロードする。
        /// </summary>
        /// <param name="hist"></param>
        public void OnHistoryNavigating(SearchHistory hist)
        {
            SearchConditions cond = hist.CondSeq.Last;
            // ChangeModelは、condを元にコピーを生成し、CondPanelのModelにアサインする。同時にカレントにセットする.
            m_Model.CurrentSearchConditions = this.condPanel.ChangeConditions(cond);

            // 検索条件に適したView&タブに切り替え
            ChangeView(cond.ActiveSearch);

            this.commandPanel.SetModel(hist.Progress);
            this.kwicView.SetModel(hist);
            this.wordListView.SetModel(hist.LexemeList);
            this.collocationView.SetModel(hist.CollList);

            switch (cond.ActiveSearch)
            {
                case SearchType.SentenceSearch:
                    this.condPanel.SelectedTab = ConditionsPanelType.CP_CORPUS;
                    break;
                case SearchType.StringSearch:
                    this.condPanel.SelectedTab = ConditionsPanelType.CP_STRING;
                    break;
                case SearchType.TagSearch:
                case SearchType.TagWordList:
                    this.condPanel.SelectedTab = ConditionsPanelType.CP_TAG;
                    break;
                case SearchType.DepSearch:
                case SearchType.DepWordList:
                    this.condPanel.SelectedTab = ConditionsPanelType.CP_DEP;
                    break;
            }
        }

        /// <summary>
        /// Historyをファイルにセーブする.
        /// </summary>
        /// <param name="sender">SearchHistory</param>
        /// <param name="args"></param>
        public void OnHistorySaveRequested(object sender, EventArgs args)
        {
            SearchHistory hist = sender as SearchHistory;
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Chaki files (*.chaki)|*.chaki";
            dlg.Title = "Save History Node";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (Save(dlg.FileName, hist))
                {
                    hist.FilePath = dlg.FileName;
                }
            }
        }

        /// <summary>
        /// Historyを削除する.
        /// </summary>
        /// <param name="sender">SearchHistory</param>
        /// <param name="args"></param>
        public void OnDeleteHistoryRequested(object sender, EventArgs args)
        {
            SearchHistory hist = sender as SearchHistory;
            m_Model.DeleteHistory(hist);
            this.historyGuidePanel.UpdateView();
        }

        /// <summary>
        /// listのindex番目の文に対して、その前後の文脈をContext Panelに表示する。
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        public void OnRequestContext(KwicList list, int index)
        {
            KwicItem ki = list.Records[index];
            Corpus crps = ki.Crps;
            int senNo = ki.SenID;
            uint count = GUISetting.Instance.SearchSettings.ContextRetrievalRange;

            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.contextPanel.Model.SetTarget(crps, senNo, count);
            SentenceContextService svc = new SentenceContextService(this.contextPanel.Model);
            svc.UseSpacing = GUISetting.Instance.ContextPanelSettings.UseSpacing;
            try
            {
                svc.Begin();
                this.contextPanel.Model.CenterOffset = ki.GetCenterCharOffset(GUISetting.Instance.ContextPanelSettings.UseSpacing);
                this.contextPanel.Model.CenterLength = ki.GetCenterCharLength();
                this.contextPanel.UpdateView();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                this.Cursor = oldCur;
            }
        }

        public void OnRequestSentenceTagList(KwicList list, int index)
        {
            if (this.attributePanel.IsEditing)
            {
                return;
            }
            Corpus crps = list.Records[index].Crps;
            int senID = list.Records[index].SenID;

            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                List<AttributeBase> attrs = new List<AttributeBase>();
                using (var svc = new SentenceTagService(crps, senID))
                {
                    this.attributePanel.SetSource(crps, svc.Sen);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("SentenceTagService: Could not lock the Corpus.");
            }
            this.Cursor = oldCur;
        }

        /// <summary>
        /// 現在のKwicListの結果(SenIDと中心語Offset)に対して、Lexeme情報付きのKwicListを再取得する.
        /// StringSearch結果をTagSearch結果に変換する場合に用いる.
        /// </summary>
        public void ReloadKwicResult()
        {
            SearchHistory curModel = this.kwicView.GetModel();
            IServiceCommand svc = new RetrieveWithTagService(curModel.KwicList);
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                svc.Begin();
                this.kwicView.SetModel(curModel);
            }
            finally
            {
                this.Cursor = oldCur;
            }
        }

        /// <summary>
        /// Annotation (Segment, Link, Group)をロードする
        /// </summary>
        public void LoadAnnotations()
        {
            SearchHistory hist = hist = this.historyGuidePanel.Current;
            if (hist == null)
            {
                return;
            }
            AnnotationLoadOperation op = new AnnotationLoadOperation();
            op.Execute(hist);

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            kwicView.UpdateSegments(); // この更新方法は要考慮（callback化など）
            this.Cursor = oldCursor;
        }

        private class AnnotationLoadOperation
        {
            private SearchHistory m_History;
            private AnnotationService m_Service;
            private ProgressDialog m_Dlg;
            private BackgroundWorker m_Worker;
            private ManualResetEvent m_WaitDone;
            private bool m_CancelFlag;

            public AnnotationLoadOperation()
            {
                m_Service = new AnnotationService();
                m_WaitDone = new ManualResetEvent(false);
            }

            public void Execute(SearchHistory hist)
            {
                m_CancelFlag = false;
                m_History = hist;

                m_Dlg = new ProgressDialog();
                m_Dlg.Title = "Querying...";
                m_Dlg.ProgressMax = 100;
                m_Dlg.ProgressReset();
                m_Dlg.WorkerCancelled += new EventHandler(OnCancelled);
                m_Worker = new BackgroundWorker();
                m_Worker.WorkerReportsProgress = true;
                m_Worker.DoWork += new DoWorkEventHandler(OnLoadAnnotations);
                m_Worker.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                m_Worker.RunWorkerAsync(hist);
                m_Dlg.ShowDialog();
                m_WaitDone.WaitOne();
                m_Dlg.Dispose();
                m_Worker.Dispose();
            }

            void OnCancelled(object sender, EventArgs e)
            {
                m_CancelFlag = true;
            }

            void OnLoadAnnotations(object sender, DoWorkEventArgs e)
            {
                try
                {
                    AnnotationService svc = new AnnotationService();
                    SearchHistory hist = (SearchHistory)e.Argument;
                    svc.SetTagNameFilter(
                        TagSetting.Instance.GetVisibleNameList(ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT),
                        TagSetting.Instance.GetVisibleNameList(ChaKi.Entity.Corpora.Annotations.Tag.LINK),
                        TagSetting.Instance.GetVisibleNameList(ChaKi.Entity.Corpora.Annotations.Tag.GROUP));
                    svc.Load(hist.KwicList, hist.AnnotationList, new Action<int>((int v) => { m_Worker.ReportProgress(v); }), ref m_CancelFlag);

                }
                catch (Exception ex)
                {
                    ErrorReportDialog edlg = new ErrorReportDialog("Error while executing query commands:", ex);
                    edlg.ShowDialog();
                }
                finally
                {
                    m_Dlg.DialogResult = DialogResult.Cancel;
                    m_WaitDone.Set();
                }
            }

            void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
            {
                m_Dlg.ProgressCount = e.ProgressPercentage;
            }
        }
    }
}
