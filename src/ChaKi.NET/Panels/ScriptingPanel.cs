using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ChaKi.GUICommon;
using System.Threading;

using System.Reflection;
using ChaKi.ToolDialogs;

namespace ChaKi.Panels
{
    public partial class ScriptingPanel : Form
    {
        public enum ScriptingEngineTypes
        {
            None = 0,
            IronPython = 1,
            IronRuby = 2,
            JScript = 3,
            CSharp = 4,
        }
        private AppDomain m_AppDomain;
        private Assembly m_MsScriptingAssembly;
        private Assembly m_IronRubyAssembly;
        private Assembly m_IronPythonAssembly;

        private object m_Engine;
        private MethodInfo m_ScriptSource;
        private MethodInfo m_Executor;

        private Stream m_OutStream;
        private Thread m_ExecutingThread;
        private Thread m_BackgroundPrinter;
        private AutoResetEvent m_CancelBackgroundEvent;

        public ScriptingEngineTypes EngineType
        {
            get { return m_EngineType; }
            set
            {
                m_EngineType = value;
                this.toolStripComboBox1.SelectedIndex = (int)value;
            }
        } private ScriptingEngineTypes m_EngineType;

        public ScriptingPanel()
        {
            InitializeComponent();

            this.EngineType = ScriptingEngineTypes.None;
        }

        private bool LoadEngine()
        {
            m_AppDomain = AppDomain.CurrentDomain;//AppDomain.CreateDomain("DomainForScripting");

            Type ty;
            MethodInfo mi;
            PropertyInfo pi;

            try
            {
                if (m_MsScriptingAssembly == null)
                {
                    m_MsScriptingAssembly = m_AppDomain.Load("Microsoft.Scripting");
                }
                switch (m_EngineType)
                {
                    case ScriptingEngineTypes.IronPython:
                        if (m_IronPythonAssembly == null)
                        {
                            m_IronPythonAssembly = m_AppDomain.Load("IronPython");
                            ty = m_IronPythonAssembly.GetType("IronPython.Hosting.Python");
                            mi = ty.GetMethod("CreateEngine", Type.EmptyTypes);
                            m_Engine = mi.Invoke(null, null);
                        }
                        break;
                    case ScriptingEngineTypes.IronRuby:
                        if (m_IronRubyAssembly == null)
                        {
                            m_IronRubyAssembly = m_AppDomain.Load("IronRuby");
                            ty = m_IronRubyAssembly.GetType("IronRuby.Ruby");
                            mi = ty.GetMethod("CreateEngine", Type.EmptyTypes);
                            m_Engine = mi.Invoke(null, null);

                        }
                        break;
                    case ScriptingEngineTypes.JScript:
                    case ScriptingEngineTypes.CSharp:
                        throw new NotSupportedException();
                    default:
                        m_Engine = null;
                        return false;
                }

                ty = m_MsScriptingAssembly.GetType("Microsoft.Scripting.Hosting.ScriptEngine");
                m_ScriptSource = ty.GetMethod("CreateScriptSourceFromString",
                    new Type[] {
                        typeof(string),
                        m_MsScriptingAssembly.GetType("Microsoft.Scripting.SourceCodeKind") });

                ty = m_MsScriptingAssembly.GetType("Microsoft.Scripting.Hosting.ScriptSource");
                m_Executor = ty.GetMethod("ExecuteAndWrap", Type.EmptyTypes);
            }
            catch (Exception ex)
            {
                ErrorReportDialog errdlg = new ErrorReportDialog("Error Loading Script Engine:", ex);
                errdlg.ShowDialog();
                m_Engine = null;
                return false;
            }
            return true;
        }

        private void CompileAndRun()
        {
            object source = null;  // ScriptSource
            m_CancelBackgroundEvent = new AutoResetEvent(false);

            if (LoadEngine())
            {
                try
                {
                    if (m_ExecutingThread != null)
                    {
                        if (!m_ExecutingThread.Join(1000))
                        {
                            throw new Exception("Cannot stop executing thread.");
                        }
                    }
                    if (m_BackgroundPrinter != null)
                    {
                        m_CancelBackgroundEvent.Set();
                        if (!m_BackgroundPrinter.Join(5000))
                        {
                            throw new Exception("Cannot stop background worker.");
                        }
                        m_BackgroundPrinter = null;
                    }
                    m_OutStream = new MemoryStream();

                    // m_Engine.Runtime.IO.SetOutput(m_OutStream, Encoding.UTF8);
                    // m_Engine.Runtime.IO.SetErrorOutput(m_OutStream, Encoding.UTF8);
                    Type ty = m_MsScriptingAssembly.GetType("Microsoft.Scripting.Hosting.ScriptEngine");
                    PropertyInfo pi = ty.GetProperty("Runtime");
                    object runtime = pi.GetValue(m_Engine, null);
                    ty = m_MsScriptingAssembly.GetType("Microsoft.Scripting.Hosting.ScriptRuntime");
                    pi = ty.GetProperty("IO");
                    object io = pi.GetValue(runtime, null);
                    ty = m_MsScriptingAssembly.GetType("Microsoft.Scripting.Hosting.ScriptIO");
                    MethodInfo mi = ty.GetMethod("SetOutput", new Type[] { typeof(System.IO.Stream), typeof(System.Text.Encoding) });
                    mi.Invoke(io, new object[] { m_OutStream, Encoding.UTF8 });
                    mi = ty.GetMethod("SetErrorOutput", new Type[] { typeof(System.IO.Stream), typeof(System.Text.Encoding) });
                    mi.Invoke(io, new object[] { m_OutStream, Encoding.UTF8 });

                    // m_Engine.CreateScriptSourceFromString(txt, SourceCodeKind.File);
                    source = m_ScriptSource.Invoke(
                        m_Engine,
                        new object[] { this.richTextBox1.Text, 4/*SourceCodeKind.File*/ });
                    // Start Executing Thread
                    m_ExecutingThread = new Thread(new ParameterizedThreadStart(ExecutingProc)) { IsBackground = true };
                    m_ExecutingThread.Start(source);
                    // Start Output-receiving Thread
                    m_BackgroundPrinter = new Thread(new ThreadStart(BackgroundProc)) { IsBackground = true };
                    m_BackgroundPrinter.Start();
                }
                catch (Exception ex)
                {
                    ErrorReportDialog errdlg = new ErrorReportDialog("Error while starting Script Engine:", ex);
                    errdlg.ShowDialog();
                }
            }
        }

        private void ExecutingProc(object source)
        {
            if (m_Executor == null) return;

            try
            {
                // source.Execute();
                m_Executor.Invoke(source, null);
            }
            catch (Exception ex)
            {
                ErrorReportDialog errdlg = new ErrorReportDialog("Error while executing script:", ex);
                errdlg.ShowDialog();
            }
            lock (m_OutStream)
            {
                m_OutStream.Position = 0;
            }
        }

        private void BackgroundProc()
        {
            Stream str = m_OutStream;
            if (m_OutStream == null)
            {
                return;
            }
            using (StreamReader rdr = new StreamReader(str, Encoding.UTF8))
            {
                while (!m_CancelBackgroundEvent.WaitOne(400, false))
                {
                    string ret;
                    lock (m_OutStream)
                    {
                        if (!m_OutStream.CanRead) break;
                        ret = rdr.ReadToEnd();
                    }
                    this.richTextBox2.BeginInvoke(new Action<string>(delegate(string s)
                        { this.richTextBox2.AppendText(s); }), ret);  // BeginInvokeでないとUIThread (CompileAndRun)との間でdeadlockする.
                }
            }
        }

        public void LoadScriptFile(string path, string scriptType)
        {
            switch (scriptType)
            {
                case ".RB":
                    this.EngineType = ScriptingEngineTypes.IronRuby;
                    break;
                case ".PY":
                    this.EngineType = ScriptingEngineTypes.IronRuby;
                    break;
                default:
                    this.EngineType = ScriptingEngineTypes.None;
                    break;
            }
            using (var rdr = new StreamReader(path))
            {
                this.richTextBox1.Clear();
                this.richTextBox2.Clear();
                var txt = rdr.ReadToEnd();
                this.richTextBox1.Text = txt;
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = this.toolStripComboBox1.SelectedIndex;
            if ((int)this.EngineType != index)
            {
                if (index != 0)
                {
                    try
                    {
                        m_EngineType = (ScriptingEngineTypes)index;
                        return;
                    }
                    catch (Exception ex)
                    {
                        ErrorReportDialog errdlg = new ErrorReportDialog("Error Loading Script Engine:", ex);
                        errdlg.ShowDialog();
                    }
                }
                this.EngineType = ScriptingEngineTypes.None;
                m_Engine = null;
            }
        }

        /// <summary>
        /// Compile and Run the code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            CompileAndRun();
        }

        private void richTextBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.F5
             || (e.KeyCode == Keys.C && e.Control))
            {
                e.IsInputKey = true; // KeyDown Eventを発生させる.
            }
        }

        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                CompileAndRun();
                e.Handled = true; // ShortcutのF5動作を起動させないため.
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                if (m_Engine != null)
                {
                    // m_Engine.Runtime.Shutdown();
                    Type ty = m_MsScriptingAssembly.GetType("Microsoft.Scripting.Hosting.ScriptEngine");
                    PropertyInfo pi = ty.GetProperty("Runtime");
                    object runtime = pi.GetValue(m_Engine, null);
                    ty = m_MsScriptingAssembly.GetType("Microsoft.Scripting.Hosting.ScriptRuntime");
                    MethodInfo mi = ty.GetMethod("Shutdown", Type.EmptyTypes);
                    mi.Invoke(runtime, null);
                }
            }
        }

        // Clear Output
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            this.richTextBox2.Clear();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            var dlg = new LoadPresetScriptsDialog();
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Selection != null)
            {
                Console.WriteLine(dlg.Selection, dlg.SelectionType);
                try {
                    LoadScriptFile(dlg.Selection, dlg.SelectionType);
                }
                catch (Exception ex)
                {
                    var errdlg = new ErrorReportDialog("Error Loading script file:", ex);
                    errdlg.ShowDialog();
                }
            }
        }
    }
}
