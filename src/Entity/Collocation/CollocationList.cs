using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;

namespace ChaKi.Entity.Collocation
{
    public class CollocationList
    {
        public Dictionary<Lexeme, List<DIValue>> Rows { get; set; }
        public List<ColumnDef> ColumnDefs { get; set; }
        public int NColumns
        {
            get
            {
                return this.ColumnDefs.Count;
            }
        }
        /// <summary>
        /// Property(Surface..)カラム群の第1カラムのみを使用する場合はこの値を
        /// セットする。カラムタイトル"Surface"がここでセットした文字列に置き換わる。
        /// 通常のPropertyを表示する場合はnullとする。
        /// </summary>
        public string FirstColTitle { get; set; }
        public CollocationCondition Cond { get; set; }


        public CollocationList(CollocationCondition cond)
        {
            this.Cond = new CollocationCondition(cond);  // 条件はこれ以降編集されて変更される場合があるので、コピーを保持する。
            this.Rows = new Dictionary<Lexeme, List<DIValue>>(new LexemeEqualityComparer());
            this.ColumnDefs = new List<ColumnDef>();
        }

        public void AddColumn(string title, ColumnType type, bool hasTotal)
        {
            this.ColumnDefs.Add(new ColumnDef() { Title = title, Type = type, HasTotal = hasTotal });
        }

        public List<DIValue> FindRow(Lexeme key)
        {
            List<DIValue> row = null;
            if (!this.Rows.TryGetValue(key, out row))
            {
                row = new List<DIValue>();
                for (int i = 0; i < this.NColumns; i++)
                {
                    row.Add(new DIValue());
                }
                this.Rows.Add(key, row);
            }
            return row;
        }

        public void DeleteAll()
        {
            this.Rows.Clear();
        }
    }
}
