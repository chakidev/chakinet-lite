using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.VirtualGrid;
using System.Data;

namespace ChaKi.ToolDialogs
{
    /// <summary>
    /// 参考ページ：「方法  Windows フォーム DataGridView コントロールで 
    /// Just-In-Time データ読み込みを使用して仮想モードを実装する」
    /// http://msdn2.microsoft.com/ja-jp/library/ms171625(VS.80).aspx
    /// 
    /// 次に、DataRetriever クラスを定義するコード例を次に示します。
    /// このクラスは、データのページをサーバーから取得する IDataPageRetriever インターフェイスを
    /// 実装します。DataRetriever クラスは、Columns プロパティと RowCount プロパティも提供します。
    /// DataGridView コントロールは、これらのプロパティを使用して、必要な列を作成し、
    /// 適切な数の空の行を Rows コレクションに追加します。
    /// 空の行の追加は、テーブル内のデータがすべて存在するようにコントロールが動作するうえで必要です。
    /// これにより、スクロール バーのスクロール ボックスが適切なサイズになり、ユーザーがテーブル内の
    /// 任意の行にアクセスできるようになります。行は、スクロール表示されたときにのみ、
    /// CellValueNeeded イベント ハンドラによって値が設定されます。 
    /// </summary>
    public class LexiconDataRetriever : IDataPageRetriever
    {
        public LexiconDataRetriever()
        {
        }

        public int RowCount
        {
            get
            {
                return 0;
            }
        }

        private DataColumnCollection columnsValue = null;

        public DataColumnCollection Columns
        {
            get
            {
                // Return the existing value if it has already been determined.
                if (columnsValue != null)
                {
                    return columnsValue;
                }
                DataTable table = new DataTable();
                DataColumn col;
                col = new DataColumn("System.StartDate");
                table.Columns.Add(col);
                col = new DataColumn("System.ItemPathDisplay");
                table.Columns.Add(col);
                col = new DataColumn("System.Size");
                table.Columns.Add(col);
                return table.Columns;
            }
        }

        private string commaSeparatedListOfColumnNamesValue = "System.ItemPathDisplay,System.Size";

        private string CommaSeparatedListOfColumnNames
        {
            get
            {
                return commaSeparatedListOfColumnNamesValue;
            }
        }

        public DataTable SupplyPageOfData(int lowerPageBoundary, int rowsPerPage)
        {
            DataTable dt = new DataTable();
            return dt;
        }
    }
}
