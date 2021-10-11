using System.Collections;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;
using NHibernate;
using NHibernate.Criterion;
using ChaKi.Entity.Kwic;
using System;
using System.Text;
using System.Diagnostics;
using System.Data;
using ChaKi.Common.Settings;

namespace ChaKi.Service.Search
{
    public class TagSearchService : SearchServiceBase
    {
        private KwicList m_Model;
        private LexemeResultSet m_LexemeResultSet;  //���Ԍ��ʂƂ��Ă�Lexeme���X�g�̃��X�g

        public TagSearchService(SearchHistory hist, SearchHistory parent)
            : base(hist, parent)
        {
            m_Model = hist.KwicList;
            m_LexemeResultSet = null;
        }

        /// <summary>
        /// �R�[�p�X���Ƃ�TagSearch�{��
        /// </summary>
        /// <param name="c">The c.</param>
        protected override void ExecuteSearchSession(Corpus c)
        {
            SearchConditions cond = m_CondSeq.Last;

            // AND�����i�i�荞�݁j�̏ꍇ�́AtagCond�̐e��KwicList��Sentece�W���������ɉ�����B
            List<int> targetSentences = null;
            if (cond.Operator == SearchSequenceOperator.And)
            {
                if (cond.Parent != null && m_HistParent != null)
                {
                    targetSentences = m_HistParent.KwicList.MakeSentenceIDListOfCorpus(c);
                }
            }

            // Word�̌���������(SQL)���쐬���A���������s����B
            string qstr = QueryBuilder.Instance.BuildTagSearchQuerySQL(cond, targetSentences);
            IQuery query = m_Session.CreateSQLQuery(qstr).AddEntity(typeof(Word));
            IList<Word> queryResult = query.List<Word>();
            int totalCount = queryResult.Count;
            IEnumerable<Word> filteredResult = (cond.FilterCond.AllEnabled) ?
                cond.FilterCond.ResultsetFilter.CreateEnumerable<Word>(queryResult) : queryResult;

            // �����܂łŌ����͏I���B

            // �������ꂽWord�ɑ΂��āA����Word�������Ă���Sentence�����ǂ�A
            // �����e��Kwic�ɕϊ��E�o�͂���
            int n = 0;
            m_Progress.SetRange(totalCount);
            foreach (Word centerWord in filteredResult)
            {
                int position = centerWord.Pos;   // KWIC��center word�ƂȂ��̈ʒu
                Sentence sen = centerWord.Sen;
                KwicItem ki = new KwicItem(c, sen.ParentDoc, sen.ID, sen.StartChar, sen.EndChar, sen.Pos);
                int pos = 0;

                // sen.Words �ȉ��̃A�N�Z�X�́AHibernate�ł͂Ȃ�����SQL�ōs���iMax�p�t�H�[�}���X�̂��߁j
                List<int> lexids = new List<int>();
                //!SQL
                ISQLQuery wq;
                if (cond.FilterCond.TargetProjectId >= 0)
                {
                    wq = m_Session.CreateSQLQuery(string.Format("SELECT * from word where sentence_id={0} and project_id={1} ORDER BY position", 
                        sen.ID, cond.FilterCond.TargetProjectId))
                            .AddEntity(typeof(Word));
                }
                else
                {
                    wq = m_Session.CreateSQLQuery(string.Format("SELECT * from word where sentence_id={0} ORDER BY position", sen.ID))
                            .AddEntity(typeof(Word));
                }
                var wlist = wq.List<Word>();
                foreach (var word in wlist)
                {
                    int lexid = word.Lex.ID;
                    // Corpus��Lexicon�֖₢���킹���ǉ�
                    Lexeme lex;
                    if (!c.Lex.TryGetLexeme(lexid, out lex))
                    {
                        //!SQL
                        ISQLQuery lq = m_Session.CreateSQLQuery(string.Format("SELECT * from lexeme where id={0}", lexid))
                            .AddEntity(typeof(Lexeme));
                        lex = lq.UniqueResult<Lexeme>();
                        c.Lex.Add(lex);
                    }

                    if (SearchSettings.Current.RetrieveExtraWordProperty)
                    {
                        QueryWordMappings(word);
                    }

                    if (pos < position)
                    {
                        ki.Left.AddLexeme(lex, word, 0);
                    }
                    else if (pos == position)
                    {
                        ki.Center.AddLexeme(lex, word, KwicWord.KWA_PIVOT);
                    }
                    else
                    {
                        ki.Right.AddLexeme(lex, word, 0);
                    }
                    pos++;
                }
                m_Model.AddKwicItem(ki);
                m_Progress.Increment();
                n++;
            }
        }
    }
}
