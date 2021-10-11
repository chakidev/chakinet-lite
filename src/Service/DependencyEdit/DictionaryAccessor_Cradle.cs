using ChaKi.Entity.Corpora;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    class DictionaryAccessor_Cradle : DictionaryAccessor
    {
        private Dictionary_Cradle m_Dict;

        static DictionaryAccessor_Cradle()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        }

        public DictionaryAccessor_Cradle(Dictionary_Cradle dict)
        {
            m_Dict = dict;
        }

        public override string Name
        {
            get { return m_Dict.Name; }
        }

        public override bool IsConnected
        {
            get { return true; }
        }

        public override void Dispose()
        {
            // do nothing
        }

        public override IList<Lexeme> FindLexemeBySurface(string surface)
        {
            var result = new List<Lexeme>();

            var client = new WebClient();
            client.Encoding = new UTF8Encoding();
            var criteria = new JObject(
                                new JProperty("surface", surface)
                            );
            var json = client.DownloadString(
                new Uri(string.Format("{0}find_word?criteria={1}", m_Dict.Url, criteria.ToString())));
            var jarray = JArray.Parse(json);
            foreach (var item in jarray)
            {
                result.Add(Json2Lexeme(item));
            }
            return result;
        }

        public Lexeme Json2Lexeme(JToken item)
        {
            if (item == null)
            {
                return null;
            }
            var lex = new Lexeme();
            lex.SID = ToSafeString(item["ID"]);
            lex.Surface = ToSafeString(item["Surface"]);
            var props = item["Properties"];
            if (props != null)
            {
                lex.BaseLexeme = new Lexeme() { Surface = ToSafeString(props["base"]) };
                lex.Pronunciation = ToSafeString(props["pronunciation"]);
                lex.Reading = ToSafeString(props["reading"]);
                lex.Lemma = ToSafeString(props["lemma"]);
                lex.LemmaForm = ToSafeString(props["lemmaForm"]);
                lex.PartOfSpeech = new PartOfSpeech(ToSafeString(props["pos"]));
                lex.CType = new CType(ToSafeString(props["ctype"]));
                lex.CForm = new CForm(ToSafeString(props["cform"]));
                var dicts = item["Dictionaries"]?.ToArray();
                if (dicts != null)
                {
                    lex.Dictionary = string.Join(",", from t in dicts select ToSafeString(t));
                }
            }
            return lex;
        }

        public override IList<MWE> FindMWEBySurface(string surface)
        {
            return FindMWEBySurface_Impl(surface, "find_mwe");
        }

        public override IList<MWE> FindMWEBySurface2(string surface)
        {
            return FindMWEBySurface_Impl(surface, "find_mwe2");
        }

        private IList<MWE> FindMWEBySurface_Impl(string surface, string apiname)
        {
            var result = new List<MWE>();

            var client = new WebClient();
            client.Encoding = new UTF8Encoding();
            var json = client.DownloadString(
                new Uri($"{m_Dict.Url}{apiname}?surface={surface}"));
            var jarray = JArray.Parse(json);
            foreach (var item in jarray)
            {
                var mwe = new MWE();
                mwe.Lex = Json2Lexeme(item);
                var deps = item["Dependencies"] as JArray;
                foreach (var token in deps)
                {
                    var mwenode = new MWENode();
                    if (token["DepTo"] == null)
                    {
                        mwenode.DependsTo = -1;
                    }
                    else
                    {
                        mwenode.DependsTo = token["DepTo"].Value<int>();
                    }
                    if (token["DepAs"] == null)
                    {
                        mwenode.DependsAs = "";
                    }
                    else
                    {
                        mwenode.DependsAs = token["DepAs"].Value<string>();
                    }
                    JToken nodeLex = null;
                    try
                    {
                        nodeLex = token["Lex"];
                        if (nodeLex != null)
                        {
                            mwenode.Label = ToSafeString(nodeLex["Surface"]);
                            mwenode.NodeType = (MWENodeType)nodeLex["NodeType"].Value<int>();
                            mwenode.SrcLex = Json2Lexeme(nodeLex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    mwe.Items.Add(mwenode);
                }
                result.Add(mwe);
            }

            return result;
        }

        public override void RegisterMWE(MWE mwe)
        {
            var client = new WebClient();
            client.Encoding = new UTF8Encoding();

            var ser = new JsonSerializer();
            using (var sw = new StringWriter())
            using (var wr = new JsonTextWriter(sw))
            {
                ser.Serialize(wr, mwe);
                var body = sw.ToString();
                Console.WriteLine(body);
                client.UploadString(new Uri(string.Format("{0}new_mwe", m_Dict.Url)),body);
            }
        }


        private string ToSafeString(JToken item)
        {
            if (item == null)
            {
                return string.Empty;
            }
            return item.Value<string>();
        }
    }
}
