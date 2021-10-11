using System;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using NHibernate.Criterion;
using System.Collections.Generic;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Common;
using System.Data;
using System.Text;

namespace ChaKi.Service.SentenceEdit
{
    public class SentenceEditService : IDisposable, ISentenceEditService
    {
        protected Corpus m_Corpus;
        private OpContext m_Context;

        public SentenceEditService()
        {
            m_Corpus = null;
            m_Context = null;
        }

        public void ChangeBoundaries(IList<Sentence> current, IList<int> targetLineLength)
        {
            if (current.Count == 0) throw new Exception("Current List has no Sentence.");
            int startChar = current[0].StartChar;
            int endChar = current[current.Count - 1].EndChar;
            int docid = current[0].ParentDoc.ID;

            bool identical = false;
            while (!identical)
            {
                identical = true;
                for (int i = 0; i < targetLineLength.Count; i++)
                {
                    if (i >= current.Count)
                    {
                        StringBuilder sb = new StringBuilder();
                        #region ExceptionFormatting
                        sb.AppendFormat("Count mismatch at {0}.", i);
                        sb.AppendLine();
                        sb.AppendLine(" Current sentence list:");
                        for (int j = 0; j < current.Count; j++)
                        {
                            string s = current[j].GetText(false);
                            sb.AppendFormat("  {0}: {1} (len={2})", j, s, s.Length);
                            sb.AppendLine();
                        }
                        sb.AppendLine(" Target length list:");
                        for (int j = 0; j < targetLineLength.Count; j++)
                        {
                            sb.AppendFormat("  {0}: {1}", j, targetLineLength[j]);
                            sb.AppendLine();
                        }
                        #endregion
                        throw new Exception(sb.ToString());
                    }
                    int curLength = current[i].EndChar - current[i].StartChar;
                    if (targetLineLength[i] < curLength)
                    {
                        Sentence newsen = SplitSentence(current[i].ID, targetLineLength[i]);
                        // Modelを更新
                        ReloadSentences(ref current, docid, startChar, endChar);
                        identical = false;
                        break;
                    }
                    else if (targetLineLength[i] > curLength)
                    {
                        if (i >= current.Count - 1)
                        {
                            StringBuilder sb = new StringBuilder();
                            #region ExceptionFormatting
                            sb.AppendFormat("Length mismatch at the last sentence ({0}).", i);
                            sb.AppendLine();
                            sb.AppendLine(" Current sentence list:");
                            for (int j = 0; j < current.Count; j++)
                            {
                                string s = current[j].GetText(false);
                                sb.AppendFormat("  {0}: {1} (len={2})", j, s, current[j].EndChar-current[j].StartChar);
                                sb.AppendLine();
                            }
                            sb.AppendLine(" Target length list:");
                            for (int j = 0; j < targetLineLength.Count; j++)
                            {
                                sb.AppendFormat("  {0}: {1}", j, targetLineLength[j]);
                                sb.AppendLine();
                            }
                            #endregion
                            throw new Exception(sb.ToString());
                        }
                        MergeSentence(current[i].ID);
                        // Modelを更新
                        ReloadSentences(ref current, docid, startChar, endChar);
                        identical = false;
                        break;
                    }
                }
            }
        }

        // 指定文字位置範囲のSentenceリストをDBから再取得
        internal void ReloadSentences(ref IList<Sentence> list, int docid, int startChar, int endChar)
        {
            IQuery q = m_Context.Session.CreateQuery(string.Format("from Sentence where ParentDoc.ID={0} and StartChar>={1} and EndChar<={2} order by StartChar asc", docid, startChar, endChar));
            list = q.List<Sentence>();
            foreach (Sentence s in list)
            {
                m_Context.Session.Refresh(s);
            }
        }

        public Sentence SplitSentence(int sid, int splitPos)
        {
            if (m_Corpus == null || m_Context == null)
            {
                throw new Exception("Corpus or Context is null.");
            }
            IQuery q =  m_Context.Session.CreateQuery(string.Format("from Sentence where ID={0}", sid));
            Sentence sen = q.UniqueResult<Sentence>();
            m_Context.Session.Refresh(sen);
            string strTxt = sen.GetText(false);

            // (1) sidの次に(sid+1)の新しいSentenceを挿入
            //@SQL
            int senpos = sen.Pos;
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence SET end_char={0} WHERE id={1}", sen.StartChar+splitPos, sen.ID));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence SET pos=(pos+1) WHERE id>{0} AND document_id={1}", sid, sen.ParentDoc.ID));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence SET id=-(id+1) WHERE id>{0}", sid));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("INSERT INTO sentence VALUES({0},{1},{2},{3},{4})",
                sid + 1, sen.StartChar + splitPos, sen.EndChar, sen.ParentDoc.ID, senpos + 1));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery("UPDATE sentence SET id=-id WHERE id<0");
            q.ExecuteUpdate();
            Sentence sen2 =  m_Context.Session.CreateQuery(string.Format("from Sentence where ID={0}", sid+1)).UniqueResult<Sentence>();

            // (2) 関連のあるテーブルのSentence ID参照をupdate
            // word
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE word SET sentence_id=(sentence_id+1) WHERE sentence_id>{0}", sid));
            q.ExecuteUpdate();
            // sentence_documenttag
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence_documenttag SET sentence_id=(sentence_id+1) WHERE sentence_id>{0}", sid));
            q.ExecuteUpdate();
            // segment
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE segment SET sentence_id=(sentence_id+1) WHERE sentence_id>{0}", sid));
            q.ExecuteUpdate();
            // link
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE link SET from_sentence_id=(from_sentence_id+1) WHERE from_sentence_id>{0}", sid));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE link SET to_sentence_id=(to_sentence_id+1) WHERE to_sentence_id>{0}", sid));
            q.ExecuteUpdate();

            // (3) senに付いているWordをsenとsen2に分割する
            int newWordSeq = 0;
            foreach (Word w in sen.Words)
            {
                m_Context.Session.Refresh(w);
                int spos = w.StartChar - sen.StartChar;
                int epos = w.EndChar - sen.EndChar;
                if (spos < splitPos && splitPos < epos)
                {
                    throw new Exception(string.Format("Cannot split sentence over a word: {0}", w.Text));
                }
                if (spos >= splitPos)
                {
                    w.Sen = sen2;
                    w.Pos = newWordSeq++;
                }
            }
            m_Context.Flush();
            // Word.Posを元に、メモリ内のSentence.Wordsリストを同期する
            m_Context.Session.Refresh(sen);
            m_Context.Session.Refresh(sen2);

            // (4) senに付いているSegmentをsenとsen2に分割する。
            //     また、senの最後の位置にdummy bunsetsu segmentを追加する。
            int senOffset = sen.StartChar;
            List<Segment> movedSegments = new List<Segment>();
            IList<Segment> segs = m_Context.Session
                .CreateQuery(string.Format("from Segment where Sentence.ID={0}", sen.ID))
                .List<Segment>();
            Segment srcDummySegment = null;
            foreach (Segment seg in segs)
            {
                m_Context.Session.Refresh(seg);
                int spos = seg.StartChar - senOffset;
                int epos = seg.EndChar - senOffset;
                if (spos < splitPos && splitPos < epos)
                {
                    string segTxt = strTxt.Substring(Math.Max(0, spos), Math.Min(strTxt.Length, epos - spos - 1));
                    throw new Exception(string.Format("Cannot split sentence over a segment: {0} (\"{1}\")", seg.Tag.Name, segTxt));
                }
                if (spos >= splitPos)
                {
                    seg.Sentence = sen2;
                    movedSegments.Add(seg);
                }
                if (spos == epos && seg.Tag.Name == "Bunsetsu") srcDummySegment = seg;
            }

            long maxSegID = m_Context.Session.CreateQuery("select max(ID) from Segment").UniqueResult<long>();
            //@SQL
            q = m_Context.Session.CreateSQLQuery(
                string.Format("INSERT INTO segment VALUES({0},{1},{2},{3},{4},{5},'',{6},{7},{8},'',{9})",
                   maxSegID + 1, srcDummySegment.Tag.ID, srcDummySegment.Version.ID, sen.ParentDoc.ID,
                   splitPos + senOffset, splitPos + senOffset,
                   srcDummySegment.Proj.ID, srcDummySegment.User.ID,
                   m_Context.DBService.GetDefault(), sen.ID));
            q.ExecuteUpdate();
            m_Context.Flush();
            Segment newDummySegment = m_Context.Session
                .CreateQuery(string.Format("from Segment where ID={0}", maxSegID + 1)).UniqueResult<Segment>();


            // (5) movedSegmentに関わるLinkを移動する.
            IList<Link> links = m_Context.Session
                .CreateQuery(string.Format("from Link where FromSentence.ID={0} or ToSentence.ID={0}", sen.ID))
                .List<Link>();
            foreach (Link lnk in links)
            {
                if (movedSegments.Contains(lnk.From))
                {
                    if (!movedSegments.Contains(lnk.To))
                    {
                        // sen2内からsenへのLinkの場合、強制的にsen2のdummyへのLinkとする
                        lnk.To = srcDummySegment;
                    }
                }
                else
                {
                    if (movedSegments.Contains(lnk.To))
                    {
                        // sen内からsen2へのLinkの場合、強制的にsenのdummyへのLinkとする
                        lnk.To = newDummySegment;
                    }
                }
                if (movedSegments.Contains(lnk.From))
                {
                    lnk.FromSentence = sen2;
                }
                if (movedSegments.Contains(lnk.To))
                {
                    lnk.ToSentence = sen2;
                }
            }
            m_Context.Flush();

            return sen2;
        }

        public void MergeSentence(int sid1)
        {
            if (m_Corpus == null || m_Context == null)
            {
                throw new Exception("Corpus or Context is null.");
            }
            int sid2 = sid1 + 1;

            IQuery q;

            // (1) sen2に付いているWordをsenに併合する
            Sentence sen1 = m_Context.Session.CreateQuery(string.Format("from Sentence where ID={0}", sid1)).UniqueResult<Sentence>();
            Sentence sen2 = m_Context.Session.CreateQuery(string.Format("from Sentence where ID={0}", sid2)).UniqueResult<Sentence>();
            m_Context.Session.Refresh(sen1);
            m_Context.Session.Refresh(sen2);
            if (sen1.ParentDoc != sen2.ParentDoc)
            {
                throw new Exception("Attempt to merge sentences in different Documents!");
            }
            int newWordSeq = sen1.Words.Count;
            foreach (Word w in sen2.Words)
            {
                w.Pos = newWordSeq++;
                w.Sen = sen1;
                sen1.Words.Add(w);
            }
            sen2.Words.Clear();
            m_Context.Session.Refresh(sen1);
            m_Context.Session.Refresh(sen2);

            // (4) sen1のendCharを変更
            int lastSen1EndChar = sen1.EndChar;
            sen1.EndChar = sen2.EndChar;

            // (5) sen1の最後の位置にあるdummy bunsetsu segmentを削除する。
            Segment sen2FirstBunsetsu = null;
            IList<Segment> segsInSen2 = m_Context.Session
                .CreateQuery(string.Format("from Segment where Sentence.ID={0} order by StartChar asc", sid2))
                .List<Segment>();
            foreach (Segment seg in segsInSen2)
            {
                if (sen2FirstBunsetsu == null && seg.Tag.Name == "Bunsetsu")
                {
                    sen2FirstBunsetsu = seg;
                }
            }

            // (6) dummySegmentToRemoveにかかるLinkを、sen2の先頭segment(bunsetsu)に係るように変更する。
            Segment dummySegmentToRemove = m_Context.Session
                .CreateQuery(string.Format("from Segment where Sentence.ID={0} and StartChar=EndChar and Tag.Name='Bunsetsu'", sid1))
                .UniqueResult<Segment>();
            //TODO: 本体が空のSentence（例外的に存在）では上記クエリは2個の結果を返す。

            IList<Link> links = m_Context.Session
                .CreateQuery(string.Format("from Link where FromSentence.ID={0} or ToSentence.ID={0}", sid1))
                .List<Link>();
            foreach (Link lnk in links)
            {
                if (lnk.To.ID == dummySegmentToRemove.ID)
                {
                    lnk.To = sen2FirstBunsetsu;
                }
            }
            m_Context.Session.Delete(dummySegmentToRemove);
            m_Context.Flush();

            // (7) sid2を削除して以降のIDを詰める
            //@SQL
            //q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence SET id=-id WHERE id>={0}", sid2));
            //q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("DELETE FROM sentence WHERE id={0}", sid2));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence SET id=(id-1) WHERE id>{0}", sid1));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence SET pos=(pos-1) WHERE id>{0} AND document_id={1}", sid1, sen1.ParentDoc.ID));
            q.ExecuteUpdate();

            // (8) 関連のあるテーブルのSentence ID参照をupdate (sid2->sid1)
            // word
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE word SET sentence_id=(sentence_id-1) WHERE sentence_id>{0}", sid1));
            q.ExecuteUpdate();
            // sentence_documenttag
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE sentence_documenttag SET sentence_id=(sentence_id-1) WHERE sentence_id>{0}", sid1));
            q.ExecuteUpdate();
            // segment
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE segment SET sentence_id=(sentence_id-1) WHERE sentence_id>{0}", sid1));
            q.ExecuteUpdate();
            // link
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE link SET from_sentence_id=(from_sentence_id-1) WHERE from_sentence_id>{0}", sid1));
            q.ExecuteUpdate();
            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE link SET to_sentence_id=(to_sentence_id-1) WHERE to_sentence_id>{0}", sid1));
            q.ExecuteUpdate();

            m_Context.Flush();

            // Ticket #24529: 「MergeSentenceの結果文構造がおかしくなることがある」への対応
            // このメソッドを出る前にSentenceの持つWordリストを必ず更新することで、
            // 連続呼び出し時にWordが更新されないことを防ぐ。(ReloadSentences()ではWordリストまでは更新されない）
            foreach (Word w in sen1.Words)
            {
                m_Context.Session.Refresh(w);
            }
            foreach (Word w in sen2.Words)
            {
                m_Context.Session.Refresh(w);
            }
        }

        [Obsolete("文内容の変更はSchema#8以降は許可されなくなりました.（以前はEOS除去のためにのみ使用されていました.）", true)]
        public void InsertDocumentText(int docID, int pos, string stringToInsert)
        {
            //@SQL
            IQuery q = m_Context.Session.CreateSQLQuery(string.Format("SELECT document_text FROM document WHERE document_id={0}", docID));
            string result = q.UniqueResult<string>();
            result = result.Insert(pos, stringToInsert);

            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE document SET document_text=? WHERE document_id={0}", docID));
            q.SetString(0, result);
            q.ExecuteUpdate();
        }

        [Obsolete("文内容の変更はSchema#8以降は許可されなくなりました.（以前はEOS除去のためにのみ使用されていました.）", true)]
        public void RemoveDocumentText(int docID, int pos, int lengthToDelete)
        {
            //@SQL
            IQuery q = m_Context.Session.CreateSQLQuery(string.Format("SELECT document_text FROM document WHERE document_id={0}", docID));
            string result = q.UniqueResult<string>();
            result = result.Remove(pos, lengthToDelete);

            q = m_Context.Session.CreateSQLQuery(string.Format("UPDATE document SET document_text=? WHERE document_id={0}", docID));
            q.SetString(0, result);
            q.ExecuteUpdate();
        }


        public void Open(Corpus cps, UnlockRequestCallback callback)
        {
            if (m_Context != null && m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("Active transaction exists. Close or Commit it first.");
            }

            if (m_Corpus == null || cps != m_Corpus)
            {
                m_Corpus = cps;
            }
            m_Context = OpContext.Create(m_Corpus.DBParam, callback, typeof(ISentenceEditService));
        }

        public void Commit()
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

        public void Close()
        {
            if (m_Context != null)
            {
                m_Context.Dispose();
                m_Context = null;
                m_Corpus = null;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
