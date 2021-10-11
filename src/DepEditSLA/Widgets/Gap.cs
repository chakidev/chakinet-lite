using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using ChaKi.Common;

namespace DependencyEditSLA.Widgets
{
    public partial class Gap : UserControl
    {
        [DllImport("user32.dll")]
        static extern IntPtr LoadCursorFromFile(string lpFileName);

        static private Cursor ms_ScissorCursor;

        public int GapPos { get; set; }

        static Gap()
        {
            try
            {
                // フルカラー・ホットスポット付のカーソルは、Cursorコンストラクタでは作成できないので、
                // 埋め込みリソースからTempファイルに書き出し、そのファイルをLoadCursorFromFileで読み込み、
                // Cursorにアタッチする。
                string tempFile = Path.GetTempPath() + "Scissor24.cur";
                Stream st = null;
                FileStream fs = null;
                try
                {
                    // Scissor.curはResources内に存在し、ビルドアクション＝「埋め込みリソース」としてある
                    st = Assembly.GetExecutingAssembly().GetManifestResourceStream("DependencyEditSLA.Properties.Scissor24.cur");
                    fs = new FileStream(tempFile, FileMode.Create);
                    while (st.CanRead)
                    {
                        int b = st.ReadByte();
                        if (b < 0)
                        {
                            break;
                        }
                        fs.WriteByte((byte)b);
                    }
                }
                finally
                {
                    if (st != null) st.Close();
                    if (fs != null) fs.Close();
                }
                    
                IntPtr pCur = LoadCursorFromFile(tempFile);
                ms_ScissorCursor = new Cursor(pCur);
            }
            catch (Exception)
            {
                try
                {
                    // この方法だと白黒になってしまう。
                    ms_ScissorCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("DependencyEdit.Properties.Scissor.cur"));
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }
        }

        public Gap( int gapPos )
        {
            InitializeComponent();

            this.Cursor = ms_ScissorCursor;
            this.GapPos = gapPos;
        }
    }
}
