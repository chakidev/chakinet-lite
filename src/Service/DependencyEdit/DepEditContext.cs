using ChaKi.Entity.Corpora;
using NHibernate;
using ChaKi.Entity.Corpora.Annotations;
using System.Collections.Generic;
using ChaKi.Service.Database;
using ChaKi.Entity;
using System;

namespace ChaKi.Service.DependencyEdit
{
    internal class DepEditContext : OpContext
    {
        private DepEditContext()
            : base()
        {
            this.TagSet = null;
            this.TSVersion = null;
            this.RelevantLexemes = new List<Lexeme>();
            MaxLexIDAtBeginning = -1;
        }

        public static DepEditContext Create(DBParameter param)
        {
            return Create(param, null, typeof(IDepEditService));
        }

        public static DepEditContext Create(DBParameter param, UnlockRequestCallback unlockCallback)
        {
            return Create(param, unlockCallback, typeof(IDepEditService));
        }

        public static DepEditContext Create(DBParameter param, UnlockRequestCallback callback, Type requestingService)
        {
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            var ctx = new DepEditContext();
            ctx.DBService = DBService.Create(param, callback, requestingService);
            ctx.Session = ctx.DBService.OpenSession();
            ctx.Trans = ctx.Session.BeginTransaction();
            return ctx;
        }


        public TagSet TagSet { get; set; }
        public TagSetVersion TSVersion { get; set; }
        // 以下2項目はToIronRubyStatement()で使用.
        public int CharOffset { get; set; }  // 操作対象文における、KWIC中心語（なければ文頭）のDocument内絶対文字位置(Word.StartChar)
        public int WordOffset { get; set; }  // 操作対象文における、KWIC中心語（なければ文頭）のSentence内番号(Word.Pos)

        /// <summary>
        /// 現セッションで異動（追加・削除・更新）のあったLexemeのリスト.
        /// </summary>
        public List<Lexeme> RelevantLexemes { get; set; }


        /// <summary>
        /// 現Transaction開始時に、その時点での最大LexemeIDをセットする.
        /// （それ以下のIDを持つLexemeは、形態素編集において決して削除しない）
        /// </summary>
        public int MaxLexIDAtBeginning { get; set; }

        public void AddRelevantLexeme(Lexeme lex)
        {
            if (!this.RelevantLexemes.Contains(lex))
            {
                this.RelevantLexemes.Add(lex);
            }
        }
    }
}
