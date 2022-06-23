var subdirs = Directory.GetDirectories(args[0], "UD_*");
foreach (var subdir in subdirs)
{
    var subdirname = Path.GetFileName(subdir);
    var tokens = subdirname.Substring(3).Split('-');
    if (tokens.Length != 2)
    {
        throw new Exception($"Invalid folder name format: {subdir}");
    }
    var files = Directory.GetFiles(subdir, "*.conllu");
    if (files.Length == 0)
    {
        throw new Exception($"CONLLU file not found in {subdir}.");
    }

    foreach (var file in files)
    {
        var found = false;
        using (var rdr = new StreamReader(file))
        {
            while (!rdr.EndOfStream)
            {
                var line = rdr.ReadLine();
                var tkns = line.Split('\t');
                if (tkns.Length == 10)
                {
                    var upos = tkns[3];
                    var xpos = tkns[4];
                    if (xpos.Length > 40)
                    {
                        Console.WriteLine($"{file} xpos={xpos}");
                        break;
                    }
                    if (upos != "_" && upos.IndexOf('-') >= 0)
                    {
                        //found = true;
                        //Console.WriteLine($"{file} upos={upos}");
                        //break;
                    }
                    if (xpos != "_" && xpos.IndexOf('-') >= 0)
                    {
                        //found = true;
                        //Console.WriteLine($"{file} xpos={xpos}");
                        break;
                    }
                }
            }
        }
    }
}