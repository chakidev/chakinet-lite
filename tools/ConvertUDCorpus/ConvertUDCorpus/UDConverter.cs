using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertUDCorpus
{
    public class UDConverter
    {
        public void Convert(string srcdir, string dstdir)
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
                    Directory.CreateDirectory(dstdirname);
                }
                var srcpath = ConcatConllFiles(subdir);
                var dstpath = Path.Combine(dstdirname, Path.GetFileName(srcpath) + ".db");
                var psi = new ProcessStartInfo()
                {
                    FileName = "CreateCorpusSLA.exe",
                    WorkingDirectory = ".",
                    Arguments = $"-C -t=CONLLU -e=UTF-8 -s {srcpath} {dstpath}"
                };
                Process.Start(psi).WaitForExit();
                File.Delete(srcpath);
                Console.Error.WriteLine(dstpath);
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
                Directory.CreateDirectory(dstdir);
            }
            else
            {
                if (Directory.EnumerateFileSystemEntries(dstdir).Any())
                {
                    throw new Exception("Dest directory is not empty.");
                }
            }
        }

        private string ConcatConllFiles(string dir)
        {
            var files = Directory.GetFiles(dir, "*.conllu");
            if (files.Length == 0)
            {
                throw new Exception($"CONLLU file not found in {dir}.");
            }
            var nm = Path.GetFileName(files[0]);
            var i = nm.LastIndexOf('-');
            var outname = nm.Substring(0, i);
            var tmppath = Path.Combine(Path.GetTempPath(), outname);
            using (var wr = new StreamWriter(tmppath, false, Encoding.UTF8))
            {
                foreach (var file in files)
                {
                    using (var rdr = new StreamReader(file))
                    {
                        while (!rdr.EndOfStream)
                        {
                            var line = rdr.ReadLine();
                            wr.WriteLine(line);
                        }
                    }
                }
            }
            return tmppath;
        }

    }
}
