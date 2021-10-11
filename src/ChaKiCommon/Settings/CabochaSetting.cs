using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace ChaKi.Common.Settings
{
    public class CabochaSetting
    {
        public static CabochaSetting Instance = new CabochaSetting();

        public string Option { get; set; }

        public string Encoding { get; set; }

        private CabochaSetting()
        {
            this.Option = "-f 1 -I 1";
            this.Encoding = "shift_jis";
        }

        public void CopyFrom(CabochaSetting src)
        {
            this.Option = src.Option;
            this.Encoding = src.Encoding;
        }

        public string FindCabochaPath()
        {
            var cabocharc_path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Cabocha", "cabocharc", null) as string;
            if (cabocharc_path == null)
            {
                return null;
            }
            var path = Directory.GetParent(Path.GetDirectoryName(cabocharc_path)).FullName;
            return Path.Combine(path, @"bin\cabocha.exe");
        }
    }
}
