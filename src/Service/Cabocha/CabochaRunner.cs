using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora;
using System.IO;
using ChaKi.Service.Export;
using ChaKi.Entity.Corpora.Annotations;
using System.Text.RegularExpressions;

namespace ChaKi.Service.Cabocha
{
    public class CabochaRunner : ICabochaRunner
    {
        private int m_TargetProjectId;

        public CabochaRunner()
        {
            m_TargetProjectId = 0;
        }

        public CabochaRunner(int targetProjectId)
        {
            m_TargetProjectId = targetProjectId;
        }

        public string ParseWithSegmentInfo(Corpus corpus, Sentence sen, IList<Segment> segs)
        {
            // Segment情報を用いて、Sentenceを部分Word列に分解
            var part_sen_list = ExtractPartialSentences(sen, segs);

            // 分解された各部分Word列をMecab形式に変換（文節情報を落とす）
            var wr = new StringWriter();
            var mecab_exporter = new ExportServiceMecab(wr);
            foreach (var part_sen in part_sen_list)
            {
                mecab_exporter.ExportItem(corpus, part_sen.Words);
            }
            
            // 各Mecab形式を入力としてCabocha Chunk解析のみを行う
            var output = Parse(wr.ToString());
            if (part_sen_list.Count != output.Count)
            {
                throw new Exception(string.Format("Incorrect parse results[Count={0}] for sentence parts[COount={1}].",
                    output.Count, part_sen_list.Count));
            }
            for (int i = 0; i < part_sen_list.Count; i++)
            {
                part_sen_list[i].CabochaResult = output[i];
            }

            // 分離されている解析結果(Cabocha出力のリスト)をマージする.
            return MergePartialSentences(sen, part_sen_list, mecab_exporter);
        }

        // Sentence中のSegment(Nest)を分解し、部分Word列(PartialSentence)のリストにして返す.
        private IList<PartialSentence> ExtractPartialSentences(Sentence sen, IList<Segment> segs)
        {
            // Word境界にあるSegment部分を分離する.但し、分離幅が大きいものから順に、既に分離されたものは無視する.
            // (将来Mecabレベルで再解析する場合にはWord境界を意識せず、平文まで戻して再解析させる.)
            var seg_ordered = from s in segs orderby (s.EndChar - s.StartChar) descending select s;
            var part_sen_map = new Dictionary<Word, PartialSentence>();
            foreach (Segment s in seg_ordered)
            {
                var part_sen = new PartialSentence(sen.GetWordsInRange(s.StartChar, s.EndChar));
                
                if (part_sen.Words.Count > 0)
                {
                    foreach (var w in part_sen.Words)
                    {
                        if (part_sen_map.ContainsKey(w))
                        {
                            break;
                        }
                        part_sen_map.Add(w, part_sen);
                    }
                }
            }
            // 抽出したSegment部分以外の主文を得る.
            // このとき、Segmentの前後が括弧類であれば、それを削除し、Segment部分を記号「…」で置き換える.
            var primary_sen = new PartialSentence(new List<Word>(), true);
            var words = sen.GetWords(m_TargetProjectId);
            foreach (var w in words)
            {
                if (part_sen_map.ContainsKey(w))
                {
                    primary_sen.Words.Add(null);  // Use null as a "leap" mark
                }
                else
                {
                    primary_sen.Words.Add(w);
                    part_sen_map.Add(w, primary_sen);
                }
            }
            return part_sen_map.Values.Distinct<PartialSentence>().ToList<PartialSentence>();
        }

        // Cabocha解析済みの部分から、単一のSentenceのCabocha解析結果にマージして結果を返す.
        private string MergePartialSentences(Sentence sen, IList<PartialSentence> part_sen_list, ExportServiceMecab mecab_exporter)
        {
            if (part_sen_list.Count == 0)
            {
                return string.Empty;
            }

            PartialSentence primary_sen = part_sen_list[part_sen_list.Count - 1];
            part_sen_list.Remove(primary_sen);
            var primary_bunsetsu_map = new Dictionary<int, int>();
            for (int i = 0; i < primary_sen.BunsetsuCount; i++)
            {
                primary_bunsetsu_map.Add(i, i);
            }

            var sb = new StringBuilder();
            int bunsetsu_count = 0;
            // まず、仮Word "…"をすべて元に戻す.
            using (var rdr = new StringReader(primary_sen.CabochaResult))
            {
                string line;
                int current_word = -1;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (line.StartsWith("*"))
                    {
                        bunsetsu_count++;
                    }
                    else
                    {
                        current_word++;
                    }
                    if (line.StartsWith("…	記号,一般\t"))
                    {
                        var w = sen.GetWords(m_TargetProjectId)[current_word];
                        sb.Append(mecab_exporter.ExportLexeme(w.Lex));
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
            }
            string result = sb.ToString();
            sb.Length = 0;

            // 次に部分解析結果の文節情報を埋め込む.
            // 文節番号はbunsetsu_countからincrementしていく.
            var change_depends_to_later = new Dictionary<int, int>();  // 後処理でDependsToを変更する必要のある文節番号
            using (var rdr = new StringReader(result))
            {
                string line;
                int current_word = 0;
                int current_primary_bunsetsu = -1;
                int current_primary_bunsetsu_dependsto = -1;
                while ((line = rdr.ReadLine()) != null)
                {
                    var part_sen = part_sen_list.FirstOrDefault<PartialSentence>(p => (p.WordStartPos == current_word));
                    if (part_sen != null)
                    {
                        // 現在の位置が部分解析（part_sen）の始まりである場合
                        int part_word_count = 0;
                        using (var rdr2 = new StringReader(part_sen.CabochaResult))
                        {
                            string line2;
                            bool first_bunsetsu_in_part = true;  // 部分内の最初の文節は前の文節とマージするので、文節タグを出力しない.
                            while ((line2 = rdr2.ReadLine()) != null)
                            {
                                if (line2 == "EOS")
                                {
                                    break;
                                }
                                if (line2.StartsWith("*"))
                                {
                                    int dependsto;
                                    line2 = OffsetBunsetsuID(line2, bunsetsu_count, current_primary_bunsetsu_dependsto, out dependsto);
                                    if (first_bunsetsu_in_part)
                                    {
                                        // 主文のcurrent係り先を、部分における最初の文節（主文のcurrent文節とマージされる）の係り先に
                                        // 後で変更する（異なる場合のみ）
                                        if (dependsto != current_primary_bunsetsu_dependsto
                                            && !change_depends_to_later.ContainsKey(current_primary_bunsetsu))
                                        {
                                            change_depends_to_later.Add(current_primary_bunsetsu, dependsto);
                                        }
                                        first_bunsetsu_in_part = false;
                                        continue;
                                    }
                                }
                                else
                                {
                                    part_word_count++;
                                }
                                sb.AppendLine(line2);
                            }
                        }
                        // (part_word_count-1)分の行数だけ、rdrを空読みする.
                        for (int i = 0; i < part_word_count-1; i++)
                        {
                            rdr.ReadLine();
                        }
                        current_word += part_word_count;
                        bunsetsu_count += part_sen.BunsetsuCount;
                    }
                    else
                    {
                        sb.AppendLine(line);
                        if (!line.StartsWith("*"))
                        {
                            current_word++;
                        }
                        else
                        {
                            var regex = new Regex(@"^* ([-\d]+) ([-\d]+)[^\d]");
                            var m = regex.Match(line);
                            if (m.Success && m.Groups.Count > 2)
                            {
                                current_primary_bunsetsu = Int32.Parse(m.Groups[1].Value);
                                current_primary_bunsetsu_dependsto = Int32.Parse(m.Groups[2].Value);
                            }
                        }
                    }
                }
            }
            // change_depends_to_laterを適用する.
            result = sb.ToString();
            sb.Length = 0;
            using (var rdr = new StringReader(result))
            {
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (line.StartsWith("*"))
                    {
                        int id;
                        int dependsTo;
                        string dependsAs;
                        var tokens = ParseBunsetsuTag(line, out id, out dependsTo, out dependsAs);
                        int newdependsTo;
                        if (change_depends_to_later.TryGetValue(id, out newdependsTo))
                        {
                            tokens[2] = string.Format("{0}{1}", newdependsTo, dependsAs);
                            sb.AppendLine(string.Join(" ", tokens));
                            continue;
                        }
                    }
                    sb.AppendLine(line);
                }
            }

            // 最後にIDを並べ替える.
            result = ReorderBunsetsuID(sb.ToString());
            return result;
        }

        private string ReorderBunsetsuID(string input)
        {
            var mapping = new Dictionary<int, int>();
            using (var rdr = new StringReader(input))
            {
                string line;
                int n = 0;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (line.StartsWith("*"))
                    {
                        int id;
                        int dependsTo;
                        string dependsAs;
                        ParseBunsetsuTag(line, out id, out dependsTo, out dependsAs);
                        mapping.Add(id, n);
                        n++;
                    }
                }
            }
            var sb = new StringBuilder();
            using (var rdr = new StringReader(input))
            {
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (line.StartsWith("*"))
                    {
                        int id;
                        int dependsTo;
                        string dependsAs;
                        var tokens = ParseBunsetsuTag(line, out id, out dependsTo, out dependsAs);
                        tokens[1] = string.Format("{0}", mapping[id]);
                        tokens[2] = string.Format("{0}{1}", (dependsTo == -1)?-1:mapping[dependsTo], dependsAs);
                        sb.AppendLine(string.Join(" ", tokens));
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
            }
            return sb.ToString();
        }

        private string OffsetBunsetsuID(string chunkdata, int offset, int current_primary_bunsetsu_dependsto, out int dependsTo)
        {
            int id;
            string dependsAs;
            var tokens = ParseBunsetsuTag(chunkdata, out id, out dependsTo, out dependsAs);
            if (tokens == null)
            {
                return chunkdata;
            }

            tokens[1] = string.Format("{0}", id + offset);
            if (dependsTo == -1)
            {
                dependsTo = current_primary_bunsetsu_dependsto;
            }
            else
            {
                dependsTo += offset;
            }
            tokens[2] = string.Format("{0}{1}", dependsTo, dependsAs);
            return string.Join(" ", tokens);
        }

        public string[] ParseBunsetsuTag(string line, out int id, out int dependsTo, out string dependsAs)
        {
            char[] numberPattern = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
            var tokens = line.Split(' ');

            id = 0;
            dependsTo = 0;
            dependsAs = string.Empty;
            if (tokens[0] != "*" || tokens.Length < 3)
            {
                return null;
            }
            id = Int32.Parse(tokens[1]);
            int pos = tokens[2].LastIndexOfAny(numberPattern);
            if (pos < 0 || pos + 1 > tokens[2].Length - 1)
            {
                return null;
            }
            dependsTo = Int32.Parse(tokens[2].Substring(0, pos + 1));
            dependsAs = tokens[2].Substring(pos + 1, tokens[2].Length - pos - 1);
            return tokens;
        }

        public IList<string> Parse(string input)
        {
            var p = new Process();
            var cabocha_setting = CabochaSetting.Instance;
            p.StartInfo.FileName = cabocha_setting.FindCabochaPath();
            p.StartInfo.Arguments = cabocha_setting.Option;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(cabocha_setting.Encoding);
            p.Start();
            var result = new List<string>();

            p.StandardInput.Write(input);
            p.StandardInput.Write("\x1a");
            var sb = new StringBuilder();
            while (!p.StandardOutput.EndOfStream)
            {
                var outs = p.StandardOutput.ReadLine(); 
                sb.Append(outs);
                sb.AppendLine();
                if (outs == "EOS")
                {
                    result.Add(sb.ToString());
                    sb.Length = 0;
                }
            }
            p.WaitForExit();
            return result;
        }

    }
}
