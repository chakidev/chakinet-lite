using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace TextFormatter
{
    public partial class Form1 : Form
    {

        // constructor
        public Form1()
        {
            InitializeComponent();
            openFileDialog1.Filter = "全てのファイル(*)|*";
            saveFileDialog1.Filter = ".mecab ファイル(*.mecab)|*.mecab";
            /*
            foreach( string enc in encodinglist ){
                comboBox1.Items.Add(enc);
                comboBox2.Items.Add(enc);
            }
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
              */
        }

        string[] encodinglist = {"shift-jis", "EUC-JP", "utf-8", "utf-16"};

        // functions
        /*
        Encoding encname2Obj(string charset_name)
        {
            switch (charset_name)
            {
                case "Shift_JIS":
                    return Encoding.GetEncoding("shift-jis");
                case "EUC-JP":
                    return Encoding.GetEncoding("EUC-JP");
                default:
                    MessageBox.Show("Invalid charset name: " + charset_name);
                case "UTF-8":
                    return Encoding.UTF8;
                case "UTF-16":
                    return Encoding.Unicode;
            }
        }*/
        /*
        string loadTextFromFile(string filename)
        {
            // Read
            var reader = new StreamReader(filename);
            string s = reader.ReadToEnd();
            reader.Close();

            //Encoding.UTF8.get

            // Convert LF=>CRLF if needed
            if (s.IndexOf("\r") == -1) s = System.Text.RegularExpressions.Regex.Replace(s, "\n", "\r\n");

            return s;
        }*/

        string splitIntoLines(string s)
        {
            string lineSeparateChars = "。．！？";
            s = System.Text.RegularExpressions.Regex.Replace(s, "([" + lineSeparateChars + "]+)", "$1\n");
            s = System.Text.RegularExpressions.Regex.Replace(s, "[\r\n]([\r\n]+)", "\n"); // multi CR/LF => single LF
            return s;
        }

        void invokeMecab(string srcfile, string destfile)
        {
            var p = new System.Diagnostics.Process();

            p.StartInfo.FileName = "c:\\program files\\mecab\\bin\\mecab.exe";
            p.StartInfo.Arguments = "\"" + srcfile + "\" -o \"" + destfile + "\"";
           
            p.StartInfo.CreateNoWindow = true;
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.Start();
            p.WaitForExit();
            //p.StandardInput.Encoding = Encoding.UTF8;
            /*
            var r = "";
            var sr = new StringReader(s);
            string tmp;

            while ((tmp = sr.ReadLine()) != null)
            {

                byte[] buf = Encoding.UTF8.GetBytes(tmp);
                p.StandardInput.WriteLine(Encoding.GetEncoding("shift-jis").GetString(buf));
                //p.StandardInput.WriteLine(tmp);
                p.StandardInput.Flush();

                string r2;
                while ((r2 = p.StandardOutput.ReadLine()) != "EOS")
                {
                    r += r2;
                    //var bbuf = Encoding.UTF8.GetBytes(r2);
                    //r += Encoding.GetEncoding("shift-jis").GetString(bbuf);
                }
            }
            p.Kill();
            return r;*/
        }

        // event handlers
        private void button_selectOpenFile_Click(object sender, EventArgs e)
        {
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox_infile.Text = openFileDialog1.FileName;
                //if (textBox_outfile.Text == "")
                //{
                    textBox_outfile.Text = openFileDialog1.FileName + ".mecab";
                //}
            }
        }

        private void button_convert_Click(object sender, EventArgs e)
        {
            string filename = textBox_infile.Text;

            // check existence of infile and outfile
            if (File.Exists(filename) == false)
            {
                MessageBox.Show("変換元ファイル " + filename + " は存在しません。ファイル名を確認してください。","変換元ファイルのエラー");
                return;
            }

            if (File.Exists(textBox_outfile.Text))
            {
                var btn = MessageBox.Show("変換結果格納ファイル " + textBox_outfile.Text + " はすでに存在します。上書きしてよろしいですか？",
                    "上書き確認", MessageBoxButtons.YesNo);
                if (btn == DialogResult.No) return;
            }

            // preprocessing
            string inenc = detectEncoding(filename);
            string s = File.ReadAllText(filename,Encoding.GetEncoding(inenc));
            s = splitIntoLines(s);
            
            string tmpfile1 = System.IO.Path.GetTempFileName();
            string tmpfile2 = System.IO.Path.GetTempFileName();
            var writer = new StreamWriter(tmpfile1, true, Encoding.UTF8);
            writer.Write(s);
            writer.Close();

            invokeMecab(tmpfile1, tmpfile2);

            // postprocessing (do nothing)
            s = File.ReadAllText(tmpfile2);
            writer = new StreamWriter(textBox_outfile.Text, true, Encoding.UTF8);
            writer.Write(s);
            writer.Close();

            File.Delete(tmpfile1);
            File.Delete(tmpfile2);

            MessageBox.Show("変換が完了しました。\n文字コードの変換(" + inenc + " => utf-8)、改行処理、MeCab処理" + "を行いました。\n\n" +
                "========== 出力結果プレビュー ==========\n" + s.Substring(0,500),"変換完了");
        }

        private void button_selectSaveFile_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox_outfile.Text = saveFileDialog1.FileName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (textBox_infile.Text == "") return;
            string enc = detectEncoding(textBox_infile.Text);
            /*
            textBox3.AppendText("===== 文字コード: " + enc + " =====\r\n");
            textBox3.AppendText(s);
            textBox3.AppendText("\r\n");
            //for( int i = 0; i < 3; i++ ) textBox3.AppendText(reader.ReadLine() + "\n");
             */

            //textBox3.AppendText(enc+"=============\r\n");
            MessageBox.Show(enc);
        }

        private string detectEncoding(string filename) {
            var readsz = 500;
            var buf = new char[readsz];
            var h_scores = new Dictionary<string, int>();

            foreach( string enc in encodinglist ){
                var encobj = Encoding.GetEncoding(enc);
                var reader = new StreamReader(filename, encobj);
                int nread = reader.ReadBlock(buf, 0, readsz);

                int score = 0;
                for (int i = 0; i < nread; i++)
                {
                    if ('あ' <= buf[i] && buf[i] <= 'ン') score++;
                }

                if (reader.CurrentEncoding != encobj) score+= 200; // encoding object was replaced with auto-detected encoding
                h_scores.Add(enc, score);

                reader.Close();
            }
            return h_scores.Where(p1 => p1.Value == h_scores.Max(p2 => p2.Value)).First().Key;
        }
/*
        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox_infile.Text == "") return;

            var readsz = 2000;
            var buf = new char[readsz];

            var encobj = Encoding.GetEncoding(comboBox1.SelectedItem.ToString());
            var reader = new StreamReader(textBox_infile.Text, encobj);
            reader.ReadBlock(buf, 0, readsz);
            var s = new string(buf);
            reader.Close();
            
            //byte[] bbuf = encobj.GetBytes(buf);
            //string s = Encoding.UTF8.GetString(bbuf);
            
            // conversions


            // display
            //bbuf = Encoding.UTF8.GetBytes(s);
            //s = Encoding.GetEncoding("shift-jis").GetString(bbuf);
            if (s.IndexOf("\r") == -1) s = System.Text.RegularExpressions.Regex.Replace(s, "\n", "\r\n");

            textBox3.AppendText("===== Preview: " + textBox_infile.Text + " =====\r\n");
            textBox3.AppendText(s);
            textBox3.AppendText("\r\n");

            textBox3.AppendText("=============\r\n");        
        }*/
    }
}
