using System;
using System.Collections.Generic;
using System.Text;
using NHibernate;
using ChaKi.Entity.Corpora;
using ChaKi.Entity;
using System.Data;
using ChaKi.Service.DependencyEdit;

namespace ChaKi.Service.Database
{
    /// <summary>
    /// Session, Transaction管理を行うクラス
    /// ChaKi EntityのProject, Userも同時に保持し、DBへの変更が
    /// どのProject, Userによって行われているかを知っている.
    /// </summary>
    public class OpContext : IContext, IDisposable
    {
        protected OpContext()
        {
            this.Session = null;
            this.Trans = null;
            this.Sen = null;
            this.Proj = null;
            this.User = null;
            this.Name = string.Empty;
        }

        public string Name { get; set; }
        public Project Proj { get; set; }
        public User User { get; set; }
        public ISession Session { get; set; }
        public Sentence Sen { get; set; }
        public ITransaction Trans { get; set; }

        public DBService DBService { get; protected set; }

        public bool IsTransactionActive()
        {
            return (this.Trans != null && this.Session != null);
        }

        public static OpContext Create(DBParameter param, UnlockRequestCallback callback, Type requestingService)
        {
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            OpContext ctx = new OpContext();
            ctx.DBService = DBService.Create(param, callback, requestingService);
            ctx.Session = ctx.DBService.OpenSession();
            ctx.Trans = ctx.Session.BeginTransaction();
            return ctx;
        }

        public void SaveOrUpdate(object obj)
        {
            this.Session.SaveOrUpdate(obj);
        }

        public void Save(object obj)
        {
            this.Session.Save(obj);
        }

        public void Delete(object obj)
        {
            this.Session.Delete(obj);
        }

        public void CreateSavePoint(string spname)
        {
            using (var cmd = this.Session.Connection.CreateCommand())
            {
                cmd.CommandText = $"SAVEPOINT {spname}";
                cmd.CommandTimeout = 5;   // 5 second
                cmd.ExecuteNonQuery();
            }
        }

        public void ReleaseSavePoint(string spname)
        {
            using (var cmd = this.Session.Connection.CreateCommand())
            {
                cmd.CommandText = $"RELEASE SAVEPOINT {spname}";
                cmd.CommandTimeout = 5;   // 5 second
                cmd.ExecuteNonQuery();
            }
        }

        public void RollbackTo(string spname)
        {
            using (var cmd = this.Session.Connection.CreateCommand())
            {
                cmd.CommandText = $"ROLLBACK TO SAVEPOINT {spname}";
                cmd.CommandTimeout = 5;   // 5 second
                cmd.ExecuteNonQuery();
            }
        }


        public void Flush()
        {
            this.Session.Flush();
        }

        public void Dispose()
        {
            if (IsTransactionActive())
            {
                this.Trans.Rollback();
            }
            try
            {
                if (this.Trans != null)
                {
                    this.Trans.Dispose();
                }
                if (this.Session != null)
                {
                    this.Session.Dispose();
                }
            }
            finally
            {
                this.Trans = null;
                this.Session = null;
            }
        }
    }
}
