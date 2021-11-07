using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChaKi
{
    public class WindowStates
    {
        public Point WindowLocation { get; set; }

        public Size WindowSize { get; set; }

        public bool IsSplitter1Expanded { get; set; }

        public int SplitterPos2 { get; set; }

        public int SplitterPos3 { get; set; }

        public static WindowStates Instance { get; private set; }

        static WindowStates()
        {
            Instance = new WindowStates();
        }

        private WindowStates()
        {
            // Defaults
            this.IsSplitter1Expanded = true;
            this.SplitterPos2 = 200;
            this.SplitterPos3 = 300;
            this.WindowLocation = new Point(0, 0);
            this.WindowSize = new Size(800, 800);
        }

        /// <summary>
        /// 設定をXMLファイルから読み込む
        /// </summary>
        /// <param name="file"></param>
        public static void Load(string file)
        {
            using (var rd = new StreamReader(file))
            {
                var ser = new XmlSerializer(typeof(WindowStates));
                Instance = (WindowStates)ser.Deserialize(rd);
            }
        }

        /// <summary>
        /// 設定をXMLファイルへ書き出す
        /// </summary>
        /// <param name="file"></param>
        public static void Save(string file)
        {
            using (var wr = new StreamWriter(file))
            {
                var ser = new XmlSerializer(typeof(WindowStates));
                ser.Serialize(wr, Instance);
            }
        }
    }
}
