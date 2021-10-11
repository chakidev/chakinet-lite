using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora;
using NHibernate;
using System.Diagnostics;
using System.Collections;
using ChaKi.Service.Database;
using ChaKi.Entity.Kwic;
using System.Data;

namespace ChaKi.Service.Search
{
    /// <summary>
    /// 複数のコーパスを縦断的に検索するサービスの基底となる抽象クラス。
    /// コーパスのオープンとセッション管理、および進捗表示の初期化を実装している。
    /// 
    /// 派生クラスでは、出力となるEntityを定義し、ExecuteSearchSessionが呼ばれたら
    /// そのコーパスを検索・Entityに結果を格納するコードを実装する。
    /// </summary>
    public abstract class SearchServiceBase : IServiceCommand
    {
        protected SearchHistory m_Hist;
        protected SearchHistory m_HistParent;
        protected CommandProgress m_Progress;
        protected SearchConditionsSequence m_CondSeq;
        protected ISession m_Session;
        protected ISessionFactory m_Factory;
        protected DBService m_DBService;

        public EventHandler Completed { get; set; }    // unused
        public EventHandler Aborted { get; set; }      // unused

        public SearchServiceBase(SearchHistory hist, SearchHistory parent)
        {
            m_Hist = hist;
            m_HistParent = parent;
            if (hist != null)
            {
                m_Progress = hist.Progress;
                m_CondSeq = hist.CondSeq;
            }
            m_Session = null;
            m_Factory = null;
            m_DBService = null;
        }

        protected abstract void ExecuteSearchSession(Corpus c);

        /// <summary>
        /// 複数コーパス検索の入口関数（派生クラスに共通の入口を与える）
        /// </summary>
        public void Begin()
        {
            if (m_Progress != null)
            {
                m_Progress.Reset();
            }
            foreach (Corpus c in m_CondSeq.Last.SentenceCond.Corpora)    //TODO: 絞り込みの場合、条件間でのCorpusの不一致はどうするか?
            {
                if (!c.Mutex.WaitOne(10, false))
                {
                    throw new Exception("SearchServiceBase: Could not lock the Corpus");
                }
                try
                {
                    // Corpus(DB)の種類に合わせてConfigurationをセットアップする
                    m_DBService = DBService.Create(c.DBParam);
                    NHibernate.Cfg.Configuration cfg = m_DBService.GetConnection();
                    using (m_Factory = cfg.BuildSessionFactory())
                    {
                        try
                        {
                            m_Session = m_Factory.OpenSession();
                            m_Session.FlushMode = FlushMode.Never;
                            if (m_Progress != null)
                            {
                                // Nc, Ndを取得
                                IQuery query = m_Session.CreateQuery("select count(*) from Word");
                                long nc = (long)query.UniqueResult();
                                query = m_Session.CreateQuery("select count(*) from Lexeme");
                                long nd = (long)query.UniqueResult();
                                c.NWords = nc;
                                c.NLexemes = nd;
                                m_Progress.StartOnItem(c.Name, (int)nc, (int)nd, 0);
                            }
                            ExecuteSearchSession(c);
                        }
                        finally
                        {
                            m_Session.Close();
                        }
                    }
                }
                finally
                {
                    c.Mutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// 検索条件Boxで指定された各Lexemeの属性からLexicon検索を行い、
        /// マッチするLexemeをすべて得る。
        /// 結果を int(Box番号) -> IList(Lexemeリスト) のマップに格納して返す。
        /// </summary>
        protected LexemeResultSet QueryLexemeResultSet(IList<LexemeCondition> lexConds, bool retrieveWildcardMatch)
        {
            Debug.Assert(m_Session != null);

            LexemeResultSet resultset = new LexemeResultSet();
            //   条件内のLexeme属性をすべて制約として追加する。
            for (int i = 0; i < lexConds.Count; i++)
            {
                LexemeCondition lexcond = lexConds[i];
                string qstr = QueryBuilder.Instance.BuildLexemeQuerySQL(lexcond);
                IList<Lexeme> lexemeResult = null;
                if (qstr.Length > 0)
                {
                    ISQLQuery query = m_Session.CreateSQLQuery(qstr).AddEntity(typeof(Lexeme));
                    lexemeResult = query.List<Lexeme>();
                    if (lexemeResult.Count == 0)
                    {
                        // 条件Box中にhitしないものがひとつでもあれば、結果は0
                        resultset.Clear();
                        return resultset;
                    }
                }
                resultset.Add(i, lexcond, lexemeResult);
            }
            return resultset;
        }

        /// <summary>
        /// DependencySearch用のLexeme検索
        /// </summary>
        /// <param name="dcond"></param>
        /// <returns></returns>
        protected LexemeResultSet QueryBunsetsuLexemeResultSet(DepSearchCondition dcond)
        {
            LexemeResultSet res = QueryLexemeResultSet(dcond.GetLexemeCondList(), false);
            if (res.Count == 0)
            {
                return res;
            }
            // 一列に並べられたLexeme検索結果それぞれに対して、その所属する文節Noをセットする。
            int segno = 0;
            int lexno = 0;
            foreach (TagSearchCondition tcond in dcond.BunsetsuConds)
            {
                foreach (LexemeCondition lcond in tcond.LexemeConds)
                {
                    if (lexno > res.Count)
                    {
                        throw new Exception(string.Format("Too few LexemeResultSet elements. Count={0}", res.Count));
                    }
                    res.GetResult(lexno).BunsetsuNo = segno;
                    lexno++;
                }
                segno++;
            }

            return res;
        }

        /// <summary>
        /// docIDを元にDocumentの全文をstringとして得る。（メモリを大量消費することがあるので注意）
        /// 得られたstringはDocument.Textにsetしてもよいし、一時的に使うだけでもよい。
        /// </summary>
        /// <param name="docID"></param>
        /// <returns></returns>
        protected string QueryDocumentText(int docID)
        {
            IDbConnection conn = m_Session.Connection;
            IDbCommand cmd = conn.CreateCommand();
            //!SQL
            cmd.CommandText = string.Format("SELECT document_text FROM document WHERE document_id={0}", docID);
            string result = (string)cmd.ExecuteScalar();
            return result;
        }

        /// <summary>
        /// 与えられたidのドキュメントの全文よりcharPos (0-base), Lengthを指定して
        /// 部分文字列を得る。
        /// MySQL, SQLiteのsubstr関数を使用しているので、全文をロードすることはないが、
        /// ローカルに全文を取得してstring.SubString()を行うよりは当然遅くなる。
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="sPos"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected string QuerySubstringFromDocText(Document doc, int sPos, int length)
        {
            if (doc.Text == null || doc.Text.Length == 0)
            {
                IDbConnection conn = m_Session.Connection;
                IDbCommand cmd = conn.CreateCommand();
                //!SQL
                cmd.CommandText = string.Format("SELECT substr(document_text,{0},{1}) FROM document WHERE document_id={2}", sPos + 1, length, doc.ID);
                string result = (string)cmd.ExecuteScalar();
                return result;
            }
            else
            {
                string result = string.Empty;
                try
                {
                    result = doc.Text.Substring(sPos, length);
                }
                catch (Exception)
                {
                    Console.WriteLine("QuerySubstringFromDocText: logic error - sPos={0}, length={1}", sPos, length); 
                }
                return result;
            }
        }


        protected void QueryWordMappings(Word word)
        {
            var sq = m_Session.CreateSQLQuery(string.Format("SELECT DISTINCT to_word from word_word where from_word={0}", word.ID));
            word.MappedTo.AddAll(sq.List<int>());
        }
    }
}
