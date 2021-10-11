using System;
using System.IO;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Corpora;
using ChaKi.Service.Readers;
using CreateCorpusSLA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using System.Text.RegularExpressions;

namespace JoinTest
{
    [TestClass]
    public class CreateCorpusTest
    {
        [TestMethod]
        public void 複数ドキュメントにおけるStartChar値とEndChar値の確認()
        {
            // CorpusSourceReaderFactoryはReaderDEfs.xmlをChaKi.NET.exeの置かれた場所から読み取るが、
            // Testの場合exeがVisualStudio IDE(devenv.exe)であるため正常に読み取れない。
            // そこで、先にInstanceをReaderDefsファイルを指定して生成しておく。
            CorpusSourceReaderFactory.CreateInstance(@"..\..\..\..\bin\Release\ReaderDefs.xml");

            File.Copy(@"..\..\..\Test\JoinTest\data\data01.cabocha", "data01.cabocha", true);
            CreateCorpus cc = new CreateCorpus()
            {
                InputPath = "data01.cabocha",
                CorpusName = "data01.db",
                TextEncoding = "UTF-8",
                ReaderType = "Auto",
                BibSource = null,
                LexSource = null,
                IsCreatingDictionary = false,
                SynchronousOff = false,
                DoNotPauseOnExit = true
            };
            Assert.IsTrue(cc.DoAllSteps());

            // 結果の照合
            var csvc = CorpusService.Create("data01.db"); 
            using (var session = csvc.OpenSession())
            {
                IQuery q;
                q = session.CreateQuery("from Document");
                Assert.AreEqual<int>(4, q.List<Document>().Count);  // Default Documentが存在するので3ではなく4が正しい.

                q = session.CreateQuery("from Sentence");
                var res1 = q.List<Sentence>();
                Assert.AreEqual<int>(3, res1.Count);
                Assert.AreEqual<string>("[Sen 0,0,10,1,0]", res1[0].ToString());
                Assert.AreEqual<string>("[Sen 1,0,10,2,0]", res1[1].ToString());
                Assert.AreEqual<string>("[Sen 2,0,10,3,0]", res1[2].ToString());

                q = session.CreateQuery("from Word order by Sen.ID asc, Pos asc");
                var res2 = q.List<Word>();
                Assert.AreEqual<int>(21, res2.Count);
                // IDは無視して結果チェックする.
                Assert.AreEqual<string>("[Word *,0,0,1,7,0,0]", Regex.Replace(res2[0].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,0,1,2,3,0,1]", Regex.Replace(res2[1].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,0,2,4,9,1,2]", Regex.Replace(res2[2].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,0,4,5,4,1,3]", Regex.Replace(res2[3].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,0,5,7,8,2,4]", Regex.Replace(res2[4].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,0,7,9,2,2,5]", Regex.Replace(res2[5].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,0,9,10,0,2,6]", Regex.Replace(res2[6].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,1,0,1,7,4,0]", Regex.Replace(res2[7].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,1,1,2,3,4,1]", Regex.Replace(res2[8].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,1,2,4,9,5,2]", Regex.Replace(res2[9].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,1,4,5,4,5,3]", Regex.Replace(res2[10].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,1,5,7,6,6,4]", Regex.Replace(res2[11].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,1,7,9,2,6,5]", Regex.Replace(res2[12].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,1,9,10,0,6,6]", Regex.Replace(res2[13].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,2,0,1,7,8,0]", Regex.Replace(res2[14].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,2,1,2,3,8,1]", Regex.Replace(res2[15].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,2,2,4,9,9,2]", Regex.Replace(res2[16].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,2,4,5,4,9,3]", Regex.Replace(res2[17].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,2,5,7,5,10,4]", Regex.Replace(res2[18].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,2,7,9,2,10,5]", Regex.Replace(res2[19].ToString(), @"\[Word [\d]+", "[Word *"));
                Assert.AreEqual<string>("[Word *,2,9,10,0,10,6]", Regex.Replace(res2[20].ToString(), @"\[Word [\d]+", "[Word *"));

                q = session.CreateQuery("from Segment");
                var res3 = q.List<Segment>();
                Assert.AreEqual<int>(12, res3.Count);
                Assert.AreEqual<string>("[Seg 0,3,0,1,0,2,0,0,0]", res3[0].ToString());
                Assert.AreEqual<string>("[Seg 1,3,0,1,2,5,0,0,0]", res3[1].ToString());
                Assert.AreEqual<string>("[Seg 2,3,0,1,5,10,0,0,0]", res3[2].ToString());
                Assert.AreEqual<string>("[Seg 3,3,0,1,10,10,0,0,0]", res3[3].ToString());
                Assert.AreEqual<string>("[Seg 4,3,0,2,0,2,0,0,1]", res3[4].ToString());
                Assert.AreEqual<string>("[Seg 5,3,0,2,2,5,0,0,1]", res3[5].ToString());
                Assert.AreEqual<string>("[Seg 6,3,0,2,5,10,0,0,1]", res3[6].ToString());
                Assert.AreEqual<string>("[Seg 7,3,0,2,10,10,0,0,1]", res3[7].ToString());
                Assert.AreEqual<string>("[Seg 8,3,0,3,0,2,0,0,2]", res3[8].ToString());
                Assert.AreEqual<string>("[Seg 9,3,0,3,2,5,0,0,2]", res3[9].ToString());
                Assert.AreEqual<string>("[Seg 10,3,0,3,5,10,0,0,2]", res3[10].ToString());
                Assert.AreEqual<string>("[Seg 11,3,0,3,10,10,0,0,2]", res3[11].ToString());

                q = session.CreateQuery("from Link");
                var res4 = q.List<Link>();
                Assert.AreEqual<int>(9, res4.Count);
                Assert.AreEqual<string>("[Link 0,7,0,0,1,0,0,0,0]", res4[0].ToString());
                Assert.AreEqual<string>("[Link 1,7,0,1,2,0,0,0,0]", res4[1].ToString());
                Assert.AreEqual<string>("[Link 2,7,0,2,3,0,0,0,0]", res4[2].ToString());
                Assert.AreEqual<string>("[Link 3,7,0,4,5,0,0,1,1]", res4[3].ToString());
                Assert.AreEqual<string>("[Link 4,7,0,5,6,0,0,1,1]", res4[4].ToString());
                Assert.AreEqual<string>("[Link 5,7,0,6,7,0,0,1,1]", res4[5].ToString());
                Assert.AreEqual<string>("[Link 6,7,0,8,9,0,0,2,2]", res4[6].ToString());
                Assert.AreEqual<string>("[Link 7,7,0,9,10,0,0,2,2]", res4[7].ToString());
                Assert.AreEqual<string>("[Link 8,7,0,10,11,0,0,2,2]", res4[8].ToString());
            }
        }

        [TestMethod]
        public void DBスキーマV6からV7への移行()
        {
            File.Copy(@"..\..\..\Test\JoinTest\data\data01_V6.db", "data01_V7.db", true);

            // V6のDBをスキーマコンバートにかける.
            var csvc = CorpusService.Create("data01_V7.db");
            csvc.LoadSchemaVersion();
            var b = csvc.ConvertSchema(new Action<string>(s => { }));
            Assert.IsTrue(b);

            // 結果の照合
            using (var session = csvc.OpenSession())
            {
                IQuery q;
                q = session.CreateQuery("from Document");
                Assert.AreEqual<int>(4, q.List<Document>().Count);  // Default Documentが存在するので3ではなく4が正しい.

                q = session.CreateQuery("from Sentence");
                var res1 = q.List<Sentence>();
                Assert.AreEqual<int>(3, res1.Count);
                Assert.AreEqual<string>("[Sen 0,0,10,1,0]", res1[0].ToString());
                Assert.AreEqual<string>("[Sen 1,0,10,2,0]", res1[1].ToString());
                Assert.AreEqual<string>("[Sen 2,0,10,3,0]", res1[2].ToString());

                q = session.CreateQuery("from Word");
                var res2 = q.List<Word>();
                Assert.AreEqual<int>(21, res2.Count);
                Assert.AreEqual<string>("[Word 0,0,0,1,7,0,0]", res2[0].ToString());
                Assert.AreEqual<string>("[Word 1,0,1,2,3,0,1]", res2[1].ToString());
                Assert.AreEqual<string>("[Word 2,0,2,4,9,1,2]", res2[2].ToString());
                Assert.AreEqual<string>("[Word 3,0,4,5,4,1,3]", res2[3].ToString());
                Assert.AreEqual<string>("[Word 4,0,5,7,8,2,4]", res2[4].ToString());
                Assert.AreEqual<string>("[Word 5,0,7,9,2,2,5]", res2[5].ToString());
                Assert.AreEqual<string>("[Word 6,0,9,10,0,2,6]", res2[6].ToString());
                Assert.AreEqual<string>("[Word 7,1,0,1,7,4,0]", res2[7].ToString());
                Assert.AreEqual<string>("[Word 8,1,1,2,3,4,1]", res2[8].ToString());
                Assert.AreEqual<string>("[Word 9,1,2,4,9,5,2]", res2[9].ToString());
                Assert.AreEqual<string>("[Word 10,1,4,5,4,5,3]", res2[10].ToString());
                Assert.AreEqual<string>("[Word 11,1,5,7,6,6,4]", res2[11].ToString());
                Assert.AreEqual<string>("[Word 12,1,7,9,2,6,5]", res2[12].ToString());
                Assert.AreEqual<string>("[Word 13,1,9,10,0,6,6]", res2[13].ToString());
                Assert.AreEqual<string>("[Word 14,2,0,1,7,8,0]", res2[14].ToString());
                Assert.AreEqual<string>("[Word 15,2,1,2,3,8,1]", res2[15].ToString());
                Assert.AreEqual<string>("[Word 16,2,2,4,9,9,2]", res2[16].ToString());
                Assert.AreEqual<string>("[Word 17,2,4,5,4,9,3]", res2[17].ToString());
                Assert.AreEqual<string>("[Word 18,2,5,7,5,10,4]", res2[18].ToString());
                Assert.AreEqual<string>("[Word 19,2,7,9,2,10,5]", res2[19].ToString());
                Assert.AreEqual<string>("[Word 20,2,9,10,0,10,6]", res2[20].ToString());

                q = session.CreateQuery("from Segment");
                var res3 = q.List<Segment>();
                Assert.AreEqual<int>(12, res3.Count);
                Assert.AreEqual<string>("[Seg 0,0,0,1,0,2,0,0,0]", res3[0].ToString());
                Assert.AreEqual<string>("[Seg 1,0,0,1,2,5,0,0,0]", res3[1].ToString());
                Assert.AreEqual<string>("[Seg 2,0,0,1,5,10,0,0,0]", res3[2].ToString());
                Assert.AreEqual<string>("[Seg 3,0,0,1,10,10,0,0,0]", res3[3].ToString());
                Assert.AreEqual<string>("[Seg 4,0,0,2,0,2,0,0,1]", res3[4].ToString());
                Assert.AreEqual<string>("[Seg 5,0,0,2,2,5,0,0,1]", res3[5].ToString());
                Assert.AreEqual<string>("[Seg 6,0,0,2,5,10,0,0,1]", res3[6].ToString());
                Assert.AreEqual<string>("[Seg 7,0,0,2,10,10,0,0,1]", res3[7].ToString());
                Assert.AreEqual<string>("[Seg 8,0,0,3,0,2,0,0,2]", res3[8].ToString());
                Assert.AreEqual<string>("[Seg 9,0,0,3,2,5,0,0,2]", res3[9].ToString());
                Assert.AreEqual<string>("[Seg 10,0,0,3,5,10,0,0,2]", res3[10].ToString());
                Assert.AreEqual<string>("[Seg 11,0,0,3,10,10,0,0,2]", res3[11].ToString());

                q = session.CreateQuery("from Link");
                var res4 = q.List<Link>();
                Assert.AreEqual<int>(9, res4.Count);
                Assert.AreEqual<string>("[Link 0,4,0,0,1,0,0,0,0]", res4[0].ToString());
                Assert.AreEqual<string>("[Link 1,4,0,1,2,0,0,0,0]", res4[1].ToString());
                Assert.AreEqual<string>("[Link 2,4,0,2,3,0,0,0,0]", res4[2].ToString());
                Assert.AreEqual<string>("[Link 3,4,0,4,5,0,0,1,1]", res4[3].ToString());
                Assert.AreEqual<string>("[Link 4,4,0,5,6,0,0,1,1]", res4[4].ToString());
                Assert.AreEqual<string>("[Link 5,4,0,6,7,0,0,1,1]", res4[5].ToString());
                Assert.AreEqual<string>("[Link 6,4,0,8,9,0,0,2,2]", res4[6].ToString());
                Assert.AreEqual<string>("[Link 7,4,0,9,10,0,0,2,2]", res4[7].ToString());
                Assert.AreEqual<string>("[Link 8,4,0,10,11,0,0,2,2]", res4[8].ToString());
            }
        }
    }
}
