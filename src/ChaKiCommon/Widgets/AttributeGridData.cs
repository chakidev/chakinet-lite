using System;
using System.Linq;
using System.Collections.Generic;

namespace ChaKi.Common
{
    /// <summary>
    /// ViewModel for AttributeGridData
    /// </summary>
    public class AttributeGridData
    {
        public AttributeGridData()
        {
            this.Rows = new List<AttributeGridRowData>();
            this.SelectedRows = new List<int>();
        }

        public AttributeGridData(AttributeGridData src)
            : this()
        {
            CopyFrom(src);
        }

        public void CopyFrom(AttributeGridData src)
        {
            this.Rows = new List<AttributeGridRowData>();
            src.Rows.ForEach(r => this.Rows.Add(r.Clone()));

            this.SelectedRows.Clear();
            src.SelectedRows.ForEach(i => this.SelectedRows.Add(i));
        }

        public List<AttributeGridRowData> Rows { get; set; }

        public bool IsEditing { get; set; }

        public List<int> SelectedRows { get; set; }

        public GroupData FindGroupOf(int row)
        {
            for (int i = row; i >= 0; i--)
            {
                var group = this.Rows[i] as GroupData;
                if (group != null)
                {
                    return group;
                }
            }
            return null;
        }

        // Model Operations
        public void ChangeValue(int row, int col, string val)
        {
            if (row < 0 || row >= this.Rows.Count)
            {
                throw new Exception(string.Format("Row count mismatch. row={0}, rowcount={1}", row, this.Rows.Count));
            }
            if (col != 0 && col != 1)
            {
                throw new Exception(string.Format("Invalid col. col={0}", col));
            }
            AttributeData ad = this.Rows[row] as AttributeData;
            if (ad == null)
            {
                throw new Exception("Cannot change value of GroupData");
            }
            switch (col)
            {
                case 0:
                    ad.Key = val;
                    break;
                case 1:
                    ad.Value = val;
                    break;
            }
        }

        public bool ToggleGroupExpansion(int index)
        {
            if (index >= this.Rows.Count)
            {
                return false;
            }
            var group = this.Rows[index] as GroupData;
            if (group == null)
            {
                return false;
            }
            group.IsExpanded = !group.IsExpanded;
            return true;
        }

        public void PasteData(string[] data, int rowAt)
        {
            foreach (string line in data)
            {
                string[] vals = line.Split('\t');
                //列数が合っているか
                if (vals.Length != 2)
                {
                    throw new Exception("Column count mismatch.");
                }
                this.Rows.Insert(rowAt + 1, new AttributeData(vals[0], vals[1], AttributeGridRowType.KeyValueWritable));
                rowAt++;
            }
        }

        public void InsertRow(int rowAt)
        {
            InsertRow(rowAt, string.Empty, string.Empty, AttributeGridRowType.KeyValueWritable);
        }

        public void InsertRow(int rowAt, string key, string value, AttributeGridRowType rowType)
        {
            this.Rows.Insert(rowAt + 1, new AttributeData(key, value, rowType));
        }

        /// <summary>
        /// 行を削除する. rowsは降順になっていること.
        /// GroupHeader行とReadOnly行は削除されない.
        /// </summary>
        /// <param name="rows"></param>
        public void RemoveRows(IEnumerable<int> rows)
        {
            foreach (int row in rows)
            {
                var ad = this.Rows[row] as AttributeData;
                if (ad != null && ad.RowType == AttributeGridRowType.KeyValueWritable)
                {
                    this.Rows.RemoveAt(row);
                }
            }
        }

        /// <summary>
        /// グループごとに、2つ以上の空白属性があれば削除する. Sort後に使用すること.
        /// </summary>
        public void RemoveExcessOfEmptyAttribute()
        {
            int count = 0;
            var remove_indexes = new List<int>();
            for (int i = 0; i < this.Rows.Count; i++)
            {
                if (this.Rows[i] is GroupData)
                {
                    count = 0;  // reset
                }
                else if (this.Rows[i] is AttributeData)
                {
                    var ad = (AttributeData)Rows[i];
                    if (ad.Key.Length == 0)
                    {
                        count++;
                    }
                    if (count > 1)
                    {
                        remove_indexes.Add(i);
                    }
                }
            }
            // 後ろから行削除
            remove_indexes.Reverse();
            foreach (var index in remove_indexes)
            {
                this.Rows.RemoveAt(index);
            }
        }

        /// <summary>
        /// Rowをソートする. グループが同じものは連続するように並び、
        /// グループ内ではReadOnly属性が先頭に、その他を後にして、各々属性名のアルファベット昇順にソートする.
        /// Keyが空白の行は各グループの最後にする.
        /// </summary>
        public void Sort()
        {
            var comp = new AttributeGridRowComparer();

            int last = -1;
            int count = 0;
            for (int i = 0; i < this.Rows.Count; i++)
            {
                if (this.Rows[i] is GroupData)
                {
                    if (last >= 0 && count > 0)
                    {
                        this.Rows.Sort(last + 1, count, comp);
                    }
                    last = i;
                    count = 0;
                }
                else if (this.Rows[i] is AttributeData)
                {
                    count++;
                }
            }
            if (last >= 0 && count > 0)
            {
                this.Rows.Sort(last + 1, count, comp);
            }
        }
    }

    class AttributeGridRowComparer : IComparer<AttributeGridRowData>
    {
        public int Compare(AttributeGridRowData x, AttributeGridRowData y)
        {
            if (x is GroupData && y is AttributeData) return -1;
            if (x is AttributeData && y is GroupData) return 1;
            if (x.RowType == AttributeGridRowType.ReadOnly && y.RowType != AttributeGridRowType.ReadOnly) return -1;
            if (x.RowType != AttributeGridRowType.ReadOnly && y.RowType == AttributeGridRowType.ReadOnly) return 1;
            if (x.Name.Length == 0 && y.Name.Length > 0) return 1;
            if (x.Name.Length > 0 && y.Name.Length == 0) return -1;
            return string.Compare(x.Name, y.Name);
        }
    }

    public abstract class AttributeGridRowData
    {
        public virtual AttributeGridRowType RowType { get; protected set; }

        public bool IsSelected { get; set; }

        public virtual string Name { get; set; }

        public abstract AttributeGridRowData Clone();
    }

    public enum AttributeGridRowType
    {
        ReadOnly,
        ValueWritable,
        KeyValueWritable,
    }

    public class GroupData : AttributeGridRowData
    {
        public const string DOCUMENT = "Document Attributes";
        public const string SENTENCE = "Sentence Attributes";
        public const string SEGMENT = "Segment Attributes";
        public const string LINK = "Link Attributes";
        public const string GROUP = "Group Attributes";

        public GroupData(string groupName)
        {
            this.Name = groupName;
            this.IsExpanded = true;
        }
        
        public bool IsExpanded { get; set; }

        public override AttributeGridRowType RowType
        {
            get { return AttributeGridRowType.ReadOnly; }
        }

        public override AttributeGridRowData Clone()
        {
            return new GroupData(this.Name) { IsExpanded = this.IsExpanded, IsSelected = this.IsSelected, RowType = this.RowType };
        }
    }

    public class AttributeData : AttributeGridRowData
    {
        public override string Name
        {
            get { return this.Key; }
            set { this.Key = value; }
        }
        public string Key { get; set; }

        public string Value { get; set; }

        public AttributeData(string key, string value, AttributeGridRowType rowType)
        {
            this.Key = key;
            this.Value = value;
            this.RowType = rowType;
        }

        public override AttributeGridRowData Clone()
        {
            return new AttributeData(this.Key, this.Value, this.RowType);
        }
    }
}
