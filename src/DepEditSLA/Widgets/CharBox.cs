using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Common.Settings;

namespace DependencyEditSLA.Widgets
{
    public partial class CharBox : UserControl
    {
        private static Font ms_Font;

        public CharBox(char ch)
        {
            InitializeComponent();

            this.label1.Text = new string(ch, 1);

            if (ms_Font == null)
            {
                CreateFont();
            }
            this.label1.Font = ms_Font;
            this.Width = this.label1.Width;
            this.Height = this.label1.Height;
        }

        public static float FontSize
        {
            get
            {
                return DepEditSettings.Current.FontSize;
            }
            set
            {
                if (ms_Font != null)
                {
                    ms_Font.Dispose();
                    DepEditSettings.Current.FontSize = value;
                    CreateFont();
                }
            }
        }

        private static void CreateFont()
        {
            float size = DepEditSettings.Current.FontSize;
            Font face = FontDictionary.Current["BaseText"];
            ms_Font = new Font(face.Name, size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        }
    }
}
