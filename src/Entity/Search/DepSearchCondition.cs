using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ChaKi.Entity.Search
{
    public class DepSearchCondition : ISearchCondition, ICloneable
    {
        public List<TagSearchCondition> BunsetsuConds { get; set; }
        public List<LinkCondition> LinkConds { get; set; }

        public event EventHandler OnModelChanged;

        public DepSearchCondition()
        {
            this.BunsetsuConds = new List<TagSearchCondition>();
            this.LinkConds = new List<LinkCondition>();
        }

        public DepSearchCondition(DepSearchCondition src)
        {
            this.BunsetsuConds = new List<TagSearchCondition>();
            foreach (TagSearchCondition bc in src.BunsetsuConds)
            {
                this.BunsetsuConds.Add(new TagSearchCondition(bc));
            }
            this.LinkConds = new List<LinkCondition>();
            foreach (LinkCondition lc in src.LinkConds)
            {
                this.LinkConds.Add(new LinkCondition(lc));
            }
            OnModelChanged = src.OnModelChanged;
        }

        public object Clone()
        {
            return new DepSearchCondition(this);
        }

        public void Reset()
        {
            this.BunsetsuConds.Clear();
            TagSearchCondition tcond = new TagSearchCondition();
            tcond.Reset();
            this.BunsetsuConds.Add(tcond);

            this.LinkConds.Clear();

            if (OnModelChanged != null) OnModelChanged(this, null);
        }

        public void AddLinkConstraint(LinkCondition lc)
        {
            this.LinkConds.Add(lc);
        }

        public void RemoveLinkConstraint(LinkCondition lc)
        {
            this.LinkConds.Remove(lc);
        }

        public TagSearchCondition InsertBunsetsuCondAt(int pos)
        {
            TagSearchCondition item = null;

            bool rightmost = (pos == BunsetsuConds.Count);  // 左右端に追加した場合は、ラベルを付け替える。
            bool leftmost = (pos == 0);
            char oldConnL;
            char oldConnR;
            if (rightmost)
            {
                oldConnL = BunsetsuConds[pos - 1].LeftConnection;
                oldConnR = BunsetsuConds[pos - 1].RightConnection;
            }
            else
            {
                oldConnL = BunsetsuConds[pos].LeftConnection;
                oldConnR = BunsetsuConds[pos].RightConnection;
            }
            try
            {
                item = new TagSearchCondition();
                item.Reset();
                BunsetsuConds.Insert(pos, item);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.WriteLine(e);
                return null;
            }

            if (rightmost && oldConnR != ' ')
            {
                BunsetsuConds[BunsetsuConds.Count - 1].RightConnection = oldConnR;
            }
            if (leftmost && oldConnL != ' ')
            {
                BunsetsuConds[0].LeftConnection = oldConnL;
            }
            Check();
            if (OnModelChanged != null) OnModelChanged(this, null);
            return item;
        }

        public void RemoveBunsesuCondAt(int i)
        {
            bool rightmost = (i == BunsetsuConds.Count - 1);  // 左右端を削除した場合は、ラベルを付け替える。
            bool leftmost = (i == 0);
            char oldConnL = BunsetsuConds[i].LeftConnection;
            char oldConnR = BunsetsuConds[i].RightConnection;
            this.BunsetsuConds.RemoveAt(i);
            if (rightmost && oldConnR != ' ')
            {
                this.BunsetsuConds[i - 1].RightConnection = oldConnR;
            }
            if (leftmost && oldConnL != ' ')
            {
                this.BunsetsuConds[0].LeftConnection = oldConnL;
            }
            Check();
            if (OnModelChanged != null) OnModelChanged(this, null);
        }

        public void SetConnection(int bunsetsu_pos, char conn_char)
        {
            TagSearchCondition tcond;
            // bunsetsu_posの左側
            if (bunsetsu_pos >= 0 && bunsetsu_pos < BunsetsuConds.Count)
            {
                tcond = BunsetsuConds[bunsetsu_pos];
                tcond.LeftConnection = conn_char;
            }
            // bunsetsu_pos-1の右側
            if (bunsetsu_pos-1 >= 0 && bunsetsu_pos-1 < BunsetsuConds.Count)
            {
                tcond = BunsetsuConds[bunsetsu_pos - 1];
                tcond.RightConnection = conn_char;
            }
            // View updateは行わない
        }


        public void InsertLexemeConditionAt(int bunsetsu_pos, int property_pos)
        {
            try
            {
                TagSearchCondition item = BunsetsuConds[bunsetsu_pos];
                item.InsertAt(property_pos);    // itemにcallbackを設定していないため、ここではModel Updateは起きない。
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.WriteLine(e);
                return;
            }
            if (OnModelChanged != null) OnModelChanged(this, null);
        }

        private void Check()
        {
            if (this.BunsetsuConds.Count == 0)
            {
                return;
            }

            char ch;
            ch = this.BunsetsuConds[0].LeftConnection;
            if (ch != ' ' && ch != '^')
            {
                this.BunsetsuConds[0].LeftConnection = ' ';
            }
            for (int i = 1; i < this.BunsetsuConds.Count; i++)
            {
                // 中間で許可されるConnectionは、' ', '-', '<'のみ
                ch = this.BunsetsuConds[i].LeftConnection;
                if (ch != ' ' && ch != '-' && ch != '<')
                {
                    this.BunsetsuConds[i].LeftConnection = ' ';
                }
                // 直前のBunsetsuCondのRightと一致させる
                ch = this.BunsetsuConds[i-1].RightConnection;
                if (ch != this.BunsetsuConds[i].LeftConnection)
                {
                    this.BunsetsuConds[i - 1].RightConnection = this.BunsetsuConds[i].LeftConnection;
                }
            }
            ch = this.BunsetsuConds[this.BunsetsuConds.Count - 1].RightConnection;
            if (ch != ' ' && ch != '$')
            {
                this.BunsetsuConds[this.BunsetsuConds.Count - 1].RightConnection = ' ';
            }

            // Linkをチェック
            List<LinkCondition> tmp = new List<LinkCondition>();
            foreach (LinkCondition lc in this.LinkConds)
            {
                if (lc.SegidFrom < 0 || lc.SegidFrom >= this.BunsetsuConds.Count
                 || lc.SegidTo < 0 || lc.SegidTo >= this.BunsetsuConds.Count
                 || lc.SegidFrom == lc.SegidTo)
                {
                    continue;
                }
                tmp.Add(lc);
            }
            this.LinkConds = tmp;
        }

        /// <summary>
        /// このDepSearch条件に含まれるPivotを返す。
        /// </summary>
        /// <returns>Pivotがあれば[0..LexemeConds.Count-1]のいずれかの値。なければ-1</returns>
        public LexemeCondition GetPivot()
        {
            foreach (TagSearchCondition item in this.BunsetsuConds)
            {
                LexemeCondition c = item.GetPivot();
                if (c != null)
                {
                    return c;
                }
            }
            return null;
        }

        /// <summary>
        /// このDepSearch条件に含まれるPivotの位置を返す。
        /// </summary>
        /// <returns>Pivotがあれば[0..LexemeConds.Count-1]のいずれかの値。なければ-1</returns>
        public int GetPivotPos()
        {
            int i = 0;
            foreach (TagSearchCondition tcond in this.BunsetsuConds)
            {
                for (int j = 0; j < tcond.LexemeConds.Count; j++)
                {
                    LexemeCondition lcond = tcond.LexemeConds[j];
                    if (lcond.IsPivot)
                    {
                        return i;
                    }
                    i++;
                }
            }
            return -1;
        }

        /// <summary>
        /// 文節を無視して、前からこの条件に含まれるすべてのLexeme条件をリストする
        /// </summary>
        /// <returns></returns>
        public List<LexemeCondition> GetLexemeCondList()
        {
            List<LexemeCondition> res = new List<LexemeCondition>();
            foreach (TagSearchCondition tcond in this.BunsetsuConds)
            {
                res.AddRange(tcond.LexemeConds);
            }
            return res;
        }

        /// <summary>
        /// 文節を無視して、この条件に含まれるすべてのLexeme条件の数を得る
        /// </summary>
        /// <returns></returns>
        public int GetLexemeCondListCount()
        {
            int n = 0;
            foreach (TagSearchCondition tcond in this.BunsetsuConds)
            {
                n += tcond.Count;
            }
            return n;
        }

        /// <summary>
        /// 文節を無視して、前からこの条件に含まれるすべてのLexeme条件をリストした場合の
        /// Lexeme条件数とPivot位置を得る.
        /// </summary>
        /// <returns></returns>
        public void GetLexemeCondParams(out int count, out int pivotPos)
        {
            count = 0;
            pivotPos = -1;
            foreach (TagSearchCondition tcond in this.BunsetsuConds)
            {
                int localPivotPos = tcond.GetPivotPos();
                if (localPivotPos >= 0)
                {
                    pivotPos = count + localPivotPos;
                }
                count += tcond.Count;
            }
        }
    }
}
