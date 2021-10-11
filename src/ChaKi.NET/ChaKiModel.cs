using ChaKi.Common;
using ChaKi.Common.Widgets;
using ChaKi.Entity;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;
using ChaKi.Entity.Settings;
using ChaKi.GUICommon;
using ChaKi.Service.Database;
using ChaKi.ToolDialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace ChaKi
{
    public class ChaKiModel
    {
        /// <summary>
        /// CurrentCorpusが変化したときの通知イベント
        /// </summary>
        public static event CurrentChangedDelegate OnCurrentChanged;

        public static ChaKiModel Instance = new ChaKiModel();

        // Initialize()でのロード進捗レポート用
        private ProgressDialog m_Dlg;
        private BackgroundWorker m_Worker;
        private ManualResetEvent m_WaitDone;
        private bool m_CancelFlag;
        private SynchronizationContext m_Context;

        private ChaKiModel()
        {
            History = SearchHistory.Root;
            CurrentSearchConditions = new SearchConditions();
        }

        /// <summary>
        /// システムで唯一保持される、カレントコーパスの参照
        /// この値はさまざまな場合に変更される
        /// 変更要因：
        /// (1) Search Conditions/Corpusタブでユーザがコーパスのリストボックスの選択状態を変更したとき
        /// (2) 検索実行中に、検索対象のコーパスが変化したとき
        /// (3) KwicViewのカレント選択行が変更されたとき（選択行の元コーパスに変更される）
        /// (4) KwicViewでマウスHoverイベントが発生したとき（Hover行の元コーパスに変更される）
        /// 参照するクラス：
        /// (a) このクラス自身が、CurrentCorpusの設定により非表示属性を決定する
        /// (b) PropertyBoxが、CurrentCorpusの設定により非表示属性を決定する
        /// (c) GuidePanelが、表示更新前にCurrentCorpusの設定により非表示属性を決定する
        /// なお、CurrentCorpus == nullの場合は、デフォルトの属性がすべて表示される。
        /// </summary>
        public static Corpus CurrentCorpus
        {
            get
            {
                return Instance.m_currentCorpus;
            }
            set
            {
                Instance.m_currentCorpus = value;
                if (OnCurrentChanged != null)
                {
                    OnCurrentChanged(Instance.m_currentCorpus, -1);
                }
            }
        }

        private Corpus m_currentCorpus;

        public SearchHistory History { get; private set; }
        public SearchConditions CurrentSearchConditions { get; set; }

        public static IList<Corpus> CurrentCorpusList
        {
            get
            {
                return Instance.CurrentSearchConditions.SentenceCond.Corpora;
            }
        }

        public void Reset()
        {
            History.Reset();
            CurrentSearchConditions.Reset();
        }

        public void DeleteHistory(SearchHistory hist)
        {
            History.Delete(hist);
        }

        /// <summary>
        /// モデルの初期化を行う。アプリケーション開始時に呼び出される。
        /// ・CurrentCorpusを前回最後に使用した状態にする.
        /// ・CurrentSearchConditions.CollCondを前回最後に使用した状態にする.
        /// </summary>
        public void Initialize()
        {
            Reset();

            ChaKiModel.CurrentCorpus = null;

            // TagSelectorの内容をLoadMandatoryCorpusInfo()のcallbackによって設定する
            TagSelector.ClearAllTags();

            // 前回使用したコーパスリストをロードする.
            var list = UserSettings.GetInstance().LastCorpus;
            // ロードするファイルの数が10を越えたらProgress dialogを出す
            if (list.Count > 10)
            {
                m_WaitDone = new ManualResetEvent(false);
                m_Context = SynchronizationContext.Current;
                using (m_Dlg = new ProgressDialog())
                {
                    m_Dlg.Title = $"Loading Corpus List (Count={list.Count})...";
                    m_Dlg.ProgressMax = 100;
                    m_Dlg.ProgressReset();
                    m_Dlg.WorkerCancelled += (s, e) => { m_CancelFlag = true; };
                    using (m_Worker = new BackgroundWorker())
                    {
                        m_Worker.WorkerReportsProgress = true;
                        m_Worker.DoWork += (obj, ea) => { LoadCorpusList((List<Corpus>)ea.Argument); };
                        m_Worker.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                        m_Worker.RunWorkerAsync(list);
                        m_Dlg.ShowDialog();
                        m_WaitDone.WaitOne();
                    }
                }
            }
            else
            {
                LoadCorpusList(list);
            }

            CollocationCondition collCond = UserSettings.GetInstance().DefaultCollCond;
            if (collCond != null)
            {
                CurrentSearchConditions.CollCond = collCond;
            }
        }

        private void LoadCorpusList(List<Corpus> corpora)
        {
            m_CancelFlag = false;
            int i = 0;
            foreach (Corpus c in UserSettings.GetInstance().LastCorpus)
            {
                if (m_CancelFlag)
                {
                    break;
                }
                if (m_Worker != null)
                {
                    m_Worker.ReportProgress((int)(i * 100.0 / corpora.Count));
                }
                i++;
                try
                {
                    if (c.DBParam.DBType != "SQLite")
                    {
                        // PasswordはDBParameters経由では保存されないので、defファイルを読み直す.
                        c.DBParam.ParseDefFile(c.DBParam.DBPath);
                    }
                    DBService dbs = DBService.Create(c.DBParam);

                    dbs.LoadSchemaVersion(c);
                    // Schemaチェック
                    if (c.Schema.Version < CorpusSchema.CurrentVersion)
                    {
                        DBSchemaConversion dlg = new DBSchemaConversion(dbs, c);
                        dlg.DoConversion();
                    }

                    dbs.LoadMandatoryCorpusInfo(c, (s, t) =>
                    {
                        if (m_Context != null)
                        {
                            var callback = new SendOrPostCallback(state => { TagSelector.PreparedSelectors[s].AddTag(t, c.Name); });
                            m_Context.Post(callback, null);
                        }
                        else
                        {
                            TagSelector.PreparedSelectors[s].AddTag(t, c.Name);
                        }
                    });

                    if (ChaKiModel.CurrentCorpus == null)
                    {
                        ChaKiModel.CurrentCorpus = c;
                    }
                }
                catch (Exception ex)
                {
                    ErrorReportDialog err = new ErrorReportDialog("Cannot load default corpus", ex);
                    err.ShowDialog();
                    continue;
                }
                CurrentSearchConditions.SentenceCond.Corpora.Add(c);
            }
            if (m_Dlg != null)
            {
                m_Dlg.DialogResult = DialogResult.Cancel;
            }
            if (m_WaitDone != null)
            {
                m_WaitDone.Set();
            }
        }

        void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            m_Dlg.ProgressCount = e.ProgressPercentage;
            var detail = e.UserState as int[];
            if (detail != null && detail.Length == 2)
            {
                m_Dlg.ProgressDetail = detail;
            }
        }
    }
}
