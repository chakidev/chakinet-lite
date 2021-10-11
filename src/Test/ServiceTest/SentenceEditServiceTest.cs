using ChaKi.Service.SentenceEdit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChaKi.Entity.Corpora;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using ChaKi.Service.Corpora;
using NHibernate;

namespace ChaKi.ServiceTest
{
    
    
    /// <summary>
    ///SentenceEditServiceTest のテスト クラスです。すべての
    ///SentenceEditServiceTest 単体テストをここに含めます
    ///</summary>
    [TestClass()]
    public class SentenceEditServiceTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///現在のテストの実行についての情報および機能を
        ///提供するテスト コンテキストを取得または設定します。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 追加のテスト属性
        // 
        //テストを作成するときに、次の追加属性を使用することができます:
        //
        //クラスの最初のテストを実行する前にコードを実行するには、ClassInitialize を使用
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //クラスのすべてのテストを実行した後にコードを実行するには、ClassCleanup を使用
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //各テストを実行する前にコードを実行するには、TestInitialize を使用
        [TestInitialize()]
        public void MyTestInitialize()
        {
#if false
            Process proc;
            proc = Process.Start(@"..\..\..\..\bin\Release\CreateCorpusSLA.exe",
                string.Format(@"-t=Mecab|Cabocha|UniDic ..\..\..\Test\ServiceTest\SentenceEditServiceTestData1.cabocha SentenceEditServiceTestData1.db"));
            proc.WaitForExit();
            proc = Process.Start(@"..\..\..\..\bin\Release\CreateCorpusSLA.exe",
                string.Format(@"-t=Mecab|Cabocha|UniDic ..\..\..\Test\ServiceTest\SentenceEditServiceTestData2.cabocha SentenceEditServiceTestData2.db"));
            proc.WaitForExit();
            proc = Process.Start(@"..\..\..\..\bin\Release\CreateCorpusSLA.exe",
                string.Format(@"-t=Mecab|Cabocha|UniDic ..\..\..\Test\ServiceTest\SentenceEditServiceTestData0.cabocha SentenceEditServiceTestData0.db"));
            proc.WaitForExit();
#endif
        }
        
        //各テストを実行した後にコードを実行するには、TestCleanup を使用
        [TestCleanup()]
        public void MyTestCleanup()
        {
            //File.Delete("SentenceEditServiceTestData1.db");
        }
        #endregion


        /// <summary>
        ///MergeSentence のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NHibernate.ByteCode.Castle.dll")]
        public void MergeSentenceTest()
        {
#if false
            SentenceEditService target = new SentenceEditService(); // TODO: 適切な値に初期化してください
            Corpus cps = Corpus.CreateFromFile("SentenceEditServiceTestData2.db");
            target.Open(cps, null);
            target.MergeSentence(0);
            target.MergeSentence(0);
            target.MergeSentence(0);
            target.MergeSentence(0);
            target.MergeSentence(0);
            target.MergeSentence(0);
            target.Commit();
#endif
        }

        /// <summary>
        ///SplitSentence のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NHibernate.ByteCode.Castle.dll")]
        public void 単純な1回のSplit()
        {
            File.Copy(@"..\..\..\Test\ServiceTest\SplitSentenceTestData.db", "SplitSentenceTestData.db", true);
            Corpus cps = Corpus.CreateFromFile("SplitSentenceTestData.db");
            using (SentenceEditService target = new SentenceEditService())
            {
                target.Open(cps, null);
                Sentence result = target.SplitSentence(0, 1);
                target.Commit();
                target.Close();
            }
            
            CorpusService csvc = CorpusService.Create("SplitSentenceTestData.db");
            using (ISession session = csvc.OpenSession())
            {
                IQuery q;
                q = session.CreateQuery("from Sentence order by ID asc");
                var res1 = q.List<Sentence>();
                Assert.AreEqual<int>(7, res1.Count);
                Assert.AreEqual<string>("[Sen 0,0,1,2,0]", res1[0].ToString());
                Assert.AreEqual<string>("[Sen 1,1,3,2,1]", res1[1].ToString());
            }
        }

        /// <summary>
        ///SplitSentence のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NHibernate.ByteCode.Castle.dll")]
        public void 第1ドキュメントの最初の文を2回Splitする()
        {
            File.Copy(@"..\..\..\Test\ServiceTest\SplitSentenceTestData.db", "SplitSentenceTestData.db", true);
            Corpus cps = Corpus.CreateFromFile("SplitSentenceTestData.db");
            using (SentenceEditService target = new SentenceEditService())
            {
                IList<Sentence> isentences = new List<Sentence>();
                target.Open(cps, null);
                target.ReloadSentences(ref isentences, 2, 0, 3);
                target.ChangeBoundaries(isentences, new List<int>() { 1, 1, 1 });
                target.Commit();
                target.Close();
            }

            CorpusService csvc = CorpusService.Create("SplitSentenceTestData.db");
            using (ISession session = csvc.OpenSession())
            {
                IQuery q;
                q = session.CreateQuery("from Sentence order by ID asc");
                var res1 = q.List<Sentence>();
                Assert.AreEqual<int>(8, res1.Count);  // 元は6文,Splitにより+2
                Assert.AreEqual<string>("[Sen 0,0,1,2,0]", res1[0].ToString());
                Assert.AreEqual<string>("[Sen 1,1,2,2,1]", res1[1].ToString());
                Assert.AreEqual<string>("[Sen 2,2,3,2,2]", res1[2].ToString());
            }
        }

        /// <summary>
        ///SplitSentence のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NHibernate.ByteCode.Castle.dll")]
        public void 第3ドキュメントの最初の文を2回Splitする()
        {
            File.Copy(@"..\..\..\Test\ServiceTest\SplitSentenceTestData.db", "SplitSentenceTestData.db", true);
            Corpus cps = Corpus.CreateFromFile("SplitSentenceTestData.db");
            using (SentenceEditService target = new SentenceEditService())
            {
                IList<Sentence> isentences = new List<Sentence>();
                target.Open(cps, null);
                target.ReloadSentences(ref isentences, 4, 0, 3);
                target.ChangeBoundaries(isentences, new List<int>() { 1, 1, 1 });
                target.Commit();
                target.Close();
            }

            CorpusService csvc = CorpusService.Create("SplitSentenceTestData.db");
            using (ISession session = csvc.OpenSession())
            {
                IQuery q;
                q = session.CreateQuery("from Sentence order by ID asc");
                var res1 = q.List<Sentence>();
                Assert.AreEqual<int>(8, res1.Count);  // 元は6文,Splitにより+2
                Assert.AreEqual<string>("[Sen 4,0,1,4,0]", res1[4].ToString());
                Assert.AreEqual<string>("[Sen 5,1,2,4,1]", res1[5].ToString());
                Assert.AreEqual<string>("[Sen 6,2,3,4,2]", res1[6].ToString());
                Assert.AreEqual<string>("[Sen 7,3,6,4,3]", res1[7].ToString());
            }
        }

        /// <summary>
        ///SplitSentence のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NHibernate.ByteCode.Castle.dll")]
        public void コーパス最後の文を2回Splitする()
        {
            File.Copy(@"..\..\..\Test\ServiceTest\SplitSentenceTestData.db", "SplitSentenceTestData.db", true);
            Corpus cps = Corpus.CreateFromFile("SplitSentenceTestData.db");
            using (SentenceEditService target = new SentenceEditService())
            {
                IList<Sentence> isentences = new List<Sentence>();
                target.Open(cps, null);
                target.ReloadSentences(ref isentences, 4, 3, 6);
                target.ChangeBoundaries(isentences, new List<int>() { 1, 1, 1 });
                target.Commit();
                target.Close();
            }

            CorpusService csvc = CorpusService.Create("SplitSentenceTestData.db");
            using (ISession session = csvc.OpenSession())
            {
                IQuery q;
                q = session.CreateQuery("from Sentence order by ID asc");
                var res1 = q.List<Sentence>();
                Assert.AreEqual<int>(8, res1.Count);  // 元は6文,Splitにより+2
                Assert.AreEqual<string>("[Sen 5,3,4,4,1]", res1[5].ToString());
                Assert.AreEqual<string>("[Sen 6,4,5,4,2]", res1[6].ToString());
                Assert.AreEqual<string>("[Sen 7,5,6,4,3]", res1[7].ToString());
            }
        }

        [TestMethod()]
        [DeploymentItem("NHibernate.ByteCode.Castle.dll")]
        public void MergeMultiSentenceTest()
        {
            File.Copy(@"..\..\..\Test\ServiceTest\MergeMultiSentenceTestData.db", "MergeMultiSentenceTestData.db", true);
            SentenceEditService target = new SentenceEditService();
            Corpus cps = Corpus.CreateFromFile("MergeMultiSentenceTestData.db");
            target.Open(cps, null);
            List<Sentence> sentences = new List<Sentence>();
            IList<Sentence> isentences = sentences;
            target.ReloadSentences(ref isentences, 1, 0, 5);
            Assert.AreEqual(isentences[0].GetText(false), "A");
            Assert.AreEqual(isentences[1].GetText(false), "B");
            Assert.AreEqual(isentences[2].GetText(false), "C");
            Assert.AreEqual(isentences[3].GetText(false), "D");
            Assert.AreEqual(isentences[4].GetText(false), "E");
            // ここまでは事前確認
            // ----------------------------------
            // 長さ5の1文("ABCDE")へMergeさせる
            target.ChangeBoundaries(isentences, new List<int>() { 5 });
            target.Commit();
            target.ReloadSentences(ref isentences, 1, 0, 5);
            Assert.AreEqual("ABCDE", isentences[0].GetText(false));
            Assert.AreEqual(1, isentences.Count);
            foreach (Sentence sen in isentences)
            {
                // check first sentence
                var words = sen.GetWords(0);
                Assert.AreEqual(5, words.Count);
                Assert.AreEqual(0, (words[0]).StartChar);
                Assert.AreEqual(1, (words[0]).EndChar);
                Assert.AreEqual(1, (words[1]).StartChar);
                Assert.AreEqual(2, (words[1]).EndChar);
                Assert.AreEqual(2, (words[2]).StartChar);
                Assert.AreEqual(3, (words[2]).EndChar);
                Assert.AreEqual(3, (words[3]).StartChar);
                Assert.AreEqual(4, (words[3]).EndChar);
                Assert.AreEqual(4, (words[4]).StartChar);
                Assert.AreEqual(5, (words[4]).EndChar);
                break;
            }
        }

        [TestMethod()]
        [DeploymentItem("NHibernate.ByteCode.Castle.dll")]
        public void MergeSentenceNotThrowCountMismatchExceptionTest()
        {
            File.Copy(@"..\..\..\Test\ServiceTest\MergeSentenceNotThrowCountMismatchExceptionTestData.db",
                "MergeSentenceNotThrowCountMismatchExceptionTest.db", true);
            SentenceEditService target = new SentenceEditService();
            Corpus cps = Corpus.CreateFromFile("MergeSentenceNotThrowCountMismatchExceptionTest.db");
            target.Open(cps, null);
            IList<Sentence> sentences = new List<Sentence>();
            target.ReloadSentences(ref sentences, 1, 0, 3);
            bool exception_occurred = false;
            try
            {
                target.ChangeBoundaries(sentences, new List<int>() { 3 });
            }
            catch (System.Exception ex)
            {
                string msg = ex.Message;
                exception_occurred = true;
            }
            Assert.IsFalse(exception_occurred);
            target.Commit();
        }
    }
}
