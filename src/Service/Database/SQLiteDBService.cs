using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data.Common;
using System.IO;

namespace ChaKi.Service.Database
{
    public class SQLiteDBService : DBService
    {
        public SQLiteDBService()
        {
        }

        public override void GetDatabaseList(ref List<string> dblist)
        {
            // dummy
            return;
        }

        public override void CreateDatabase()
        {
            // dummy
        }

        public override bool DatabaseExists()
        {
            return File.Exists(DBParam.DBPath);
        }


        public override void DropDatabase()
        {
            File.Delete(DBParam.DBPath);
        }


        public override void SetupConnection(NHibernate.Cfg.Configuration cfg)
        {
            cfg.SetProperty("dialect", "NHibernate.Dialect.SQLiteDialect");
            cfg.SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider");
            cfg.SetProperty("connection.driver_class", "NHibernate.Driver.SQLite20Driver");
            cfg.SetProperty("query.substitutions", "true=1;false=0");
            cfg.SetProperty("connection.connection_string", GetConnectionString());
            cfg.SetProperty("proxyfactory.factory_class", "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle");
            //cfg.SetProperty("show_sql", "true");

            SQLiteFunction.RegisterFunction(typeof(SQLiteFunc_RegExp));
        }

        public override string GetConnectionString()
        {
            return string.Format("Data Source=\"{0}\";Version=3", DBParam.DBPath);
        }

        public override DbConnection GetDbConnnection()
        {
            return new SQLiteConnection(GetConnectionString());
        }

        public override string GetDefault()
        {
            return "\"\"";
        }

        public override string GetBooleanString(bool b)
        {
            return b ? "1" : "0";
        }

        /// <summary>
        /// SQLiteで有効なSynchoronous Pragma設定
        /// </summary>
        /// <param name="mode"></param>
        public override void SetSynchronousMode(string mode)
        {
            using (DbConnection cnn = this.GetDbConnnection())
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cnn.Open();
                cmd.CommandText = string.Format("PRAGMA synchronous={0}", mode);
                cmd.CommandTimeout = 600;   // 10 minutes
                cmd.ExecuteNonQuery();
            }
        }
        public override DbParameter CreateParameter()
        {
            return new SQLiteParameter();
        }

    }
}
