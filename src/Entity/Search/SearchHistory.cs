using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Search;
using ChaKi.Entity.Kwic;
using System.Xml.Serialization;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Collocation;

namespace ChaKi.Entity.Search
{
    public class SearchHistoryResetEventArgs : EventArgs
    {
    }

    public class SearchHistoryAddNodeEventArgs : EventArgs
    {
        public SearchHistory Hist;

        public SearchHistoryAddNodeEventArgs(SearchHistory hist)
        {
            this.Hist = hist;
        }
    }

    public class SearchHistory
    {
        public event EventHandler OnUpdateModel;

        // ツリー構造
        public static SearchHistory Root { get; set; }
        [XmlIgnore]
        public List<SearchHistory> Children { get; set; }
        [XmlIgnore]
        public SearchHistory Parent { get; set; }

        // ノード自身の内容
        public string Name { get; set; }
        public string FilePath { get; set; }
        public SearchConditionsSequence CondSeq { get; set; }
        public KwicList KwicList { get; set; }
        [XmlIgnore]
        public AnnotationList AnnotationList { get; set; }
        [XmlIgnore]
        public LexemeCountList LexemeList { get; set; }
        [XmlIgnore]
        public CollocationList CollList { get; set; }
        [XmlIgnore]
        public CommandProgress Progress { get; set; }

        private int m_ShiftPos;     // Centerのシフト値
        private int m_HilightPos;   // Hilightのシフト値

        // 履歴固有名称の元となる数値
        private static int seqno;

        static SearchHistory()
        {
            Root = new SearchHistory();
            seqno = 1;
        }


        /// <summary>
        /// 条件に応じた検索履歴モデルを生成する
        /// </summary>
        /// <param name="cond"></param>
        /// <returns></returns>
        public static SearchHistory Create(SearchConditionsSequence condSeq)
        {
            SearchHistory obj = new SearchHistory();
            obj.CondSeq = condSeq;
            obj.Progress = new CommandProgress();
            switch (condSeq.Last.ActiveSearch)
            {
                case SearchType.SentenceSearch:
                case SearchType.StringSearch:
                case SearchType.TagSearch:
                case SearchType.DepSearch:
                    obj.KwicList = new KwicList();
                    obj.AnnotationList = new AnnotationList();
                    break;
                case SearchType.TagWordList:
                    obj.LexemeList = new LexemeCountList(obj.CondSeq.Last.TagCond);
                    break;
                case SearchType.DepWordList:
                    obj.LexemeList = new LexemeCountList(obj.CondSeq.Last.DepCond);
                    break;
                case SearchType.Collocation:
                    obj.CollList = new CollocationList(obj.CondSeq.Last.CollCond);
                    break;
            }
            obj.Name = string.Format("[{0}]{1}", seqno++, condSeq.Last.ActiveSearch.ToString());
            return obj;
        }

        public SearchHistory()
        {
            this.Children = new List<SearchHistory>();
            this.Parent = null;
            this.Name = "/";
            this.FilePath = null;
            this.CondSeq = null;
            this.KwicList = null;
            this.LexemeList = null;
            this.CollList = null;
            this.Progress = new CommandProgress();
            this.AnnotationList = new AnnotationList();

            m_ShiftPos = 0;
            m_HilightPos = 0;
        }

        public SearchHistory(SearchHistory hist)
        {
            this.Children = new List<SearchHistory>();
            this.Parent = hist.Parent;
            this.Name = hist.Name;
            this.FilePath = hist.FilePath;
            this.CondSeq = hist.CondSeq;
            this.KwicList = hist.KwicList;
            this.AnnotationList = hist.AnnotationList;
            this.LexemeList = hist.LexemeList;
            this.CollList = hist.CollList;
            this.Progress = hist.Progress;

            m_ShiftPos = hist.m_ShiftPos;
            m_HilightPos = hist.m_HilightPos;
        }

        public void Reset()
        {
            Children.Clear();
            Parent = null;

            if (Root.OnUpdateModel != null)
            {
                Root.OnUpdateModel(this, new SearchHistoryResetEventArgs());
            }
        }

        /// <summary>
        /// このノードの内容をクリアする
        /// </summary>
        public void DeleteAll()
        {
            if (this.KwicList != null)
            {
                this.KwicList.DeleteAll();
            }
            if (this.LexemeList != null)
            {
                this.LexemeList.Clear();
            }
            if (this.CollList != null)
            {
                this.CollList.DeleteAll();
            }

        }

        /// <summary>
        /// 自分の持つ子孫からhistと一致するヒストリ項目を探し、削除する.
        /// histが子を持つ場合は一緒に削除される.
        /// </summary>
        /// <param name="hist"></param>
        public void Delete(SearchHistory hist)
        {
            if (hist == null || hist.Name == null)
            {
                return;
            }
            foreach (SearchHistory h in Children)
            {
                if (h.Name != null && h.Name.Equals(hist.Name))
                {
                    Children.Remove(h);
                    return;
                }
                h.Delete(hist);
            }
        }

        public void AddChild(SearchHistory hist)
        {
            this.Children.Add(hist);
            hist.Parent = this;

            if (Root.OnUpdateModel != null)
            {
                Root.OnUpdateModel(this, new SearchHistoryAddNodeEventArgs(hist));
            }
        }

        public SearchHistory FindHistory(string p)
        {
            if (this.Name == null)
            {
                return null;
            }
            if (this.Name.Equals(p))
            {
                return this;
            }
            // Search recursively
            foreach (SearchHistory hist in this.Children)
            {
                SearchHistory found = hist.FindHistory(p);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        public SearchConditions Shift(int shift)
        {
            if (this.KwicList == null)
            {
                return null;
            }
            m_ShiftPos += shift;
            this.KwicList.Shift(shift);

            var searchcond = this.CondSeq[this.CondSeq.Count - 1];
            var tcond = searchcond.TagCond;
            if (tcond != null)
            {
                tcond.Shift(shift);
            }
            return searchcond;
        }

        public void ShiftHilight(int shift)
        {
            if (this.KwicList == null)
            {
                return;
            }
            m_HilightPos += shift;
            this.KwicList.SetHilight(shift);
        }

        public bool CanShift()
        {
            return (this.KwicList != null);
        }
    }
}
