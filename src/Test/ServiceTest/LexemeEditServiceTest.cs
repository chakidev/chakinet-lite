using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChaKi.Service.Lexicons;
using ChaKi.Entity.Corpora;
using System.IO;
using ChaKi.Service.Database;

namespace ChaKi.ServiceTest
{
    /// <summary>
    /// LexemeEditServiceTest の概要の説明
    /// </summary>
    [TestClass]
    public class LexemeEditServiceTest
    {
        public LexemeEditServiceTest()
        {
            //
            // TODO: コンストラクター ロジックをここに追加します
            //
        }

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
        // テストを作成する際には、次の追加属性を使用できます:
        //
        // クラス内で最初のテストを実行する前に、ClassInitialize を使用してコードを実行してください
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // クラス内のテストをすべて実行したら、ClassCleanup を使用してコードを実行してください
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 各テストを実行する前に、TestInitialize を使用してコードを実行してください
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // 各テストを実行した後に、TestCleanup を使用してコードを実行してください
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void ユーザー辞書へ差分を追加_非活用語()
        {
            // 準備
            //
            // "A", "B", "C"を含む参照辞書
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData1.ddb", "LexemeEditServiceTestData1.ddb", true);
            // "C", "D" から成るコーパス
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData2.db", "LexemeEditServiceTestData2.db", true);
            // 空のユーザー辞書
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData3.ddb", "LexemeEditServiceTestData3.ddb", true);
            Corpus cps = Corpus.CreateFromFile("LexemeEditServiceTestData2.db");

            // 処理本体
            //
            var service = new LexemeEditService();
            var corpora = new List<Corpus>() { cps };
            var refdics = new List<string>() { "LexemeEditServiceTestData1.ddb" };
            var userdic = "LexemeEditServiceTestData3.ddb";
            var list = service.ListLexemesNotInRefDic(corpora, refdics, null);
            if (list == null)
            {
                throw new Exception("Diff is null");
            }
            if (list.Count == 0)
            {
                throw new Exception("Diff count is 0");
            }
            foreach (var tuple in list)
            {
                tuple.Item3 = true;
            }
            service.UpdateCorpusInternalDictionaries(list);
            service.AddToUserDictionary(list, userdic);

            // 結果チェック
            // コーパスに含まれる語"C", "D"に対して、
            // "C"は参照辞書にあるので、ユーザー辞書には追加しない。
            // "D"は参照辞書にないので、ユーザー辞書に追加する。
            Assert.AreEqual(1, list.Count);
            // コーパス内部辞書のチェック ("D"は内部辞書には追加されないので、内部辞書のエントリ数は2のまま）
            var dbs = DBService.Create(cps.DBParam);
            var cfg = dbs.GetConnection();
            var factory = cfg.BuildSessionFactory();
            using (var session = factory.OpenSession())
            {
                var result = session.CreateQuery("from Lexeme").List<Lexeme>();
                Assert.AreEqual(2, result.Count);
            }
            // ユーザー辞書のチェック（ユーザー辞書は差分の"D"が追加される）
            var dict = Dictionary.Create(userdic) as Dictionary_DB;
            var conn = DBService.Create(dict.DBParam).GetConnection();
            using (var session = conn.BuildSessionFactory().OpenSession())
            {
                var result = session.CreateQuery("from Lexeme").List<Lexeme>();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("Ｄ", result[0].Surface);
            }
        }

        [TestMethod]
        public void ユーザー辞書へ差分を追加_非活用語_終止形以外のプロパティを変更()
        {
            // 準備
            //
            // "A", "B", "C"を含む参照辞書
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData1.ddb", "LexemeEditServiceTestData1.ddb", true);
            // "C", "D" から成るコーパス
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData2.db", "LexemeEditServiceTestData2.db", true);
            // 空のユーザー辞書
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData3.ddb", "LexemeEditServiceTestData3.ddb", true);
            Corpus cps = Corpus.CreateFromFile("LexemeEditServiceTestData2.db");

            // 処理本体
            //
            var service = new LexemeEditService();
            var corpora = new List<Corpus>() { cps };
            var refdics = new List<string>() { "LexemeEditServiceTestData1.ddb" };
            var userdic = "LexemeEditServiceTestData3.ddb";
            var list = service.ListLexemesNotInRefDic(corpora, refdics, null);
            if (list == null)
            {
                throw new Exception("Diff is null");
            }
            if (list.Count == 0)
            {
                throw new Exception("Diff count is 0");
            }
            Lexeme lex_to_edit = null;
            foreach (var tuple in list)
            {
                tuple.Item3 = true;
                if (tuple.Item1.Surface == "Ｄ")
                {
                    lex_to_edit = tuple.Item1;
                }
            }
            if (lex_to_edit == null)
            {
                throw new Exception("D not found in the list");
            }
            lex_to_edit.Reading = "＿ディー";
            lex_to_edit.Pronunciation = "＿ディー";
            lex_to_edit.Lemma = "＿Ｄ";
            lex_to_edit.LemmaForm = "＿Ｄ";
            lex_to_edit.CType = new CType("ダミー");
            lex_to_edit.CForm = new CForm("ダミー");

            service.UpdateCorpusInternalDictionaries(list);
            service.AddToUserDictionary(list, userdic);

            // 結果チェック
            // コーパスに含まれる語"C", "D"に対して、
            // "C"は参照辞書にあるので、ユーザー辞書には追加しない。
            // "D"は参照辞書にないので、ユーザー辞書に追加する。
            Assert.AreEqual(1, list.Count);
            // コーパス内部辞書のチェック (修正された"D"と置き換わり、また"D"の基本形も内部辞書に追加される）
            var dbs = DBService.Create(cps.DBParam);
            var cfg = dbs.GetConnection();
            var factory = cfg.BuildSessionFactory();
            using (var session = factory.OpenSession())
            {
                var result = session.CreateQuery("from Lexeme order by ID").List<Lexeme>();
                Assert.AreEqual(3, result.Count);
                Assert.AreEqual("Ｄ,＿ディー,＿Ｄ,＿ディー,Ｄ,＿Ｄ,名詞-普通名詞-一般,ダミー,ダミー", result[1].ToString());
                Assert.AreEqual("Ｄ,＿ディー,＿Ｄ,＿ディー,Ｄ,＿Ｄ,名詞-普通名詞-一般,ダミー,基本形", result[2].ToString());
                Assert.AreEqual(2, result[1].BaseLexeme.ID);
            }
            // ユーザー辞書のチェック（ユーザー辞書は差分の"D"が追加される）
            var dict = Dictionary.Create(userdic) as Dictionary_DB;
            var conn = DBService.Create(dict.DBParam).GetConnection();
            using (var session = conn.BuildSessionFactory().OpenSession())
            {
                var result = session.CreateQuery("from Lexeme order by ID").List<Lexeme>();
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("Ｄ,＿ディー,＿Ｄ,＿ディー,Ｄ,＿Ｄ,名詞-普通名詞-一般,ダミー,基本形", result[0].ToString());
                Assert.AreEqual("Ｄ,＿ディー,＿Ｄ,＿ディー,Ｄ,＿Ｄ,名詞-普通名詞-一般,ダミー,ダミー", result[1].ToString());
                Assert.AreEqual(1, result[1].BaseLexeme.ID);
            }
        }

        [TestMethod]
        public void ユーザー辞書へ差分を追加_活用語()
        {
            // 準備
            //
            // "A", "B", "C"を含む参照辞書（Cは活用語. Cの終止形も含むが、終止形のPronunciationとReadingは空になっている)
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData5.ddb", "LexemeEditServiceTestData5.ddb", true);
            // "C", "D" から成るコーパス(活用形・基本形を含め、合計4つの語を持つ)
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData4.db", "LexemeEditServiceTestData4.db", true);
            // 空のユーザー辞書
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData3.ddb", "LexemeEditServiceTestData3.ddb", true);
            Corpus cps = Corpus.CreateFromFile("LexemeEditServiceTestData4.db");

            // 処理本体
            //
            var service = new LexemeEditService();
            var corpora = new List<Corpus>() { cps };
            var refdics = new List<string>() { "LexemeEditServiceTestData5.ddb" };
            var userdic = "LexemeEditServiceTestData3.ddb";
            var list = service.ListLexemesNotInRefDic(corpora, refdics, null);
            if (list == null)
            {
                throw new Exception("Diff is null");
            }
            if (list.Count == 0)
            {
                throw new Exception("Diff count is 0");
            }
            foreach (var tuple in list)
            {
                tuple.Item3 = true;
            }
            service.UpdateCorpusInternalDictionaries(list);
            service.AddToUserDictionary(list, userdic);

            // 結果チェック
            // コーパスに含まれる語"C", "D"に対して、
            // "C"は参照辞書にあるので、ユーザー辞書には追加しない。
            // "D"は参照辞書にないので、ユーザー辞書に追加する。(仮定形と終止形の2つを追加)
            Assert.AreEqual(2, list.Count);
            // コーパス内部辞書のチェック ("D"は内部辞書には追加されないので、内部辞書のエントリ数は2のまま）
            var dbs = DBService.Create(cps.DBParam);
            var cfg = dbs.GetConnection();
            var factory = cfg.BuildSessionFactory();
            using (var session = factory.OpenSession())
            {
                var result = session.CreateQuery("from Lexeme").List<Lexeme>();
                Assert.AreEqual(4, result.Count);
            }
            // ユーザー辞書のチェック（ユーザー辞書は差分の"D"の仮定形と終止形が追加される）
            var dict = Dictionary.Create(userdic) as Dictionary_DB;
            var conn = DBService.Create(dict.DBParam).GetConnection();
            using (var session = conn.BuildSessionFactory().OpenSession())
            {
                var result = session.CreateQuery("from Lexeme").List<Lexeme>();
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("Ｄ", result[0].Surface);
                Assert.AreEqual("Ｄ", result[1].Surface);
            }
        }

        //
        // これ以降は、LexemeEditPanelからの操作に関するテスト
        // （単一の語の内容編集）
        //
        [TestMethod]
        public void Lexeme編集_非活用語から非活用語()
        {
            // 準備
            //
            // "C", "D" から成るコーパス
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData2.db", "LexemeEditServiceTestData2.db", true);
            Corpus cps = Corpus.CreateFromFile("LexemeEditServiceTestData2.db");
            // あらかじめ編集対象のLexemeを求めておく("C"とする)
            var dbs = DBService.Create(cps.DBParam);
            var cfg = dbs.GetConnection();
            var factory = cfg.BuildSessionFactory();
            Lexeme target = null;
            using (var session = factory.OpenSession())
            {
                target = session.CreateQuery("from Lexeme where Surface='Ｃ'").UniqueResult<Lexeme>();
            }

            // 処理本体
            //
            var service = new LexemeEditService();
            service.Open(cps, target, null);
            service.Save(new Dictionary<string, string>()
                {
                    {"Reading", "＿シー"},
                    {"Pronunciation", "＿シー"},
                    {"Lemma", "＿Ｃ"},
                    {"LemmaForm", "＿Ｃ"},
                    {"PartOfSpeech", "記号-一般"},
                });
            service.Close();

            // 結果チェック
            using (var session = factory.OpenSession())
            {
                var result = session.CreateQuery("from Lexeme where Surface='Ｃ'").List<Lexeme>();
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("Ｃ,＿シー,＿Ｃ,＿シー,Ｃ,＿Ｃ,記号-一般,,", result[0].ToString());
                var result2 = session.CreateQuery("from PartOfSpeech").List<PartOfSpeech>();
                Assert.AreEqual(2, result2.Count);  // "名詞-普通名詞-一般", "記号-一般"の2つ
                var result3 = session.CreateQuery("from CType").List<CType>();
                Assert.AreEqual(1, result3.Count);  // "" 1つ
                var result4 = session.CreateQuery("from CForm").List<CForm>();
                Assert.AreEqual(1, result4.Count);  // "" 1つ
            }
        }

        [TestMethod]
        public void Lexeme編集_POS変更あり()
        {
            // 準備
            //
            // "C", "D" から成るコーパス
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData2.db", "LexemeEditServiceTestData2.db", true);
            Corpus cps = Corpus.CreateFromFile("LexemeEditServiceTestData2.db");
            // あらかじめ編集対象のLexemeを求めておく("C"とする)
            var dbs = DBService.Create(cps.DBParam);
            var cfg = dbs.GetConnection();
            var factory = cfg.BuildSessionFactory();
            Lexeme target = null;
            using (var session = factory.OpenSession())
            {
                target = session.CreateQuery("from Lexeme where Surface='Ｃ'").UniqueResult<Lexeme>();
            }

            // 処理本体
            //
            var service = new LexemeEditService();
            service.Open(cps, target, null);
            service.Save(new Dictionary<string, string>()
                {
                    {"Reading", "＿シー"},
                    {"Pronunciation", "＿シー"},
                    {"Lemma", "＿Ｃ"},
                    {"LemmaForm", "＿Ｃ"},
                    {"PartOfSpeech", "動詞-一般"},
                    {"CType", "ダミー"},
                    {"CForm", "ダミー"},
                });
            service.Close();

            // 結果チェック
            using (var session = factory.OpenSession())
            {
                var result = session.CreateQuery("from Lexeme where Surface='Ｃ'").List<Lexeme>();
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("Ｃ,＿シー,＿Ｃ,＿シー,Ｃ,＿Ｃ,動詞-一般,ダミー,ダミー", result[0].ToString());
                Assert.AreEqual("Ｃ,＿シー,＿Ｃ,＿シー,Ｃ,＿Ｃ,動詞-一般,ダミー,基本形", result[1].ToString());
                var result2 = session.CreateQuery("from PartOfSpeech").List<PartOfSpeech>();
                Assert.AreEqual(2, result2.Count);
                var result3 = session.CreateQuery("from CType").List<CType>();
                Assert.AreEqual(2, result3.Count);  // "", "ダミー"の2つ
                var result4 = session.CreateQuery("from CForm").List<CForm>();
                Assert.AreEqual(3, result4.Count);  // "", "ダミー", "基本形"の3つ
            }
        }

        [TestMethod]
        public void Lexeme編集_活用語から非活用語へ()
        {
            // 準備
            //
            // "C", "D" から成るコーパス
            File.Copy(@"..\..\..\Test\ServiceTest\LexemeEditServiceTestData6.db", "LexemeEditServiceTestData6.db", true);
            Corpus cps = Corpus.CreateFromFile("LexemeEditServiceTestData6.db");
            // あらかじめ編集対象のLexemeを求めておく("D"とする)
            var dbs = DBService.Create(cps.DBParam);
            var cfg = dbs.GetConnection();
            var factory = cfg.BuildSessionFactory();
            Lexeme target = null;
            using (var session = factory.OpenSession())
            {
                target = session.CreateQuery("from Lexeme where Surface='Ｄ' and CForm.Name='連用形-一般'").UniqueResult<Lexeme>();
            }

            // 処理本体
            //
            var service = new LexemeEditService();
            service.Open(cps, target, null);
            service.Save(new Dictionary<string, string>()
                {
                    {"Reading", "＿ディー"},
                    {"Pronunciation", "＿ディー"},
                    {"Lemma", "＿Ｄ"},
                    {"LemmaForm", "＿Ｄ"},
                    {"PartOfSpeech", "名詞-普通名詞-一般"},
                    {"CType", ""},
                    {"CForm", ""},
                });
            service.Close();

            // 結果チェック
            using (var session = factory.OpenSession())
            {
                var result = session.CreateQuery("from Lexeme where Surface='Ｄ'").List<Lexeme>();
                Assert.AreEqual(3, result.Count);
                Assert.AreEqual("Ｄ,＿ディー,＿Ｄ,＿ディー,Ｄ,＿Ｄ,名詞-普通名詞-一般,,", result[2].ToString());
                Assert.AreEqual(3, result[2].BaseLexeme.ID);
                var result2 = session.CreateQuery("from PartOfSpeech").List<PartOfSpeech>();
                Assert.AreEqual(2, result2.Count);  // "名詞-普通名詞-一般", "動詞-一般"の2つ
                var result3 = session.CreateQuery("from CType").List<CType>();
                Assert.AreEqual(2, result3.Count);  // "", "五段-ワア行-一般"の2つ
                var result4 = session.CreateQuery("from CForm").List<CForm>();
                Assert.AreEqual(4, result4.Count);  // "", "基本形", "終止形-一般", "連用形-一般"の3つ
            }
        }

    }
}
