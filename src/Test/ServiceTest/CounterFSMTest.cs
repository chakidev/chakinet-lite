using System.Collections.Generic;
using ChaKi.Entity.Collocation;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using ChaKi.Service.Collocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ChaKi.ServiceTest
{
    class FSMTestResult : IComparable<FSMTestResult>
    {
        public string Pattern;
        public int Frequency;
        public string IDs;


        public int CompareTo(FSMTestResult other)
        {
            return this.Pattern.CompareTo(other.Pattern);
        }
    }

    /// <summary>
    ///CounterFSMTest のテスト クラスです。すべての
    ///CounterFSMTest 単体テストをここに含めます
    ///</summary>
    [TestClass()]
    public class CounterFSMTest
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
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //各テストを実行した後にコードを実行するには、TestCleanup を使用
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        ///fsmtest01 -----------------------------------------------------------------
        ///1: A B s C D
        ///2: E A B C D F
        ///3: A B s G H
        ///MinFreq=1, MinLen=1, NumOfGaps=0 Stopwords=s
        ///正解
        ///[ E A B C D F ]
        ///[ G H ]
        ///
        ///コメント
        ///最初の文からは[ A B ], [ C D ]の系列が得られるが、どちらも[ E A B C D F ]に含まれる.
        ///※連続系列はStopwordで区切られる
        /// 最大系列のみなので、Freq=1だと短い系列はすべてより長い系列に含まれる
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test01()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'A', 'B', 's', 'C', 'D' },
                 new char[] { 'E', 'A', 'B', 'C', 'D', 'F' },
                 new char[] { 'A', 'B', 's', 'G', 'H' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = false,
                MinFrequency = 1,
                MinLength = 1,
                MaxGapCount = 0,
                MaxGapLen = 0,
                Filter = new LexemeFilter(),
                Stopwords = new string[] { "s" }
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(2, sresults.Count);
            Assert.AreEqual<string>("[ E A B C D F ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ G H ]", sresults[1].Pattern);
            Assert.AreEqual<string>("1", sresults[0].IDs);
            Assert.AreEqual<string>("2", sresults[1].IDs);
        }

        /// <summary>
        ///fsmtest01_1 -----------------------------------------------------------------
        ///1: A B s C D
        ///2: E A B C D F
        ///3: A B s G H
        ///MinFreq=2, MinLen=1, NumOfGaps=0 Stopwords=s
        ///正解
        ///[ A B ] Freq=3
        ///[ C D ] Freq=2
        ///
        ///コメント
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test01_1()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'A', 'B', 's', 'C', 'D' },
                 new char[] { 'E', 'A', 'B', 'C', 'D', 'F' },
                 new char[] { 'A', 'B', 's', 'G', 'H' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = false,
                MinFrequency = 2,
                MinLength = 1,
                MaxGapCount = 0,
                MaxGapLen = 0,
                Filter = new LexemeFilter(),
                Stopwords = new string[] { "s" }
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(2, sresults.Count);
            Assert.AreEqual<string>("[ A B ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ C D ]", sresults[1].Pattern);
            Assert.AreEqual<string>("0,1,2", sresults[0].IDs);
            Assert.AreEqual<string>("0,1", sresults[1].IDs);
        }

        /// <summary>
        ///fsmtest02-----------------------------------------------------------------
        ///a x b c
        ///a y b c
        ///a z c
        ///条件MinFreq=2, MaxNumOfGap=1
        ///
        ///正解
        ///[ a ] [ b c ] Freq=2
        ///[ b c ]
        ///
        ///コメント
        ///[ a ] [ b c ]
        ///[ a ] [ c ]    --> cの直前をbで拡張可能なので削除.
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test02()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'x', 'b', 'c' },
                 new char[] { 'a', 'y', 'b', 'c' },
                 new char[] { 'a', 'z', 'c' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = false,
                MinFrequency = 2,
                MinLength = 1,
                MaxGapCount = 1,
                MaxGapLen = -1,
                Filter = new LexemeFilter(),
                Stopwords = new string[] { "s" }
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(2, sresults.Count);
            Assert.AreEqual<string>("[ a ] [ b c ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ b c ]", sresults[1].Pattern);
            Assert.AreEqual<string>("0,1", sresults[0].IDs);
            Assert.AreEqual<string>("0,1", sresults[1].IDs);
        }

        /// <summary>
        ///fsmtest03-----------------------------------------------------------------
        ///a b x c
        ///a b y c
        ///a z c
        ///条件MinFreq=2, MaxNumOfGap=1
        ///
        ///正解
        ///[ a b ] [ c ] Freq=2
        ///[ c ]
        ///
        ///コメント
        ///[ a b ] [ c ]
        ///[ a ] [ c ]    --> aの直後をbで拡張可能なので削除.
        ///[ b ] [ c ]    --> bの直前はaが2回なので削除
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test03()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'b', 'x', 'c' },
                 new char[] { 'a', 'b', 'y', 'c' },
                 new char[] { 'a', 'z', 'c' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = false,
                MinFrequency = 2,
                MinLength = 1,
                MaxGapCount = 1,
                MaxGapLen = -1,
                Filter = new LexemeFilter(),
                Stopwords = new string[] { "s" }
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(2, sresults.Count);
            Assert.AreEqual<string>("[ a b ] [ c ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ c ]", sresults[1].Pattern);
            Assert.AreEqual<string>("0,1", sresults[0].IDs);
            Assert.AreEqual<string>("0,1,2", sresults[1].IDs);
        }

        /// <summary>
        ///fsmtest04-----------------------------------------------------------------
        ///1: a x b
        ///2: a y c b
        ///3: a z c b
        ///条件MinFreq=2, MaxNumOfGap=1
        ///
        ///正解
        ///[ a ] [ c b ]   Freq=2
        ///[ c b ]
        ///
        ///コメント
        ///[ a ] [ b ]    --> bの直前をcで拡張可能なので削除.
        ///[ a ] [ c b ]  --> cの直前はyが1回、zが1回なので残る.
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test04()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'x', 'b' },
                 new char[] { 'a', 'y', 'c', 'b' },
                 new char[] { 'a', 'z', 'c', 'b' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = false,
                MinFrequency = 2,
                MaxGapCount = 1,
                Filter = new LexemeFilter(),
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(2, sresults.Count);
            Assert.AreEqual<string>("[ a ] [ c b ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ c b ]", sresults[1].Pattern);
            Assert.AreEqual<string>("1,2", sresults[0].IDs);
            Assert.AreEqual<string>("1,2", sresults[1].IDs);
        }


        /// <summary>
        ///fsmtest05-----------------------------------------------------------------
        ///1: a i b e u x c d
        ///2: a j b e v x y c d
        ///3: a k b e w x z c d
        ///条件MinFreq=2, MaxNumOfGap=3
        ///
        ///正解
        ///[ a ] [ b e ] [ c d ]
        ///[ a ] [ b e ] [ x ] [ c d ]
        ///[ a ] [ c d ]
        ///[ a ] [ x ] [ c d ]
        ///[ b e ] [ c d ]
        ///[ b e ] [ x ] [ c d ]
        ///[ c d ]
        ///[ x ] [ c d ]
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test05()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'i', 'b', 'e', 'u', 'x', 'c', 'd' },
                 new char[] { 'a', 'j', 'b', 'e', 'v', 'x', 'y', 'c', 'd' },
                 new char[] { 'a', 'k', 'b', 'e', 'w', 'x', 'z', 'c', 'd' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = false,
                MinFrequency = 2,
                MaxGapCount = 3,
                Filter = new LexemeFilter(),
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(8, sresults.Count);
            Assert.AreEqual<string>("[ a ] [ b e ] [ c d ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ a ] [ b e ] [ x ] [ c d ]", sresults[1].Pattern);
            Assert.AreEqual<string>("[ a ] [ c d ]", sresults[2].Pattern);
            Assert.AreEqual<string>("[ a ] [ x ] [ c d ]", sresults[3].Pattern);
            Assert.AreEqual<string>("[ b e ] [ c d ]", sresults[4].Pattern);
            Assert.AreEqual<string>("[ b e ] [ x ] [ c d ]", sresults[5].Pattern);
            Assert.AreEqual<string>("[ c d ]", sresults[6].Pattern);
            Assert.AreEqual<string>("[ x ] [ c d ]", sresults[7].Pattern);
            Assert.AreEqual<string>("0,1,2", sresults[0].IDs);
            Assert.AreEqual<string>("1,2", sresults[1].IDs);
            Assert.AreEqual<string>("0,1,2", sresults[2].IDs);
            Assert.AreEqual<string>("1,2", sresults[3].IDs);
            Assert.AreEqual<string>("0,1,2", sresults[4].IDs);
            Assert.AreEqual<string>("1,2", sresults[5].IDs);
            Assert.AreEqual<string>("0,1,2", sresults[6].IDs);
            Assert.AreEqual<string>("1,2", sresults[7].IDs);
        }

        /// <summary>
        ///fsmtest05_1-----------------------------------------------------------------
        ///1: a i b e u x c d
        ///2: a j b e v x y c d
        ///3: a k b e w x z c d
        ///条件MinFreq=2, NumOfGap=3(Exact)
        ///
        ///正解
        ///[ a ] [ b e ] [ x ] [ c d ]
        ///※前テストからgap=3のみを抽出したものとなる
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test05_1()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'i', 'b', 'e', 'u', 'x', 'c', 'd' },
                 new char[] { 'a', 'j', 'b', 'e', 'v', 'x', 'y', 'c', 'd' },
                 new char[] { 'a', 'k', 'b', 'e', 'w', 'x', 'z', 'c', 'd' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = true,
                MinFrequency = 2,
                MaxGapCount = 3,
                Filter = new LexemeFilter(),
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(1, sresults.Count);
            Assert.AreEqual<string>("[ a ] [ b e ] [ x ] [ c d ]", sresults[0].Pattern);
            Assert.AreEqual<string>("1,2", sresults[0].IDs);
        }

        /// <summary>
        ///fsmtest06-----------------------------------------------------------------
        ///a x b
        ///a x b
        ///a y b
        ///
        ///Freq=2, NumOfGaps=1(Exact)
        ///
        ///[ a ] [ b ] 3
        ///
        ///コメント
        ///[ a x b ]が排除されること
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test06()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'x', 'b' },
                 new char[] { 'a', 'x', 'b' },
                 new char[] { 'a', 'y', 'b' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = true,
                MinFrequency = 2,
                MaxGapCount = 1,
                Filter = new LexemeFilter(),
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(1, sresults.Count);
            Assert.AreEqual<string>("[ a ] [ b ]", sresults[0].Pattern);
            Assert.AreEqual<string>("0,1,2", sresults[0].IDs);
        }

        /// <summary>
        ///fsmtest07-----------------------------------------------------------------
        ///a x y b
        ///a x y b
        ///
        ///Freq=2, NumOfGaps=1(Exact)
        ///
        ///[ a ] [ y b ]
        ///[ a x ] [ b ]
        ///
        ///コメント
        ///[ a ] [ b ] 拡張可能なので削除される
        ///
        ///※aからProjectionする際の後続候補について：
        /// x 
        /// !y  --> gap長が1なので削除しない
        /// !b  --> gap長が2なので削除
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test07()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'x', 'y', 'b' },
                 new char[] { 'a', 'x', 'y', 'b' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                ExactGC = true,
                MinFrequency = 2,
                MaxGapCount = 1,
                Filter = new LexemeFilter(),
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(2, sresults.Count);
            Assert.AreEqual<string>("[ a ] [ y b ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ a x ] [ b ]", sresults[1].Pattern);
            Assert.AreEqual<string>("0,1", sresults[0].IDs);
            Assert.AreEqual<string>("0,1", sresults[1].IDs);
        }

        /// <summary>
        ///fsmtest07_1-----------------------------------------------------------------
        ///a x y b
        ///a x y b
        ///
        ///Freq=2, MaxNumOfGaps=1
        ///        
        ///[ a ] [ y b ]
        ///[ a x ] [ b ]
        ///[ a x y b ]
        ///
        ///コメント
        ///※aからProjectionする際の後続候補について：
        /// x 
        /// !y  --> gap長が1なので削除しない
        /// !b  --> gap長が2なので削除
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test07_1()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'x', 'y', 'b' },
                 new char[] { 'a', 'x', 'y', 'b' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                MinFrequency = 2,
                MaxGapCount = 1,
                Filter = new LexemeFilter(),
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(3, sresults.Count);
            Assert.AreEqual<string>("[ a ] [ y b ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ a x ] [ b ]", sresults[1].Pattern);
            Assert.AreEqual<string>("[ a x y b ]", sresults[2].Pattern);
            Assert.AreEqual<string>("0,1", sresults[0].IDs);
            Assert.AreEqual<string>("0,1", sresults[1].IDs);
            Assert.AreEqual<string>("0,1", sresults[2].IDs);
        }

        /// <summary>
        ///fsmtest08-----------------------------------------------------------------
        /// 総合的なテスト
        /// ---------------------------------------------------------------------------
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ChaKiService.dll")]
        public void Test08()
        {
            KwicList src = MakeKwicList(new List<char[]> {
                 new char[] { 'a', 'x', 'c', 'd' },
                 new char[] { 'a', 'y', 'c', 'd', 'g', 'f' },
                 new char[] { 'a', 'u', 'v', 'd', 'w', 'e' },
                 new char[] { 'a', 'r', 's', 'd', 't', 'e' },
             });

            CollocationCondition cond = new CollocationCondition()
            {
                CollType = CollocationType.FSM,
                MinFrequency = 2,
                MinLength = 3,
                Filter = new LexemeFilter(),
            };
            cond.Filter.SetFiltered(LP.Reading);
            cond.Filter.SetFiltered(LP.LemmaForm);
            cond.Filter.SetFiltered(LP.Pronunciation);
            cond.Filter.SetFiltered(LP.BaseLexeme);
            cond.Filter.SetFiltered(LP.Lemma);
            cond.Filter.SetFiltered(LP.PartOfSpeech);
            cond.Filter.SetFiltered(LP.CForm);
            cond.Filter.SetFiltered(LP.CType);
            CollocationList result = new CollocationList(cond);
            CounterFSM target = new CounterFSM(src, result, cond);

            target.Count();
            List<FSMTestResult> sresults;
            ToFSMTestResult(result.Rows, out sresults);

            Assert.AreEqual<int>(2, sresults.Count);
            Assert.AreEqual<int>(2, sresults[0].Frequency);
            Assert.AreEqual<int>(3, sresults[1].Frequency);
            Assert.AreEqual<string>("[ a ] [ c d ]", sresults[0].Pattern);
            Assert.AreEqual<string>("[ a ] [ d ] [ e ]", sresults[1].Pattern);
            Assert.AreEqual<string>("0,1", sresults[0].IDs);
            Assert.AreEqual<string>("0,2,3", sresults[1].IDs);
        }

        private Dictionary<char, Lexeme> MyLexicon = new Dictionary<char, Lexeme>();

        private Lexeme FindLexeme(char c)
        {
            Lexeme lex;
            if (!MyLexicon.TryGetValue(c, out lex))
            {
                lex = new Lexeme() { Surface = new string(c, 1) };
                MyLexicon.Add(c, lex);
            }
            return lex;
        }

        private KwicList MakeKwicList(List<char[]> strings)
        {
            KwicList lst = new KwicList();
            int senpos = 0;
            foreach (char[] symbol_array in strings)
            {
                KwicItem ki = new KwicItem() { SenID = senpos++ };
                foreach (char c in symbol_array)
                {
                    Lexeme lex = FindLexeme(c);
                    ki.Right.AddLexeme(lex, null, 0);
                }
                lst.AddKwicItem(ki);
            }
            return lst;
        }

        private void ToFSMTestResult(Dictionary<Lexeme, List<DIValue>> result, out List<FSMTestResult> patterns)
        {
            patterns = new List<FSMTestResult>();
            foreach (KeyValuePair<Lexeme, List<DIValue>> pairs in result)
            {
                FSMTestResult item = new FSMTestResult() {
                    Pattern = pairs.Key.Surface,
                    Frequency = pairs.Value[0].ival,
                    IDs = pairs.Value[3].sval };
                patterns.Add(item);
            }
            patterns.Sort();
        }
    }
}
