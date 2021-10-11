using ChaKi.Entity;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    class DictionaryAccessor_DB : DictionaryAccessor
    {
        public DBService DBService { get; protected set; }
        public ISession Session { get; set; }
        public ITransaction Trans { get; set; }

        public DictionaryAccessor_DB(Dictionary_DB dict)
        {
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            this.DBService = DBService.Create(dict.DBParam);
            this.Session = this.DBService.OpenSession();
            this.Trans = this.Session.BeginTransaction();

            m_Name = dict.Name;
            if (this.Session.IsConnected)
            {
                this.Trans = null;
            }
        }

        private string m_Name;
        public override string Name
        {
            get { return m_Name; }
        }

        public override bool IsConnected
        {
            get
            {
                return this.Session.IsConnected;
            }
        }


        public override void Dispose()
        {
            if (this.Trans != null) this.Trans.Dispose();
            if (this.Session != null) this.Session.Dispose();
        }

        public override IList<Lexeme> FindLexemeBySurface(string surface)
        {
            return this.Session.CreateCriteria<Lexeme>()
                .Add(Expression.Eq("Surface", surface))
                .List<Lexeme>();
        }

        public override IList<MWE> FindMWEBySurface(string surface)
        {
            return new List<MWE>(); // This Dictionary does not support MWE(Compound Word)
        }

        public override IList<MWE> FindMWEBySurface2(string surface)
        {
            return new List<MWE>(); // This Dictionary does not support MWE(Compound Word)
        }

        public override void RegisterMWE(MWE mwe)
        {
            // This Dictionary does not support MWE(Compound Word)
        }
    }
}
