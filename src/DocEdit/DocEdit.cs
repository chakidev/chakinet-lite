using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using ChaKi.GUICommon;
using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora.Annotations;
using System.Threading;
using System.IO;
using ChaKi.Common;

namespace ChaKi.DocEdit
{
    class DocEdit
    {
        private Corpus m_Corpus = null;
        private DBService m_Service = null;
        private ISession m_Session = null;
        private ITransaction m_Transaction = null;
        private Dictionary<int, string> m_SentenceTextList;
        private Dictionary<int, Dictionary<string, string>> m_SentenceAttributeList;
        private Dictionary<int, Dictionary<string, string>> m_DocumentAttributeList;
        private Thread m_BackgroundLoader;
        private Stack<int> m_BackGroundLoadRequests;
        private ISQLQuery m_WordListQuery;
        private ISQLQuery m_LexemeQuery;

        public event EventHandler IsDirtyChanged;
        private bool m_IsDirty;
        public bool IsDirty {
            get { return m_IsDirty; }
            private set
            {
                m_IsDirty = value;
                if (IsDirtyChanged != null)
                {
                    IsDirtyChanged(this, EventArgs.Empty);
                }
            }
        }

        public event Action<int> SentenceTextListUpdated;

        public List<int> m_ModifiedDocuments;

        public DocEdit()
        {
            m_SentenceTextList = new Dictionary<int, string>();
            m_SentenceAttributeList = new Dictionary<int, Dictionary<string, string>>();
            m_DocumentAttributeList = new Dictionary<int, Dictionary<string, string>>();
            m_BackGroundLoadRequests = new Stack<int>();
            m_BackgroundLoader = new Thread(BackgroundLoadThread) { IsBackground = true };
            m_BackgroundLoader.Start();
            IsDirty = false;
            m_ModifiedDocuments = new List<int>();
        }

        public void InvalidateCache()
        {
            m_SentenceTextList.Clear();
            m_SentenceAttributeList.Clear();
            m_DocumentAttributeList.Clear();
        }

        public int[] GetSentenceIdList()
        {
            var result = new int[0];
            try
            {
                //!SQL
                var q = m_Session.CreateSQLQuery("SELECT id FROM sentence ORDER BY id ASC");
                var r = q.List<int>();
                result = r.ToArray<int>();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                dlg.ShowDialog();
            }
            return result;
        }

        public Dictionary<int, string[]> GetSentenceList()
        {
            var result = new Dictionary<int, string[]>();
            try
            {
                //!SQL
                var q = m_Session.CreateSQLQuery("SELECT id, document_id FROM sentence ORDER BY id ASC");
                var qres = q.List<object[]>();
                foreach (var qrow in qres)
                {
                    int senid = (int)qrow[0];
                    result.Add(senid, new string[] { qrow[0].ToString(), string.Empty, qrow[1].ToString() });
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                dlg.ShowDialog();
            }
            return result;
        }

        private void BackgroundLoadThread()
        {
            while (true)
            {
                if (m_BackGroundLoadRequests.Count > 0)
                {
                    while (m_BackGroundLoadRequests.Count > 200)
                    {
                        m_BackGroundLoadRequests.Pop();
                    }
                    int id;
                    lock (m_BackGroundLoadRequests)
                    {
                        id = m_BackGroundLoadRequests.Pop();
                    }
                    if (!m_SentenceTextList.ContainsKey(id))
                    {
                        try
                        {
                            var text = GetSentenceTextSync(id);
                            lock (m_SentenceTextList)
                            {
                                m_SentenceTextList[id] = text;
                            }
                            if (this.SentenceTextListUpdated != null)
                            {
                                this.SentenceTextListUpdated(id);
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                            dlg.ShowDialog();
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        public string GetSentenceTextSync(int senid)
        {
            var maxlen = DocEditSettings.Instance.TextSnipLength;
            if (maxlen == 0)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            if (m_WordListQuery == null || m_LexemeQuery == null)
            {
                return string.Empty;
            }
            lock (m_Session)
            {
                //!SQL
                m_WordListQuery.SetInt32("senid", senid);
                var words = m_WordListQuery.List<object[]>();
                foreach (var word in words)
                {
                    var wid = (int)word[0];
                    var extra_char = word[1] as string;
                    //!SQL
                    m_LexemeQuery.SetInt32("wordid", wid);
                    var surface = m_LexemeQuery.UniqueResult<string>();
                    sb.Append(surface);
                    if (ContextPanelSettings.Current.UseSpacing)
                    {
                        sb.Append(" ");
                    }
                    if (extra_char != null)
                    {
                        sb.Append(extra_char);
                    }
                    if (maxlen >= 0 && sb.Length > maxlen) break;
                }
            }
            return sb.ToString();
        }

        public string GetSentenceTextAsync(int senid)
        {
            string result = string.Empty;
            lock (m_SentenceTextList)
            {

                if (m_SentenceTextList.TryGetValue(senid, out result))
                {
                    return result;
                }
            }
            lock (m_BackGroundLoadRequests)
            {
                m_BackGroundLoadRequests.Push(senid);
            }
            return string.Empty;
        }

        public void OpenCorpus(string corpusName)
        {
            if (m_Session != null)
            {
                CloseCorpus();
            }

            try
            {
                m_Corpus = Corpus.CreateFromFile(corpusName);
                m_Service = DBService.Create(m_Corpus.DBParam);
                m_Session = m_Service.OpenSession();
                m_Transaction = m_Session.BeginTransaction();
                m_WordListQuery = m_Session.CreateSQLQuery("SELECT w.lexeme_id, w.extra_chars FROM word w INNER JOIN sentence s ON w.sentence_id=s.id WHERE s.id=:senid ORDER BY w.position ASC");
                m_LexemeQuery = m_Session.CreateSQLQuery("SELECT surface FROM lexeme WHERE id=:wordid");
                m_ModifiedDocuments = new List<int>();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                dlg.ShowDialog();
            }
            IsDirty = false;
        }

        public void CloseCorpus()
        {
            if (m_Session != null)
            {
                if (m_Transaction != null)
                {
                    m_Transaction.Dispose();
                    m_Transaction = null;
                }
                m_Session.Close();
                m_Session = null;
            }
            m_SentenceTextList.Clear();
            m_SentenceAttributeList.Clear();
            m_DocumentAttributeList.Clear();
        }

        public void Commit(IProgress progress)
        {
            lock (m_Session)
            {
                if (m_Transaction != null)
                {
                    CleanupCorpus(progress);
                    m_Transaction.Commit();
                }
            }
            IsDirty = false;
        }

        public List<string[]> GetDocumentList()
        {
            var result = new List<string[]>();
            try
            {
                //!SQL
                var q = m_Session.CreateSQLQuery("SELECT document_id FROM document ORDER BY document_id ASC");
                var qresult = q.List<int>();
                foreach (var row in qresult)
                {
                    result.Add(new string[] { row.ToString() });
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                dlg.ShowDialog();
            }
            return result;
        }

        public bool AssignDocumentIdToSentences(List<int> senids, int newdocid)
        {
            bool changed = false;
            var senids_str = string.Join(",", (from e in senids select e.ToString()).ToArray<string>());
            IList<int> olddocids;
            lock (m_Session)
            {
                try
                {
                    //!SQL
                    var q = m_Session.CreateSQLQuery(string.Format("SELECT DISTINCT document_id FROM sentence WHERE id in ({0}) ORDER BY document_id", senids_str));
                    olddocids = q.List<int>();
                    if (olddocids.Count > 1 || olddocids[0] != newdocid)  // 変更する必要があるか確認.
                    {
                        // newdocidが存在しなければ新規Documentをinsert
                        q = m_Session.CreateSQLQuery(string.Format("SELECT count(*) FROM document WHERE document_id={0}", newdocid));
                        if (q.UniqueResult<Int64>() == 0)
                        {
                            q = m_Session.CreateSQLQuery("SELECT count(*) FROM document");
                            Int64 document_order = q.UniqueResult<Int64>();
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO document VALUES({0},{1},'','')", newdocid, document_order));
                            q.ExecuteUpdate();
                            changed = true;
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO document_set_document VALUES({0},{1})", newdocid, 0));
                            q.ExecuteUpdate();
                        }
                        q = m_Session.CreateSQLQuery(string.Format("UPDATE sentence SET document_id={0} WHERE id in ({1})", newdocid, senids_str));
                        q.ExecuteUpdate();
                        changed = true;
                        foreach (var id in olddocids)
                        {
                            if (!m_ModifiedDocuments.Contains(id))
                            {
                                m_ModifiedDocuments.Add(id);
                            }
                        }
                        if (!m_ModifiedDocuments.Contains(newdocid))
                        {
                            m_ModifiedDocuments.Add(newdocid);
                        }
                        m_ModifiedDocuments.Sort();
                    }
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                    dlg.ShowDialog();
                }
                finally
                {
                    this.IsDirty = this.IsDirty || changed;
                }
            }
            return changed;
        }


        public bool AssignDocumentIdToSentence(int senid, int newdocid)
        {
            bool changed = false;
            lock (m_Session)
            {
                try
                {
                    //!SQL
                    var q = m_Session.CreateSQLQuery(string.Format("SELECT DISTINCT document_id FROM sentence WHERE id={0}", senid));
                    var olddocid = q.UniqueResult<int>();
                    if (olddocid != newdocid)  // 変更する必要があるか確認.
                    {
                        // newdocidが存在しなければ新規Documentをinsert
                        q = m_Session.CreateSQLQuery(string.Format("SELECT count(*) FROM document WHERE document_id={0}", newdocid));
                        if (q.UniqueResult<Int64>() == 0)
                        {
                            q = m_Session.CreateSQLQuery("SELECT count(*) FROM document");
                            Int64 document_order = q.UniqueResult<Int64>();
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO document VALUES({0},{1},'','')", newdocid, document_order));
                            q.ExecuteUpdate();
                            changed = true;
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO document_set_document VALUES({0},{1})", newdocid, 0));
                            q.ExecuteUpdate();
                        }
                        q = m_Session.CreateSQLQuery(string.Format("UPDATE sentence SET document_id={0} WHERE id={1}", newdocid, senid));
                        q.ExecuteUpdate();
                        changed = true;
                        if (!m_ModifiedDocuments.Contains(olddocid))
                        {
                            m_ModifiedDocuments.Add(olddocid);
                        }
                        if (!m_ModifiedDocuments.Contains(newdocid))
                        {
                            m_ModifiedDocuments.Add(newdocid);
                        }
                        m_ModifiedDocuments.Sort();
                    }
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                    dlg.ShowDialog();
                }
                finally
                {
                    this.IsDirty = this.IsDirty || changed;
                }
            }
            return changed;
        }

        public IList<AttributeBase32> GetSentenceAttributes()
        {
            var result = new List<AttributeBase32>();
            lock (m_Session)
            {
                var q = m_Session.CreateSQLQuery("SELECT tag,description FROM documenttag WHERE document_id==-1 ORDER BY id");
                var qresult = q.List<object[]>();
                foreach (var item in qresult)
                {
                    result.Add(new SentenceAttribute() { Key = (string)item[0], Value = (string)item[1] });
                }
            }
            return result;
        }

        public Dictionary<string, string> GetSentenceAttribute(int senid)
        {
            Dictionary<string, string> attrs;
            if (!m_SentenceAttributeList.TryGetValue(senid, out attrs))
            {
                attrs = new Dictionary<string, string>();
                try
                {
                    lock (m_Session)
                    {
                        var q = m_Session.CreateSQLQuery(string.Format("SELECT t.tag,t.description FROM documenttag t INNER JOIN sentence_documenttag m ON t.id=m.documenttag_id WHERE m.sentence_id={0}", senid));
                        var qresult = q.List<object[]>();
                        foreach (var attr in qresult)
                        {
                            attrs.Add((string)attr[0], (string)attr[1]);
                        }
                        m_SentenceAttributeList[senid] = attrs;
                    }
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                    dlg.ShowDialog();
                    return new Dictionary<string, string>();
                }
            }
            return attrs;
        }

        public Dictionary<string, string> GetSentenceAttribute(IList<int> senids)
        {
            Dictionary<string, string> result = null;

            // すべてのsentenceに共通のSentenceAttributeを抽出する.(Intersection of SentenceAttributes)
            foreach (int senid in senids)
            {
                Dictionary<string, string> attrs = GetSentenceAttribute(senid);
                if (attrs.Count == 0)
                {
                    // sentenceAttributeが空のSentenceが含まれれば、結果は空.
                    return new Dictionary<string, string>();
                }
                if (result == null)
                {
                    result = attrs;
                    continue;
                }
                var newresult = new Dictionary<string, string>();
                foreach (var attr in attrs)
                {
                    if (result.ContainsKey(attr.Key))
                    {
                        if (result[attr.Key] == attr.Value)
                        {
                            newresult.Add(attr.Key, attr.Value);
                        }
                    }
                }
                result = newresult;
            }
            return result;
        }

        public Dictionary<string, string> GetDocumentAttribute(int docid)
        {
            var result = new Dictionary<string, string>();
            if (docid < 0 || m_DocumentAttributeList.TryGetValue(docid, out result))
            {
                return result;
            }
            result = new Dictionary<string, string>();
            try
            {
                lock (m_Session)
                {
                    var q = m_Session.CreateSQLQuery(string.Format("SELECT tag,description FROM documenttag WHERE document_id={0}", docid));
                    var qresult = q.List<object[]>();
                    foreach (var attr in qresult)
                    {
                        result.Add((string)attr[0], (string)attr[1]);
                    }
                    m_DocumentAttributeList[docid] = result;
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannnot execute operation", ex);
                dlg.ShowDialog();
            }
            return result;
        }

        public void UpdateAttributesForSentences(int docid, IList<int> senids, Dictionary<string, string> newSenAttrs, Dictionary<string, string> newDocAttrs)
        {
            if (docid < 0 || senids.Count == 0)
            {
                return;
            }
            // Document Attributeを比較し、完全に一致していれば何も変更しない.
            bool identical = true;
            {
                lock (m_Session)
                {

                    var q = m_Session.CreateSQLQuery(string.Format("SELECT tag,description FROM documenttag WHERE document_id={0}", docid));
                    var qresult = q.List<object[]>();
                    foreach (object[] pair in qresult)
                    {
                        var key = pair[0] as string;
                        var value = pair[1] as string;
                        if (!newDocAttrs.ContainsKey(key) || newDocAttrs[key] != value)
                        {
                            identical = false;
                            break;
                        }
                    }
                    foreach (var pair in newDocAttrs)
                    {
                        if (qresult.All<object[]>(x => ((string)x[0] != pair.Key))
                         || qresult.Any<object[]>(x => ((string)x[0] == pair.Key && (string)x[1] != pair.Value)))
                        {
                            identical = false;
                            break;
                        }
                    }
                }
                try
                {
                    //!SQL
                    ISQLQuery q;
                    if (!identical)
                    {
                        q = m_Session.CreateSQLQuery(string.Format("DELETE from documenttag WHERE document_id={0}", docid));
                        q.ExecuteUpdate();
                        foreach (var a in newDocAttrs)
                        {
                            var id = (int)GetNextAvailableID("documenttag");
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                                        id,
                                        a.Key,
                                        (a.Value == null) ? string.Empty : a.Value.Replace("'", "''"),
                                        docid,
                                        m_Service.GetDefault()));
                            q.ExecuteUpdate();
                        }
                        m_DocumentAttributeList.Remove(docid);
                    }

                    // Sentence Attribute
                    foreach (var senid in senids)
                    {
                        q = m_Session.CreateSQLQuery(string.Format("DELETE from sentence_documenttag WHERE sentence_id={0}", senid));
                        q.ExecuteUpdate();
                        foreach (var a in newSenAttrs)
                        {
                            q = m_Session.CreateSQLQuery(string.Format("SELECT id FROM documenttag WHERE tag='{0}' AND description='{1}' AND document_id=-1",
                                        a.Key,
                                        (a.Value == null) ? string.Empty : a.Value.Replace("'", "''")));
                            var result = q.List<int>();
                            int id;
                            if (result.Count > 0)
                            {
                                id = (int)result[0];
                            }
                            else
                            {
                                id = (int)GetNextAvailableID("documenttag");
                                q = m_Session.CreateSQLQuery(string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                                        id,
                                        a.Key,
                                        (a.Value == null) ? string.Empty : a.Value.Replace("'", "''"),
                                        -1,
                                        m_Service.GetDefault()));
                                q.ExecuteUpdate();
                            }
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO sentence_documenttag VALUES({0},{1})", senid, id));
                            q.ExecuteUpdate();
                        }
                        m_SentenceAttributeList.Remove(senid);
                    }
                }
                finally
                {
                    IsDirty = true;
                }
            }
        }

        public void UpdateAttributesForSentence(int docid, int senid, Dictionary<string, string> newSenAttrs, Dictionary<string, string> newDocAttrs)
        {
            if (docid < 0 || senid < 0)
            {
                return;
            }
            // Document Attributeを比較し、完全に一致していれば何も変更しない.
            bool identical = true;
            {
                if (newDocAttrs != null)
                {
                    lock (m_Session)
                    {

                        var q = m_Session.CreateSQLQuery(string.Format("SELECT tag,description FROM documenttag WHERE document_id={0}", docid));
                        var qresult = q.List<object[]>();
                        foreach (object[] pair in qresult)
                        {
                            var key = pair[0] as string;
                            var value = pair[1] as string;
                            if (!newDocAttrs.ContainsKey(key) || newDocAttrs[key] != value)
                            {
                                identical = false;
                                break;
                            }
                        }
                        foreach (var pair in newDocAttrs)
                        {
                            if (qresult.All<object[]>(x => ((string)x[0] != pair.Key))
                             || qresult.Any<object[]>(x => ((string)x[0] == pair.Key && (string)x[1] != pair.Value)))
                            {
                                identical = false;
                                break;
                            }
                        }
                    }
                }
                try
                {
                    //!SQL
                    ISQLQuery q;
                    if (!identical)
                    {
                        q = m_Session.CreateSQLQuery(string.Format("DELETE from documenttag WHERE document_id={0}", docid));
                        q.ExecuteUpdate();
                        foreach (var a in newDocAttrs)
                        {
                            var id = (int)GetNextAvailableID("documenttag");
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                                        id,
                                        a.Key,
                                        (a.Value == null) ? string.Empty : a.Value.Replace("'", "''"),
                                        docid,
                                        m_Service.GetDefault()));
                            q.ExecuteUpdate();
                        }
                        m_DocumentAttributeList.Remove(docid);
                    }

                    // Sentence Attribute
                    q = m_Session.CreateSQLQuery(string.Format("DELETE from sentence_documenttag WHERE sentence_id={0}", senid));
                    q.ExecuteUpdate();
                    foreach (var a in newSenAttrs)
                    {
                        q = m_Session.CreateSQLQuery(string.Format("SELECT id FROM documenttag WHERE tag='{0}' AND description='{1}' AND document_id=-1",
                                    a.Key,
                                    (a.Value == null) ? string.Empty : a.Value.Replace("'", "''")));
                        var result = q.List<int>();
                        int id;
                        if (result.Count > 0)
                        {
                            id = (int)result[0];
                        }
                        else
                        {
                            id = (int)GetNextAvailableID("documenttag");
                            q = m_Session.CreateSQLQuery(string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                                    id,
                                    a.Key,
                                    (a.Value == null) ? string.Empty : a.Value.Replace("'", "''"),
                                    -1,
                                    m_Service.GetDefault()));
                            q.ExecuteUpdate();
                        }
                        q = m_Session.CreateSQLQuery(string.Format("INSERT INTO sentence_documenttag VALUES({0},{1})", senid, id));
                        q.ExecuteUpdate();
                    }
                    m_SentenceAttributeList.Remove(senid);
                }
                finally
                {
                    IsDirty = true;
                }
            }
        }

        // ・Sentenceから参照されていないDocumentをdocument, documentset_document両テーブルから削除する.
        // ・m_ModifiedDocumentsに登録されているドキュメントについて、Textカラムを設定しなおす.
        // ・同Documentに属するSentenceについて、start_char, end_char, pos(SentenceNo in the Document)を振りなおす.
        // ・同Sentenceそれぞれに関連するSegmentについて、start_char, end_charをオフセットさせる.
        // ・同Sentenceそれぞれに関連するWordについて、start_char, end_charをオフセットさせる.
        public void CleanupCorpus(IProgress progress)
        {
            //!SQL
            var q = m_Session.CreateSQLQuery("DELETE FROM document WHERE document_id NOT IN (SELECT DISTINCT document_id FROM sentence)");
            q.ExecuteUpdate();
            q = m_Session.CreateSQLQuery("DELETE FROM document_set_document WHERE document_id NOT IN (SELECT DISTINCT document_id FROM document)");
            q.ExecuteUpdate();

            int start_char;
            int end_char;
            int pos;

            // 実行前に関与するSentenceの総数を求める.
            int n = 0;
            foreach (int id in m_ModifiedDocuments)
            {
                q = m_Session.CreateSQLQuery(string.Format("SELECT count(*) FROM sentence WHERE document_id={0} ORDER BY id ASC", id));
                n += (int)q.UniqueResult<Int64>();
            }
            progress.ProgressMax = n;
            n = 0;
            foreach (int id in m_ModifiedDocuments)
            {
                q = m_Session.CreateSQLQuery(string.Format("SELECT id FROM sentence WHERE document_id={0} ORDER BY id ASC", id));
                var senids = q.List<int>();
                var sb = new StringBuilder();
                start_char = 0;
                end_char = 0;
                pos = 0;
                foreach (var senid in senids)
                {
                    q = m_Session.CreateSQLQuery(string.Format("SELECT lexeme_id FROM word WHERE sentence_id={0} ORDER BY position ASC", senid));
                    var lexids = q.List<int>();
                    foreach (var lexid in lexids)
                    {
                        q = m_Session.CreateSQLQuery(string.Format("SELECT surface FROM lexeme WHERE id={0}", lexid));
                        string w = q.UniqueResult<string>();
                        end_char += w.Length;
                        w = w.Replace("'", "''");
                        sb.Append(w);
                    }
                    q = m_Session.CreateSQLQuery(string.Format("SELECT start_char FROM sentence WHERE id={0}", senid));
                    var charoffset = start_char - q.UniqueResult<int>();
                    q = m_Session.CreateSQLQuery(string.Format("UPDATE sentence SET start_char={0},end_char={1},pos={2} WHERE id={3}",
                        start_char, end_char, pos, senid));
                    q.ExecuteUpdate();
                    sb.Append("\n");
                    end_char++;
                    start_char = end_char;
                    pos++;
                    // Segmentのchar_offset調整
                    q = m_Session.CreateSQLQuery(string.Format("UPDATE segment SET start_char=start_char+({0}),end_char=end_char+({0}),document_id={1} WHERE sentence_id={2}",
                        charoffset, id, senid));
                    q.ExecuteUpdate();
                    q = m_Session.CreateSQLQuery(string.Format("UPDATE word SET start_char=start_char+({0}),end_char=end_char+({0}) WHERE sentence_id={1}",
                        charoffset, senid));
                    q.ExecuteUpdate();
                    n++;
                    progress.ProgressCount = n;
                }
                string text = sb.ToString();
                q = m_Session.CreateSQLQuery(string.Format("UPDATE document SET document_text='{0}' WHERE document_id={1}", text, id));
                q.ExecuteUpdate();
            }
        }

        private long GetNextAvailableID(string table)
        {
            return m_Session.CreateSQLQuery(string.Format("SELECT min(id)+1 FROM {0} WHERE id+1 NOT IN (SELECT id FROM {0})", table))
                    .UniqueResult<long>();
        }
    }
}
