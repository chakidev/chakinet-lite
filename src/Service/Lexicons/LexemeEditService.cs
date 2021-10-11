using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using ChaKi.Common;
using NHibernate;
using NHibernate.Criterion;
using ChaKi.Service.Common;
using ChaKi.Service.DependencyEdit;

namespace ChaKi.Service.Lexicons
{
    public class LexemeEditService : ILexemeEditService
    {
        private Corpus m_Corpus;        // Lexemeの修正用にOpenするCorpus
        private OpContext m_Context;    // 修正操作のContext
        private Lexeme m_Target;        // 修正対象Lexeme

        private List<Corpus> m_Corpora;     //差分を取るためのCorpus(ReadOnly)
        private List<string> m_RefdicPaths; //差分を取るための参照辞書(ReadOnly)

        public LexemeEditService()
        {
            m_Context = null;
            m_Corpus = null;
            m_Target = null;
        }

        /// <summary>
        /// コーパスをupdateする処理について、開始前に排他チェックを行う.
        /// </summary>
        /// <param name="corpus"></param>
        /// <param name="lex"></param>
        /// <param name="callback"></param>
        public void Open(Corpus corpus, Lexeme lex, UnlockRequestCallback callback)
        {
            if (m_Context != null && m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("Active transaction exists. Close or Commit it first.");
            }

            m_Corpus = corpus;
            m_Corpora = new List<Corpus>() { m_Corpus };
            m_Target = lex;

            m_Context = OpContext.Create(m_Corpus.DBParam, callback, typeof(ILexemeEditService));
            m_Context.Session.Load(m_Target, m_Target.ID);
        }

        public void Close()
        {
            if (m_Context != null)
            {
                m_Context.Dispose();
                m_Context = null;
                m_Corpus = null;
            }
        }

        /// <summary>
        /// m_Corpus中のm_Lexemeの内容を修正する.
        /// m_Lexemeは必ず存在していること.
        /// Open/Close必要
        /// </summary>
        /// <param name="data"></param>
        public void Save(Dictionary<string, string> data)
        {
            if (m_Context == null || m_Target == null)
            {
                return;
            }

            var changed_list = new HashSet<string>();
            foreach (var pair in data)
            {
                if (pair.Key == Lexeme.PropertyName[LP.Reading])
                {
                    if (m_Target.ReplaceReading(pair.Value))
                    {
                        changed_list.Add(pair.Key);
                    }
                }
                else if (pair.Key == Lexeme.PropertyName[LP.Pronunciation])
                {
                    if (m_Target.ReplacePronunciation(pair.Value))
                    {
                        changed_list.Add(pair.Key);
                    }
                }
                else if (pair.Key == Lexeme.PropertyName[LP.LemmaForm])
                {
                    if (m_Target.ReplaceLemmaForm(pair.Value))
                    {
                        changed_list.Add(pair.Key);
                    }
                }
                else if (pair.Key == Lexeme.PropertyName[LP.Lemma])
                {
                    if (m_Target.ReplaceLemma(pair.Value))
                    {
                        changed_list.Add(pair.Key);
                    }
                }
                else if (pair.Key == Lexeme.PropertyName[LP.PartOfSpeech])
                {
                    if (m_Target.PartOfSpeech.Name != pair.Value)
                    {
                        var newval = FindPartOfSpeech(pair.Value, m_Context.Session);
                        if (newval == null)
                        {
                            newval = new PartOfSpeech(pair.Value);
                            m_Context.Session.Save(newval);
                        }
                        m_Target.PartOfSpeech = newval;
                        changed_list.Add(pair.Key);
                    }
                }
                else if (pair.Key == Lexeme.PropertyName[LP.CType])
                {
                    if (m_Target.CType.Name != pair.Value)
                    {
                        var newval = FindCType(pair.Value, m_Context.Session);
                        if (newval == null)
                        {
                            newval = new CType(pair.Value);
                            m_Context.Session.Save(newval);
                        }
                        m_Target.CType = newval;
                        changed_list.Add(pair.Key);
                    }
                }
                else if (pair.Key == Lexeme.PropertyName[LP.CForm])
                {
                    if (m_Target.CForm.Name != pair.Value)
                    {
                        var newval = FindCForm(pair.Value, m_Context.Session);
                        if (newval == null)
                        {
                            newval = new CForm(pair.Value);
                            m_Context.Session.Save(newval);
                        }
                        m_Target.CForm = newval;
                        changed_list.Add(pair.Key);
                    }
                }
                else if (pair.Key == "Custom")
                {
                    if (m_Target.ReplaceCustomProperty(pair.Value))
                    {
                        changed_list.Add(pair.Key);
                    }
                }
            }
            var cformIsBase = m_Target.CForm.IsBase();
            if (changed_list.Contains(Lexeme.PropertyName[LP.CForm]))
            {
                // CFormが変更された場合・・・
                if (m_Target.CForm.Name.Length == 0 || cformIsBase)
                {
                    // CFormが 空 or 基本形 に変更された場合、強制的にBase=thisとする。
                    // ex) ・元々活用語だった語を非活用語にした場合.
                    //     ・活用語を基本形以外から基本形に変更した場合.
                    m_Target.BaseLexeme = m_Target;
                }
                else if (m_Target.BaseLexeme == m_Target)
                {
                    // それ以外のCForm値に変更された場合、
                    // Baseを探してアサインする。なければ新しくBaseを作成する。
                    //  ex) ・元々非活用語でBase=thisだった語を活用語とし、基本形以外のCFormを指定した場合.
                    //      ・元々活用語の基本形だった語を、基本形以外のCFormに変更した場合.
                    var baseLex = FindOrCreateBase(m_Target, m_Context.Session);
                    m_Target.BaseLexeme = baseLex;
                    changed_list.Add("BaseLexeme");
                }
            }
            if (changed_list.Count > 0)
            {
                if (changed_list.Contains(Lexeme.PropertyName[LP.PartOfSpeech]))
                {
                    m_Context.Session.Update(m_Target.PartOfSpeech);
                }
                if (changed_list.Contains(Lexeme.PropertyName[LP.CType]))
                {
                    m_Context.Session.Update(m_Target.CType);
                }
                if (changed_list.Contains(Lexeme.PropertyName[LP.CForm]))
                {
                    m_Context.Session.Update(m_Target.CForm);
                }
                //if (changed_list.Contains("BaseLexeme"))
                //{
                //    m_Context.Session.Update(m_Target.BaseLexeme);
                //}
                m_Context.Session.Update(m_Target);
                m_Context.Session.Flush();
                Commit();
            }
        }

        private void Commit()
        {
            if (!m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("No active transaction exists.");
            }

            // コミットする
            m_Context.Trans.Commit();
            m_Context.Trans.Dispose();
            m_Context.Trans = null;

            // 操作記録をScriptの形で得てstaticメンバにセットする.
            //@todo

        }

        // m_Corpus中の一致するPartOfSpeechを検索する。なければnullを返す。
        private PartOfSpeech FindPartOfSpeech(string name, ISession session)
        {
            if (name == null) name = string.Empty;
            return session.CreateQuery(string.Format("from PartOfSpeech p where p.Name='{0}' order by p.ID asc", name))
                .List<PartOfSpeech>()
                .FirstOrDefault<PartOfSpeech>();
        }

        // m_Corpus中の一致するCFormを検索する。なければnullを返す。
        private CForm FindCForm(string name, ISession session)
        {
            if (name == null) name = string.Empty;
            return session.CreateQuery(string.Format("from CForm p where p.Name='{0}' order by p.ID asc", name))
                .List<CForm>()
                .FirstOrDefault<CForm>();
        }

        // m_Corpus中の一致するCTypeを検索する。なければnullを返す。
        private CType FindCType(string name, ISession session)
        {
            if (name == null) name = string.Empty;
            return session.CreateQuery(string.Format("from CType p where p.Name='{0}' order by p.ID asc", name))
                .List<CType>()
                .FirstOrDefault<CType>();
        }

        // m_Corpus中で、lexの持つCFormに対応する基本形Lexemeを検索する。なければ新たにTransientなLexemeを返す。
        private Lexeme FindOrCreateBase(Lexeme lex, ISession session)
        {
            var result = session.CreateQuery(
                string.Format("from Lexeme where Surface='{0}' and PartOfSpeech.Name='{1}' and CForm.Name='基本形'",
                    Util.EscapeQuote(lex.BaseLexeme.Surface), lex.PartOfSpeech.Name))
                .List<Lexeme>()
                .FirstOrDefault<Lexeme>();
            if (result != null)
            {
                return result;
            }
            result = session.CreateQuery(
                string.Format("from Lexeme where Surface='{0}' and PartOfSpeech.Name='{1}' and CForm.Name='終止形-一般'",
                    Util.EscapeQuote(lex.BaseLexeme.Surface), lex.PartOfSpeech.Name))
                .List<Lexeme>()
                .FirstOrDefault<Lexeme>();
            if (result != null)
            {
                return result;
            }
            var basecform = session.CreateQuery("from CForm where Name='基本形'").List<CForm>().FirstOrDefault<CForm>();
            if (basecform == null)
            {
                basecform = new CForm("基本形");
                session.Save(basecform);
            }

            var newbase = new Lexeme()
            {
                ID = -1,
                Surface = lex.BaseLexeme.Surface,
                Reading = lex.BaseLexeme.Reading,
                Pronunciation = lex.BaseLexeme.Pronunciation,
                Lemma = lex.BaseLexeme.Lemma,
                LemmaForm = lex.BaseLexeme.LemmaForm,
                PartOfSpeech = lex.PartOfSpeech,
                CType = lex.CType,
                CForm = basecform,
            };
            newbase.BaseLexeme = newbase;
            session.Save(newbase);
            return newbase;
        }

        /// <summary>
        /// corporaで与えられるコーパスの内部辞書の語のうち、
        /// どの参照辞書(refdicpaths)にも含まれないものをリストする.
        /// Open/Close不要
        /// </summary>
        /// <param name="corpora"></param>
        /// <param name="refdicpaths"></param>
        /// <returns></returns>
        public List<LexemeCorpusBoolLongTuple> ListLexemesNotInRefDic(List<Corpus> corpora, List<string> refdicpaths, IProgress progress)
        {
            m_Corpora = corpora;
            m_RefdicPaths = refdicpaths;

            var newlexemes = new List<LexemeCorpusBoolLongTuple>();
            var refsessions = new List<DictionaryAccessor>();
            try
            {
                foreach (var refdicpath in refdicpaths)
                {
                    var dict = Dictionary.Create(refdicpath);
                    var ctx = DictionaryAccessor.Create(dict);
                    refsessions.Add(ctx);
                }

                // Count lexemes in all corpus
                long total = 0;
                if (progress != null)
                {
                    foreach (var crps in corpora)
                    {
                        DBService dbs = DBService.Create(crps.DBParam);
                        NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
                        ISessionFactory factory = cfg.BuildSessionFactory();
                        using (ISession session = factory.OpenSession())
                        {
                            // ローカルLexiconのすべてのLexemeに対して、
                            var result = (long)session.CreateQuery("select count(*) from Lexeme").UniqueResult<long>();
                            total += result;
                        }
                    }
                    progress.ProgressMax = 100;
                    progress.ProgressCount = 0;
                }
                long count = 0;
                int last_percentage = -1;

                foreach (var crps in corpora)
                {
                    DBService dbs = DBService.Create(crps.DBParam);
                    NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
                    ISessionFactory factory = cfg.BuildSessionFactory();
                    using (ISession session = factory.OpenSession())
                    {
                        // ローカルLexiconのすべてのLexemeに対して、
                        var result = session.CreateQuery("from Lexeme").List<Lexeme>();
                        foreach (var lex in result)
                        {
                            var found = false; // そのLexemeがReferenceに存在しているか.
                            foreach (var refsession in refsessions)
                            {
                                if (IsLexemeExisting(lex, refsession))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                var freq = session.CreateSQLQuery(string.Format("SELECT COUNT(*) FROM word WHERE lexeme_id={0}", lex.ID))
                                    .UniqueResult<long>();
                                newlexemes.Add(new LexemeCorpusBoolLongTuple(lex, crps, false, freq));
                            }
                            count++;
                            if (progress != null)
                            {
                                var percentage = (int)(count * 100 / total);
                                progress.ProgressCount = percentage;
                                last_percentage = percentage;
                            }
                        }
                    }
                }
            }
            finally
            {
                foreach (var refdic in refsessions)
                {
                    refdic.Dispose();
                }
                if (progress != null)
                {
                    progress.EndWork();
                }
            }
            return newlexemes;
        }

        /// <summary>
        /// 参照用辞書を含め、全ての使用可能なPOS, CType, CFormタグのリストを得る.
        /// stringキーは辞書名（カレントコーパスについては"Default"という名称とする）.
        /// Open/Close不要.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="ctypes"></param>
        /// <param name="cforms"></param>
        public void GetLexiconTags(out Dictionary<string, IList<PartOfSpeech>> pos, out Dictionary<string, IList<CType>> ctypes, out Dictionary<string, IList<CForm>> cforms)
        {
            pos = new Dictionary<string, IList<PartOfSpeech>>();
            ctypes = new Dictionary<string, IList<CType>>();
            cforms = new Dictionary<string, IList<CForm>>();

            // コーパスのタグ
            foreach (var crps in m_Corpora)
            {
                DBService dbs = DBService.Create(crps.DBParam);
                NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
                ISessionFactory factory = cfg.BuildSessionFactory();
                using (ISession session = factory.OpenSession())
                {
                    pos.Add(crps.Name, GetPOSList(session));
                    ctypes.Add(crps.Name, GetCTypeList(session));
                    cforms.Add(crps.Name, GetCFormList(session));
                }
            }

            // 参照用辞書のタグ
            if (m_RefdicPaths != null)
            {
                foreach (var refdicpath in m_RefdicPaths)
                {
                    var dict = Dictionary.Create(refdicpath) as Dictionary_DB;  //@todo: DB Dictionaryのみに対応.
                    if (dict != null)
                    {
                        using (var session = DBService.Create(dict.DBParam).GetConnection().BuildSessionFactory().OpenSession())
                        {
                            pos.Add(dict.Name, GetPOSList(session));
                            ctypes.Add(dict.Name, GetCTypeList(session));
                            cforms.Add(dict.Name, GetCFormList(session));
                        }
                    }
                }
            }
        }

        private IList<PartOfSpeech> GetPOSList(ISession session)
        {
            return session.CreateQuery("from PartOfSpeech x order by x.Name asc").List<PartOfSpeech>();
        }
        private IList<CType> GetCTypeList(ISession session)
        {
            return session.CreateQuery("from CType x order by x.Name asc").List<CType>();
        }
        private IList<CForm> GetCFormList(ISession session)
        {
            return session.CreateQuery("from CForm x order by x.Name asc").List<CForm>();
        }


        /// <summary>
        /// listで与えられた語をuserdicpathで指定される辞書DBに追加する.
        /// 但し、既に一致する語が含まれている場合を除く.
        /// Open/Close不要
        /// </summary>
        /// <param name="list"></param>
        /// <param name="userdicpath"></param>
        public void AddToUserDictionary(List<LexemeCorpusBoolLongTuple> list, string userdicpath)
        {
            var dict = Dictionary.Create(userdicpath) as Dictionary_DB;
            if (dict == null)
            {
                throw new Exception("A non-database User Dictionary specified.");
            }
            var conn = DBService.Create(dict.DBParam).GetConnection();
            using (var session = conn.BuildSessionFactory().OpenSession())
            {
                foreach (var lextuple in list)
                {
                    using (var trans = session.BeginTransaction())
                    {
                        if (!lextuple.Item3)  // chekされていないlexemeは除く.
                        {
                            continue;
                        }
                        var lex = lextuple.Item1;
                        if (!IsLexemeExisting(lex, session))
                        {
                            AddLexeme(lex, session);
                        }
                        trans.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// LexemeをDBに登録する.
        /// Base, POS, CForm, CTypeも必要に応じて登録する.
        /// </summary>
        private void AddLexeme(Lexeme lex, ISession session)
        {
            // POS
            var pos = FindPartOfSpeech(lex.PartOfSpeech.Name, session);
            if (pos == null)
            {
                pos = new PartOfSpeech(lex.PartOfSpeech);
                session.Save(pos);
            }
            lex.PartOfSpeech = pos;
            // CType
            var ctype = FindCType(lex.CType.Name, session);
            if (ctype == null)
            {
                ctype = new CType(lex.CType);
                session.Save(ctype);
            }
            lex.CType = ctype;
            // CForm
            var cform = FindCForm(lex.CForm.Name, session);
            if (cform == null)
            {
                cform = new CForm(lex.CForm);
                session.Save(cform);
            }
            lex.CForm = cform;
            bool baseUpdated = false;

            if (lex.CForm.Name.Length == 0 || lex.CForm.IsBase())
            {
                // CFormが 空 or 基本形 に設定された場合、強制的にBase=thisとする。
                // ex) ・非活用語を追加.
                //     ・活用語(基本形)を追加.
                lex.BaseLexeme = lex;
            }
            else if (!lex.CForm.IsBase())
            {
                // それ以外のCForm値に設定された場合、
                // Baseを探してアサインする。なければ新しくBaseを作成する。
                //  ex) ・活用語(基本形以外)を追加.
                lex.BaseLexeme = FindOrCreateBase(lex, session);
                baseUpdated = true;
            }

            // Base
            if (!IsLexemeExisting(lex.BaseLexeme, session))
            {
                //session.Save(lex.BaseLexeme);
            }
            else if (baseUpdated)
            {
                // BaseのCFormを必要に応じてSave
                var basecform = FindCForm(lex.BaseLexeme.CForm.Name, session);
                if (basecform == null)
                {
                    basecform = new CForm(lex.CForm);
                    session.Save(basecform);
                }
                lex.BaseLexeme.CForm = basecform;
                //session.Update(lex.BaseLexeme);
            }

            // LexemeをSave
            session.Save(lex);
            session.Flush();
        }

        private bool IsLexemeExisting(Lexeme lex, ISession session)
        {
            var result = session.CreateCriteria<Lexeme>()
                .Add(Expression.Eq("Surface", lex.Surface))
                .List<Lexeme>();
            return IsLexemeExistingInList(lex, result);
        }

        private bool IsLexemeExisting(Lexeme lex, DictionaryAccessor ctx)
        {
            var result = ctx.FindLexemeBySurface(lex.Surface);
            return IsLexemeExistingInList(lex, result);
        }

        private bool IsLexemeExistingInList(Lexeme lex, IList<Lexeme> list)
        {
            foreach (var lex2 in list)
            {
                var lex3 = new Lexeme(lex2);
                // 比較元（内部辞書）のReading,Pronunciationが空なら比較対象から除外（内部辞書における仮の基本形を除外するため）
                if ((lex.Reading == null || lex.Reading.Length == 0) && (lex.Pronunciation == null || lex.Pronunciation.Length == 0))
                {
                    lex3.Reading = string.Empty;
                    lex3.Pronunciation = string.Empty;
                }
                if (lex.CForm.IsBase() && lex3.CForm.IsBase())
                {
                    // 基本形には複数の表記がある. 双方が基本形なら同一視する.
                    lex3.CForm = lex.CForm;
                }
                if (lex.ToString() == lex3.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// listで与えられたLexemeに基づき、コーパス内辞書を更新する.
        /// </summary>
        /// <param name="list"></param>
        public void UpdateCorpusInternalDictionaries(List<LexemeCorpusBoolLongTuple> list)
        {
            foreach (var item in list)
            {
                if (!item.Item3)
                {
                    continue;
                }
                try
                {
                    var updateData = item.Item1.GetPropertyAsDictionary();
                    Open(item.Item2, item.Item1, null);
                    // Reload Target Lexeme, because it may have modified fields.
                    m_Target = m_Context.Session.CreateQuery(string.Format("from Lexeme where ID={0}", item.Item1.ID)).UniqueResult<Lexeme>();
                    Save(updateData);
                }
                finally
                {
                    Close();
                }
            }
        }
    }
}
