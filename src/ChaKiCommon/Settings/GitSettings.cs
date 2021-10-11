using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Common.Settings
{
    public class GitSettings : ICloneable
    {
        public bool ExportBunsetsuSegmentsAndLinks { get; set; }

        public string LocalRepositoryBasePath { get; set; }

        public static GitSettings Current = new GitSettings();

        private GitSettings()
        {
            this.ExportBunsetsuSegmentsAndLinks = true;
            this.LocalRepositoryBasePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    @"ChaKiRepositories");
        }

        public GitSettings Copy()
        {
            var obj = new GitSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(GitSettings src)
        {
            this.ExportBunsetsuSegmentsAndLinks = src.ExportBunsetsuSegmentsAndLinks;
            this.LocalRepositoryBasePath = src.LocalRepositoryBasePath;
        }

        public object Clone()
        {
            return Copy();
        }


    }
}
