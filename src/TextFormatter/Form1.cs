using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

namespace TextFormatter
{
    enum SrcDestType{
        NotSpecified,
        SingleFile,
        Folder
    }

    public partial class Form1 : Form
    {
        string mecabPath = "";
        string cabochaPath = "";
        string unidicPath = "";
        string mecabEncoding="";
        string cabochaUnidicPath = "";

        Thread thread;

        ProcessStartInfo processStartInfo = new ProcessStartInfo();

        const string UNIDICCABOCHA_MODELDEP = "modeldep";
        const string UNIDICCABOCHA_MODELCHUNK = "modelchunk";

        SrcDestType srctype=SrcDestType.NotSpecified, desttype=SrcDestType.NotSpecified;

        // constructor
        public Form1()
        {
            InitializeComponent();
        }

        string[] encodinglist = {"shift-jis", "EUC-JP", "utf-8", "utf-16"};

        Encoding get_encoding(string encname) {
            if (encname == "utf-8") return new UTF8Encoding(); // UTF-8N  //Encoding.GetEncoding(65001);
            else return Encoding.GetEncoding(encname);
        }

        // functions
        private void throw_error(string msg)
        {
            MessageBox.Show(msg);
            Application.Exit();
        }

        string splitIntoLines(string s)
        {
            string lineSeparateChars = "。．！？」";
            s = System.Text.RegularExpressions.Regex.Replace(s, "\r?\n([^\r\n])", "$1"); // remove single (CR)LFs
            //MessageBox.Show(s.Substring(0,100));
            s = System.Text.RegularExpressions.Regex.Replace(s, "([" + lineSeparateChars + "]+)", "$1\n");
            s = System.Text.RegularExpressions.Regex.Replace(s, "[\r\n]([\r\n]+)", "\n"); // multiple CR/LF => single LF
            s = Microsoft.VisualBasic.Strings.StrConv(s, Microsoft.VisualBasic.VbStrConv.Wide, 0);
            return s;
        }

        int[] unidicColumns = { 1, 2, 3, 4, 5, 6, 13, 11, 10, 7, 8 };

        void changeEncoding(string infile, string outfile, Encoding inenc, Encoding outenc)
        {
            var sr = new StreamReader(infile, inenc);
            var sw = new StreamWriter(outfile, false, outenc);

            string l;
            while ((l = sr.ReadLine()) != null )
            {
                sw.WriteLine(l);
            }
            sr.Close();
            sw.Close();
            return;
        }

        void invokeMecab(string srcfile, string destfile)
        {
            processStartInfo.FileName = mecabPath;//"c:\\program files\\mecab\\bin\\mecab.exe";
            processStartInfo.Arguments = "\"" + srcfile + "\" -o \"" + destfile + "\"" + (radioButton_ipadic.Checked ? "" : (" -d \"" + unidicPath + "\""));
           
            Process.Start(processStartInfo).WaitForExit();
        }

        void invokeCaboCha(string srcfile, string destfile)
        {
            var p = new System.Diagnostics.Process();

            processStartInfo.FileName = cabochaPath;
            processStartInfo.Arguments = "-I1 -O4 -f1 -n0";
            if (radioButton_Unidic.Checked) processStartInfo.Arguments += " -P IPA -t SHIFT_JIS -m \"" + cabochaUnidicPath + "\\" + UNIDICCABOCHA_MODELDEP + "\" -M \"" + cabochaUnidicPath + "\\" + UNIDICCABOCHA_MODELCHUNK + "\"";
            processStartInfo.Arguments+= " \"" + srcfile + "\" -o \"" + destfile + "\"";

            Process.Start(processStartInfo).WaitForExit();
        }

        string getMecabPath(string name)
        {
            return getSoftwarePath("mecab");
        }

        string getSoftwarePath(string name)
        {
            RegistryKey rkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\"+name);
            if (rkey == null) rkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + name);
            if (rkey != null)
            {
                string mecabrc = (string)rkey.GetValue(name+"rc");
                string exename = mecabrc.Replace("etc\\"+name+"rc", "bin\\"+name+".exe");
                if (File.Exists(exename)) return exename;
            }

            string defaultPath = "c:\\Program Files\\"+name+"\\bin\\"+name+".exe";
            if (File.Exists(defaultPath)) return defaultPath;
            defaultPath = "c:\\Program Files (x86)\\" + name + "\\bin\\" + name + ".exe";
            if (File.Exists(defaultPath)) return defaultPath;

            return ""; // not found
        }

        string getCaboChaUniDicPath()
        {
            string path = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            if (File.Exists(path + "\\" + UNIDICCABOCHA_MODELDEP) && File.Exists(path + "\\" + UNIDICCABOCHA_MODELCHUNK)) return path;
            return "";
        }

        string getUnidicPath()
        {
            RegistryKey rkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\unidic_win");
            if (rkey == null) rkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\unidic_win");
            if (rkey != null)
            {
                string unidicloc = (string)rkey.GetValue("InstallLocation");
                string dicpath = unidicloc + "\\dic\\unidic-mecab";
                string rcpath = dicpath + "\\dicrc";
                if (File.Exists(rcpath)) return dicpath;
            }

            string defaultPath = "c:\\Program Files\\unidic\\dic\\unidic-mecab";
            string defrcpath = defaultPath + "\\dicrc";
            if (File.Exists(defrcpath)) return defaultPath;

            defaultPath = "c:\\Program Files (x86)\\unidic\\dic\\unidic-mecab";
            defrcpath = defaultPath + "\\dicrc";
            if (File.Exists(defrcpath)) return defaultPath;

            return ""; // not found
        }

        string detectMecabEncoding()
        {
            string srcfile  = System.IO.Path.GetTempFileName();
            string destfile = System.IO.Path.GetTempFileName();
            string teststr  = "これは辞書の文字コードを判定するためのテスト文です。\r\n";
            string result = "";
            int maxscore = 0;

            foreach (string enc in encodinglist)
            {
                var encobj = get_encoding(enc);
                var writer = new StreamWriter(srcfile, false, encobj);
                writer.Write(teststr);
                writer.Close();

                processStartInfo.FileName = mecabPath;//"c:\\program files\\mecab\\bin\\mecab.exe";
                processStartInfo.Arguments = "\"" + srcfile + "\" -o \"" + destfile + "\"" + (radioButton_ipadic.Checked ? "" : (" -d \"" + unidicPath + "\""));

                Process prc = Process.Start(processStartInfo);
                prc.WaitForExit();

                var reader = new StreamReader(destfile, encobj);
                string readstr = reader.ReadToEnd();
                reader.Close();

                int score = 0;
                for (int i = 0; i < readstr.Length; i++)
                {
                    if (readstr[i] == '詞') score++;
                }
                if (score > maxscore)
                {
                    maxscore = score;
                    result = enc;
                }

            }
            File.Delete(srcfile);
            File.Delete(destfile);
            return result;
        }
           

        private void setInfile(string infilename, SrcDestType type)
        {
            textBox_infile.Text = infilename;
            switch (type)
            {
                case SrcDestType.SingleFile:
                    textBox_outfile.Text = Path.ChangeExtension(textBox_infile.Text, checkBox_doDepParse.Checked ? ".cabocha" : ".mecab");
                    srctype = SrcDestType.SingleFile;
                    desttype = SrcDestType.SingleFile;
                    break;

                case SrcDestType.Folder:
                    textBox_outfile.Text = textBox_infile.Text;
                    srctype = SrcDestType.Folder;
                    desttype = SrcDestType.Folder;
                    break;

                default:
                    MessageBox.Show("Internal Error: Invalid SrcType: " + type);
                    break;
            }
            setConversionMode();
            //textBox_outFileCaboCha.Text = Path.ChangeExtension(textBox_infile.Text, ".cabocha");
        }

        private void setConversionMode() {
            button_selectOpenFile.Enabled = true;
            button_selectOpenFolder.Enabled = true;
            radioButton_ipadic.Enabled = true;
            radioButton_Unidic.Enabled = (unidicPath != "");
            checkBox_doSeparateLine.Enabled = true;
            
            button_convert.Enabled = (srctype != SrcDestType.NotSpecified && desttype != SrcDestType.NotSpecified);
            button_selectSaveFolder.Enabled = (srctype == SrcDestType.Folder);
            button_selectSaveFile.Enabled = (srctype == SrcDestType.SingleFile); //(srctype != SrcDestType.NotSpecified);
            /*
            Font fnt = button_selectOpenFolder
            button_selectOpenFile.Font = (srctype == SrcDestType.SingleFile);
            button_selectOpenFolder.Font.Bold = (srctype == SrcDestType.Folder);
            button_selectSaveFile.Font.Bold = (desttype == SrcDestType.SingleFile);
            button_selectSaveFolder.Font.Bold = (desttype == SrcDestType.Folder);
            */
            if (cabochaPath != "" && (radioButton_ipadic.Checked || cabochaUnidicPath != "" )) checkBox_doDepParse.Enabled = true;
            else {
                checkBox_doDepParse.Enabled = false;
                checkBox_doDepParse.Checked = false;
            }

            saveFileDialog1.FilterIndex = (checkBox_doDepParse.Checked) ? 2 : 1;
        }

        private void enumerateFiles(List<string> list, string dir)
        {
            foreach (string file in System.IO.Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(file);
                if (ext == ".txt" /*ext != ".mecab" && ext != ".cabocha" && ext != ".db" && ext != ".bib"*/) list.Add(file);
            }
            foreach (string subdir in System.IO.Directory.GetDirectories(dir))
            {
                enumerateFiles(list, subdir);
            }
        }

        private string fullToRelativePath(string filename, string basedir)
        {
            return System.Web.HttpUtility.UrlDecode(new Uri(basedir).MakeRelativeUri(new Uri(filename)).ToString()).Replace("/", "\\");
        }

        private string relativeToFullPath(string filename, string basedir)
        {
            return new Uri(new Uri(basedir), filename).LocalPath;
        }

        private string makeSureEndsWithSlash(string dir)
        {
            if (dir.EndsWith("\\")) return dir;
            else return dir += "\\";
        }

        List<string> _srcfiles, _relpaths;
        string _doneMessage;
        private bool convert()
        {

            string srcfilename = textBox_infile.Text;
            _srcfiles = new List<string>();
            _relpaths = new List<string>();

            // check existence of infile and outfile
            if (srctype == SrcDestType.SingleFile)
            {
                if (File.Exists(srcfilename) == false)
                {
                    MessageBox.Show("変換元ファイル " + srcfilename + " は存在しません。ファイル名を確認してください。", "変換元ファイルのエラー");
                    return false;
                }

                if (textBox_infile.Text == textBox_outfile.Text /*|| textBox_infile.Text == textBox_outFileCaboCha.Text*/)
                {
                    MessageBox.Show("変換元ファイルと変換結果格納ファイルが同じです。ファイル名を確認してください。", "ファイル名エラー");
                    return false;
                }

                if (File.Exists(textBox_outfile.Text))
                {
                    var btn = MessageBox.Show("変換結果格納ファイル " + textBox_outfile.Text + " はすでに存在します。上書きしてよろしいですか？",
                        "上書き確認", MessageBoxButtons.YesNo);
                    if (btn == DialogResult.No) return false;
                }

                string ext = Path.GetExtension(textBox_outfile.Text);
                if (checkBox_doDepParse.Checked && ext != ".cabocha")
                {
                    if (MessageBox.Show("変換結果はCaboChaフォーマットとなりますが、変換結果格納ファイルの拡張子は .cabocha ではなく " + ext + " となっています。変換を続行してもよろしいですか？",
                            "変換結果格納ファイルの拡張子についての警告", MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
                }
                if (!checkBox_doDepParse.Checked && ext != ".mecab")
                {
                    if (MessageBox.Show("変換結果はMeCabフォーマットとなりますが、変換結果格納ファイルの拡張子は .mecab ではなく " + ext + " となっています。変換を続行してもよろしいですか？",
                            "変換結果格納ファイルの拡張子についての警告", MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
                }
                _srcfiles.Add(srcfilename);
            }
            else if (srctype == SrcDestType.Folder)
            {
                string srcdir  = makeSureEndsWithSlash(textBox_infile.Text);
                string destdir = makeSureEndsWithSlash(textBox_outfile.Text);
                if (Directory.Exists(srcdir) == false)
                {
                    MessageBox.Show("変換元フォルダ " + srcdir + " は存在しません。フォルダ名を確認してください。", "変換元フォルダのエラー");
                    return false;
                }
                enumerateFiles(_srcfiles, srcdir);

                if (_srcfiles.Count == 0)
                {
                    MessageBox.Show("変換元フォルダ " + srcdir + " には変換対象ファイルがありません。", "変換元フォルダ指定エラー");
                    return false;
                }

                // check dest folder whether files such that will be overwritten exists
                // get relative paths of files


                foreach (string fn in _srcfiles)
                {
                    string rp = fullToRelativePath(fn, srcdir);
                    rp = Path.ChangeExtension(rp, checkBox_doDepParse.Checked ? ".cabocha" : ".mecab");
                    _relpaths.Add(rp);
                }
                // check each file exists at dest dir
                var existsfiles = new List<string>();
                foreach (string fn in _relpaths)
                {
                    string fp = relativeToFullPath(fn, destdir);
                    if (File.Exists(fp)) existsfiles.Add(fn);
                }
                if (existsfiles.Count != 0)
                {
                    string cmsg = "変換結果格納フォルダにある " + existsfiles.Count + " 個のファイルが上書きされます。変換を続行してもよろしいですか？\n\n";
                    cmsg+= "--------------- 上書きされるファイル --------------\n";
                    for (int i = 0; i < 10 && i < existsfiles.Count; i++) cmsg += destdir + existsfiles[i] + "\n";
                    if (existsfiles.Count > 10) cmsg += "...";
                    if (MessageBox.Show(cmsg, "上書き確認", MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
                }
            }
            else throw_error("Internal Error: srctype invalid:" + srctype);

            //if (mecabEncoding == "")
            //{
                mecabEncoding = detectMecabEncoding();

                if (mecabEncoding == "")
                {
                    MessageBox.Show("MeCabの文字コードを認識できませんでした。");
                    Application.Exit();
                }
            //}
            _doneMessage = "変換が完了しました。\n" +//変換元ファイルの文字コード: " + inenc + "\n" +
                (checkBox_doSeparateLine.Checked ? "改行処理、" : "")+ "MeCab処理(辞書文字コード:" + mecabEncoding + ")";

            if (checkBox_doDepParse.Checked) _doneMessage += "、CaboCha処理を行いました。";
            else _doneMessage+= "を行いました。";
            //msg += "を行いました。";
            _doneMessage += "\n\n";

            thread = new Thread(new ThreadStart(conversionThread));
            thread.IsBackground = true;
            thread.Start();
            return true;
        }

        string _threadState = "";
        private void conversionThread(){
            
            _threadState =  "変換中...";
            string tmpfile1 = System.IO.Path.GetTempFileName();
            string tmpfile2 = System.IO.Path.GetTempFileName();
            string inenc="", s="";


            for (int i_file = 0; i_file < _srcfiles.Count; i_file++)
            {
                string fn = _srcfiles[i_file];
                string outfilename;
                if (desttype == SrcDestType.SingleFile) outfilename = textBox_outfile.Text;
                else outfilename = relativeToFullPath(_relpaths[i_file], makeSureEndsWithSlash(textBox_outfile.Text));

                if (Directory.Exists(Path.GetDirectoryName(outfilename)) == false)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outfilename));
                }

                // preprocessing
                inenc = detectEncoding(fn);
                s = File.ReadAllText(fn, get_encoding(inenc));

                if (checkBox_doSeparateLine.Checked)
                {
                    _threadState = "改行処理中... (ファイル " + (i_file + 1) + "/" + (_srcfiles.Count) + ")";
                    s = splitIntoLines(s);
                }

                var writer = new StreamWriter(tmpfile1, false, get_encoding(mecabEncoding));
                writer.Write(s);
                writer.Close();

                _threadState = "MeCab処理中... (ファイル " + (i_file + 1) + "/" + (_srcfiles.Count) + ")";
                invokeMecab(tmpfile1, tmpfile2);
                string finalresult = tmpfile2;

                //s = File.ReadAllText(finalresult, Encoding.GetEncoding(mecabEncoding));
                if (radioButton_Unidic.Checked)
                {
                    string outenc = mecabEncoding;
                    if (checkBox_doDepParse.Checked)
                    {
                        changeEncoding(tmpfile2, tmpfile1, get_encoding(mecabEncoding), get_encoding("shift-jis"));
                        _threadState = "CaboCha処理中... (ファイル " + (i_file + 1) + "/" + (_srcfiles.Count) + ")";
                        invokeCaboCha(tmpfile1, tmpfile2);
                        outenc = "shift-jis";
                        finalresult = tmpfile2;
                    }
                    changeEncoding(tmpfile2, outfilename, get_encoding(outenc), Encoding.UTF8);
                    if(i_file == _srcfiles.Count-1) s = File.ReadAllText(outfilename, Encoding.UTF8);
                }
                else // IPADIC
                {
                    if (checkBox_doDepParse.Checked)
                    {
                        _threadState = "CaboCha処理中... (ファイル " + (i_file + 1) + "/" + (_srcfiles.Count) + ")";
                        invokeCaboCha(tmpfile2, tmpfile1);
                        finalresult = tmpfile1;
                    }

                    // postprocessing (do nothing)
                    s = File.ReadAllText(finalresult, get_encoding(mecabEncoding));
                    writer = new StreamWriter(outfilename, false, Encoding.UTF8);
                    writer.Write(s);
                    writer.Close();
                }
            }
            File.Delete(tmpfile1);
            File.Delete(tmpfile2);
            _doneMessage += "==== 出力結果プレビュー (出力文字コード: utf-8) ====\n" + (s.Length < 500 ? s : s.Substring(0, 500));
            _threadState = "Done";
        }

        void printDoneMessage(){
            MessageBox.Show(_doneMessage, "変換完了");
            _doneMessage = "";
        }


        // event handlers
        private void button_selectOpenFile_Click(object sender, EventArgs e)
        {
            if (srctype == SrcDestType.SingleFile) openFileDialog1.InitialDirectory = Path.GetDirectoryName(textBox_infile.Text);
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                setInfile(openFileDialog1.FileName, SrcDestType.SingleFile);
            }
        }

        private void button_selectOpenFolder_Click(object sender, EventArgs e)
        {
            if (srctype == SrcDestType.Folder) folderBrowserDialog1.SelectedPath = textBox_infile.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                setInfile(folderBrowserDialog1.SelectedPath, SrcDestType.Folder);
            }
        }

        private void button_convert_Click(object sender, EventArgs e)
        {
            //button_convert.Text = "変換中...";
            button_convert.Enabled = false;
            button_selectOpenFile.Enabled = button_selectOpenFolder.Enabled = button_selectSaveFile.Enabled = button_selectSaveFolder.Enabled = false;
            radioButton_ipadic.Enabled = radioButton_Unidic.Enabled = false;
            checkBox_doDepParse.Enabled = checkBox_doSeparateLine.Enabled = false;
            if (!convert())
            {
                _threadState = "";
                button_convert.Enabled = true;
                setConversionMode();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            switch (_threadState)
            {
                case "Done":
                    _threadState = "";
                    printDoneMessage();
                    //button_convert.Text = "変換";
                    button_convert.Enabled = true;
                    setConversionMode();
                    break;

                case "":
                    button_convert.Text = "変換";
                    break;
                    
                default:
                    button_convert.Text = _threadState;
                    break;
            }
        }
        
        private void button_selectSaveFile_Click(object sender, EventArgs e)
        {
            if (desttype == SrcDestType.SingleFile) saveFileDialog1.InitialDirectory = Path.GetDirectoryName(textBox_outfile.Text);
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox_outfile.Text = saveFileDialog1.FileName;
                desttype = SrcDestType.SingleFile;
                setConversionMode();
            }
        }

        private void button_selectSaveFolder_Click(object sender, EventArgs e)
        {
            if (desttype == SrcDestType.Folder) folderBrowserDialog2.SelectedPath = textBox_outfile.Text;
            if (folderBrowserDialog2.ShowDialog() == DialogResult.OK)
            {
                textBox_outfile.Text = folderBrowserDialog2.SelectedPath;
                desttype = SrcDestType.Folder;
                setConversionMode();
            }
        }

        private string detectEncoding(string filename) {
            var readsz = 500;
            var buf = new char[readsz];
            var h_scores = new Dictionary<string, int>();

            foreach( string enc in encodinglist ){
                var encobj = get_encoding(enc);
                var reader = new StreamReader(filename, encobj);
                int nread = reader.ReadBlock(buf, 0, readsz);

                int score = 0;
                for (int i = 0; i < nread; i++)
                {
                    if ('あ' <= buf[i] && buf[i] <= 'ン') score++;
                }

                if (reader.CurrentEncoding != encobj) score-= 100; // encoding object was replaced with auto-detected encoding
                h_scores.Add(enc, score);

                reader.Close();
            }

            string bestenc = "";
            foreach (KeyValuePair<string, int> kvp in h_scores) {
                if (bestenc == "" || h_scores[bestenc] < kvp.Value) bestenc = kvp.Key;
            }
            return bestenc;
            //return h_scores.Where(p1 => p1.Value == h_scores.Max(p2 => p2.Value)).First().Key;
        }

        private SrcDestType fileOrFolderExists(string name)
        {
            if (System.IO.File.Exists(name)) return SrcDestType.SingleFile;
            else if (System.IO.Directory.Exists(name)) return SrcDestType.Folder;
            return SrcDestType.NotSpecified;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            saveFileDialog1.FileName = "";
            //saveFileDialog2.FileName = "";
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;

            mecabPath = getSoftwarePath("mecab");
            unidicPath = getUnidicPath();
            cabochaPath = getSoftwarePath("cabocha");
            cabochaUnidicPath = getCaboChaUniDicPath();
            if (mecabPath == "")
            {
                MessageBox.Show("MeCabのインストール先を認識できませんでした。");
                Application.Exit();
            }

            // We do not report warning of the validity of cabochaPath here, just we cannot select the checkbox for dependency parsing.

            if (unidicPath == "")
            {
                MessageBox.Show("UniDicのインストール先を認識できませんでした。");
                radioButton_Unidic.Enabled = false;
                radioButton_ipadic.Checked = true;
                //Application.Exit();
            }

            if (Program.args.Length != 0)
            {
                SrcDestType type = fileOrFolderExists(Program.args[0]);
                if (type != SrcDestType.NotSpecified) setInfile(Program.args[0], type);
                else MessageBox.Show("ファイルまたはフォルダ " + Program.args[0] + " は存在しません。");
            }
            setConversionMode();
        }

        private void textBox_infile_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {

                // ドラッグ中のファイルやディレクトリの取得
                string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (drags.Length != 1) return; // should be single file or folder

                foreach (string d in drags)
                {
                    if (fileOrFolderExists(d) == SrcDestType.NotSpecified)
                    {
                        // do nothing
                        return;
                    }
                }
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void textBox_infile_DragDrop(object sender, DragEventArgs e)
        {
            string name = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            setInfile(name, fileOrFolderExists(name));
        }

        private void radioButton_Unidic_CheckedChanged(object sender, EventArgs e)
        {
            setConversionMode();
        }

        private void checkBox_doDepParse_CheckedChanged(object sender, EventArgs e)
        {
            string inext = Path.GetExtension(textBox_outfile.Text);
            if ( checkBox_doDepParse.Checked && desttype == SrcDestType.SingleFile && inext == ".mecab") textBox_outfile.Text = Path.ChangeExtension(textBox_outfile.Text, ".cabocha");
            if (!checkBox_doDepParse.Checked && desttype == SrcDestType.SingleFile && inext == ".cabocha") textBox_outfile.Text = Path.ChangeExtension(textBox_outfile.Text, ".mecab");
            setConversionMode();
        }



    }
}
