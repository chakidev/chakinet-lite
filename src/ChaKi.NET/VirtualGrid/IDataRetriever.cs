using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace ChaKi.VirtualGrid
{
    /// <summary>
    /// 参考ページ：「方法  Windows フォーム DataGridView コントロールで 
    /// Just-In-Time データ読み込みを使用して仮想モードを実装する」
    /// http://msdn2.microsoft.com/ja-jp/library/ms171625(VS.80).aspx
    /// 
    /// DataRetriever クラスによって実装される IDataPageRetriever インターフェイスを定義する
    /// コード例を次に示します。このインターフェイスで宣言されているメソッドは、
    /// 初期行インデックスと 1 ページのデータの行数を要求する SupplyPageOfData メソッドだけです。
    /// これらの値は、データ ソースからデータのサブセットを取得する実装側で使用されます。
    /// 
    /// Cache オブジェクトは、このインターフェイスの実装を構築時に使用して、
    /// データの最初の 2 ページを読み込みます。未キャッシュのデータが必要になると、
    /// キャッシュはこれらのページのいずれかを破棄し、IDataPageRetriever にその値を含む新しいページを要求します。
    /// </summary>
    public interface IDataPageRetriever
    {
        DataTable SupplyPageOfData(int lowerPageBoundary, int rowsPerPage);
    }
}
