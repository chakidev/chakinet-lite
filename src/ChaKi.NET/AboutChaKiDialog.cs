using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ChaKi
{
    partial class AboutChaKiDialog : Form
    {
        public AboutChaKiDialog()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyTitle);
            this.BuildNumber.Text = "Revision: " + AssemblyBuild;

            var regex = new Regex(@"(\d+)\.(\d+)\.\d+\.\d+");
            var version = regex.Match(AboutChaKiDialog.AssemblyVersion);
            if (version.Groups.Count > 1)
            {
                var ver1 = Int32.Parse(version.Groups[1].Value);
                var ver2 = Int32.Parse(version.Groups[2].Value);
                this.label1.Text = string.Format("ChaKi.NET Version {0}.{1:00}", ver1, ver2);
            }


#if false
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0} {0}", AssemblyVersion);
            this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
            this.textBoxDescription.Text = AssemblyDescription;
#endif
        }

        #region アセンブリ属性アクセサ

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public static string AssemblyBuild
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.Build.ToString();
            }
        }

        public static string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public static string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public static string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public static string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void AboutChaKiDialog_Paint(object sender, PaintEventArgs e)
        {
            Rectangle r = this.Bounds;
            r = this.RectangleToClient(r);
            using (Brush br = new LinearGradientBrush(r, Color.Beige, Color.Silver, 0, false))
            {
                e.Graphics.FillRectangle(br, r);
            }
        }

    }
}
