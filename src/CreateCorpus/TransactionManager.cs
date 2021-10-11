using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Threading;
using ChaKi.Service.Database;

namespace CreateCorpus
{
    internal class SimpleTransactionManager : IDisposable
    {
        public DbConnection Conn = null;
        public DbCommand Cmd = null;
        public DbTransaction Trans = null;

        private DBService m_Service;

        public SimpleTransactionManager(DBService svc)
        {
            m_Service = svc;
        }

        public void Begin()
        {
            Conn = m_Service.GetDbConnnection();
            Conn.Open();
            Cmd = Conn.CreateCommand();
            Trans = Conn.BeginTransaction();
            Cmd.Connection = Conn;
            Cmd.Transaction = Trans;
        }

        public DbCommand CommitAndContinue()
        {
            this.Commit();
            this.Dispose();
            // Reopen (Retry to avoid unexpected SQLite file lock situation)
            for (int tries = 0; ; tries++)
            {
                try
                {
                    Conn = m_Service.GetDbConnnection();
                    Conn.Open();
                    break;
                }
                catch (Exception ex)
                {
                    if (tries > 10)
                    {
                        throw ex;
                    }
                    Thread.Sleep(1000);
                    continue;
                }
            }
            Cmd = Conn.CreateCommand();
            Cmd.CommandTimeout = 600;
            for (int tries = 0; ; tries++)
            {
                try
                {
                    Trans = Conn.BeginTransaction();
                    break;
                }
                catch (Exception ex)
                {
                    if (tries > 10)
                    {
                        throw ex;
                    }
                    Thread.Sleep(1000);
                    continue;
                }
            } 
            Cmd.Connection = Conn;
            Cmd.Transaction = Trans;
            return Cmd;
        }

        public void Commit()
        {
            Trans.Commit();
        }

        public void Dispose()
        {
            Trans.Dispose();
            Conn.Close();
            Conn.Dispose();
            Cmd.Dispose();
        }
    }
}
