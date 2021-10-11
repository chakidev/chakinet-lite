using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ChaKi.Entity.Search
{
    public class TagSearchCondition : ISearchCondition, ICloneable
    {
        public List<LexemeCondition> LexemeConds { get; set; }
        public char LeftConnection { get; set; }
        public char RightConnection { get; set; }
        /// <summary>
        /// DepSearch条件においてのみ使用される、Segmentに対するTag条件
        /// </summary>
        public string SegmentTag { get; set; }

        public SerializableDictionary<string, string> SegmentAttrs { get; set; }

        public event EventHandler ModelChanged;

        public TagSearchCondition()
        {
            this.LexemeConds = new List<LexemeCondition>();
            this.SegmentTag = "Bunsetsu";
            this.SegmentAttrs = new SerializableDictionary<string, string>();
        }

        public TagSearchCondition(TagSearchCondition src)
        {
            this.LexemeConds = new List<LexemeCondition>();
            foreach (LexemeCondition lexcond in src.LexemeConds)
            {
                LexemeConds.Add(new LexemeCondition(lexcond));
            }
            this.LeftConnection = src.LeftConnection;
            this.RightConnection = src.RightConnection;
            this.SegmentTag = src.SegmentTag;
            this.SegmentAttrs = new SerializableDictionary<string, string>();
            foreach (var pair in src.SegmentAttrs)
            {
                this.SegmentAttrs.Add(pair.Key, pair.Value);
            }
            ModelChanged = src.ModelChanged;
        }

        public object Clone()
        {
            return new TagSearchCondition(this);
        }

        public void Reset()
        {
            LexemeConds.Clear();
            LeftConnection = ' ';
            RightConnection = ' ';
            this.SegmentTag = "Bunsetsu";
            this.SegmentAttrs.Clear();
            // デフォルトで１つの項目は作成する
            LexemeCondition item = new LexemeCondition();
            item.IsPivot = true;
            LexemeConds.Add(item);
            if (ModelChanged != null) ModelChanged(this, null);
        }

        public int Count
        {
            get { return LexemeConds.Count; }
        }

        /// <summary>
        /// 左にひとつLexeme条件を追加する。
        /// </summary>
        public LexemeCondition InsertLexemeConditionAtLeft()
        {
            LexemeCondition item = null;
            try
            {
                LexemeCondition right = this.LexemeConds[0];
                item = new LexemeCondition();
                item.RelativePosition = new Range(right.RelativePosition.Start-1,right.RelativePosition.Start-1);
                LexemeConds.Insert(0, item);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.WriteLine(e);
                return null;
            }
            if (ModelChanged != null) ModelChanged(this, null);
            return item;
        }

        /// <summary>
        /// 右にひとつLexeme条件を追加する。
        /// </summary>
        public LexemeCondition InsertLexemeConditionAtRight()
        {
            LexemeCondition item = null;
            try
            {
                LexemeCondition left = this.LexemeConds[LexemeConds.Count - 1];
                item = new LexemeCondition();
                item.RelativePosition = new Range(left.RelativePosition.End + 1, left.RelativePosition.End + 1);
                LexemeConds.Add(item);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.WriteLine(e);
                return null;
            }
            if (ModelChanged != null) ModelChanged(this, null);
            return item;
        }

        /// <summary>
        /// 指定位置にひとつLexeme条件を追加する。
        /// 追加された条件のRelativePositionは初期値となる。
        /// </summary>
        /// <param name="i"></param>
        public LexemeCondition InsertAt(int i)
        {
            bool rightmost = (i == LexemeConds.Count);  // 左右端に追加した場合は、ラベルを付け替える。
            bool leftmost = (i == 0);
            char oldConnL = ' ';
            char oldConnR = ' ';
            if (LexemeConds.Count > 0)
            {
                if (rightmost)
                {
                    oldConnL = LexemeConds[i - 1].LeftConnection;
                    oldConnR = LexemeConds[i - 1].RightConnection;
                }
                else
                {
                    oldConnL = LexemeConds[i].LeftConnection;
                    oldConnR = LexemeConds[i].RightConnection;
                }
            }

            LexemeCondition item = new LexemeCondition();
            LexemeConds.Insert(i, item);

            if (rightmost && oldConnR != ' ')
            {
                LexemeConds[LexemeConds.Count-1].RightConnection = oldConnR;
            }
            if (leftmost && oldConnL != ' ')
            {
                LexemeConds[0].LeftConnection = oldConnL;
            }
            Check();
            if (ModelChanged != null) ModelChanged(this, null);
            return item;
        }

        public void RemoveAt(int i)
        {
            bool rightmost = (i == LexemeConds.Count - 1);  // 左右端を削除した場合は、ラベルを付け替える。
            bool leftmost = (i == 0);
            char oldConnL = LexemeConds[i].LeftConnection;
            char oldConnR = LexemeConds[i].RightConnection;

            LexemeConds.RemoveAt(i);

            if (LexemeConds.Count > 0)
            {
                if (rightmost && oldConnR != ' ')
                {
                    LexemeConds[i - 1].RightConnection = oldConnR;
                }
                if (leftmost && oldConnL != ' ')
                {
                    LexemeConds[0].LeftConnection = oldConnL;
                }
                Check();
            }
            if (ModelChanged != null) ModelChanged(this, null);
        }

        private void Check()
        {
            if (this.Count == 0)
            {
                return;
            }
            // 左右両端の接続条件をチェック
            char ch;
            ch = this.LexemeConds[0].LeftConnection;
            if (ch != ' ' && ch != '^')
            {
                this.LexemeConds[0].LeftConnection = ' ';
            }
            for (int i = 1; i < this.Count; i++)
            {
                // 中間で許可されるConnectionは、' ', '-', '<'のみ
                ch = this.LexemeConds[i].LeftConnection;
                if (ch != ' ' && ch != '-' && ch != '<')
                {
                    this.LexemeConds[i].LeftConnection = ' ';
                }
                // 直前のBunsetsuCondのRightと一致させる
                ch = this.LexemeConds[i - 1].RightConnection;
                if (ch != this.LexemeConds[i].LeftConnection)
                {
                    this.LexemeConds[i - 1].RightConnection = this.LexemeConds[i].LeftConnection;
                }
            } 
            ch = this.LexemeConds[this.Count - 1].RightConnection;
            if (ch != ' ' && ch != '$')
            {
                this.LexemeConds[this.Count - 1].RightConnection = ' ';
            }
        }

        public void SetConnection(int pos, char conn_char)
        {
            LexemeCondition lcond;
            // posの左側
            if (pos >= 0 && pos < LexemeConds.Count)
            {
                lcond = LexemeConds[pos];
                lcond.LeftConnection = conn_char;
            }
            // pos-1の右側
            if (pos-1 >= 0 && pos-1 < LexemeConds.Count)
            {
                lcond = LexemeConds[pos - 1];
                lcond.RightConnection = conn_char;
            }
            // View updateは行わない
        }


        /// <summary>
        /// このTagSearch条件に含まれるPivotを返す。
        /// </summary>
        /// <returns>Pivotがあれば[0..LexemeConds.Count-1]のいずれかの値。なければ-1</returns>
        public LexemeCondition GetPivot()
        {
            foreach (LexemeCondition item in this.LexemeConds)
            {
                if (item.IsPivot)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// このTagSearch条件に含まれるPivotの位置を返す。
        /// </summary>
        /// <returns>Pivotがあれば[0..LexemeConds.Count-1]のいずれかの値。なければ-1</returns>
        public int GetPivotPos()
        {
            for (int i = 0; i < this.LexemeConds.Count; i++)
            {
                if (this.LexemeConds[i].IsPivot)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Shift(int shift)
        {
            foreach (LexemeCondition lcond in this.LexemeConds)
            {
                lcond.OffsetRange(shift);
            }
            // 位置0を含むLexemeConditionのみ、isPivot=trueとする
            bool pivotFound = false;
            foreach (LexemeCondition lcond in this.LexemeConds)
            {
                if (lcond.RelativePosition.Start <= 0 && lcond.RelativePosition.End >= 0 && !pivotFound)
                {
                    lcond.IsPivot = true;
                    pivotFound = true;
                }
                else
                {
                    lcond.IsPivot = false;
                }
            }
            if (ModelChanged != null) ModelChanged(this, null);
        }
    }
}
