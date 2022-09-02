using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddLicenseFiles
{
    internal class AddLicenseFiles
    {
        public void Run(string srcdir, string dstdir)
        {
            Prepare(srcdir, dstdir);

            var subdirs = Directory.GetDirectories(srcdir, "UD_*");
            foreach (var subdir in subdirs)
            {
                var subdirname = Path.GetFileName(subdir);
                var tokens = subdirname.Substring(3).Split('-');
                if (tokens.Length != 2)
                {
                    throw new Exception($"Invalid folder name format: {subdir}");
                }
                var dstdirname = Path.Combine(dstdir, tokens[0]);
                if (!Directory.Exists(dstdirname))
                {
                    throw new Exception($"Cannot find destination folder: {dstdirname}");
                }
                var srcFile = Path.Combine(subdir, "README.txt");
                var dstFile = Path.Combine(dstdirname, $"README.{subdirname}.txt");
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, dstFile, true);
                    Console.WriteLine($"Copied: {srcFile} -> {dstFile}");
                }
                srcFile = Path.Combine(subdir, "README.md");
                dstFile = Path.Combine(dstdirname, $"README.{subdirname}.md");
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, dstFile, true);
                    Console.WriteLine($"Copied: {srcFile} -> {dstFile}");
                }
                srcFile = Path.Combine(subdir, "LICENSE.txt");
                dstFile = Path.Combine(dstdirname, $"LICENSE.{subdirname}.txt");
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, dstFile, true);
                    Console.WriteLine($"Copied: {srcFile} -> {dstFile}");
                }
                srcFile = Path.Combine(subdir, "LICENSE.md");
                dstFile = Path.Combine(dstdirname, $"LICENSE.{subdirname}.md");
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, dstFile, true);
                    Console.WriteLine($"Copied: {srcFile} -> {dstFile}");
                }
            }

        }

        private void Prepare(string srcdir, string dstdir)
        {
            if (!Directory.Exists(srcdir))
            {
                throw new Exception("Source directory not found.");
            }
            if (!Directory.Exists(dstdir))
            {
                throw new Exception("Destination directory not found.");
            }
        }
    }
}
