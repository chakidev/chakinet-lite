using System;
using System.Collections.Generic;
using System.Data.Common;
using Npgsql;

namespace ChaKi.Service.Database
{
    public class PgSQLDBService : DBService
    {
        public override void GetDatabaseList(ref List<string> dblist)
        {
            string cstr = string.Format("Server={0};User ID={1};Password={2}", DBParam.Server, DBParam.Login, DBParam.Password);
            DbConnection cnn = null;
            DbCommand cmd = null;
            try
            {
                cnn = new NpgsqlConnection(cstr);
                cnn.Open();
                cmd = new NpgsqlCommand("SELECT datname from pg_database", (NpgsqlConnection)cnn);
                DbDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    dblist.Add((string)rdr[0]);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn != null)
                {
                    cnn.Dispose();
                }
            }
        }

        public override bool DatabaseExists()
        {
            string cstr = string.Format("Server={0};User ID={1};Password={2}", DBParam.Server, DBParam.Login, DBParam.Password);
            DbConnection cnn = null;
            DbCommand cmd = null;
            try
            {
                cnn = new NpgsqlConnection(cstr);
                cnn.Open();
                cmd = new NpgsqlCommand(
                    string.Format("SELECT datname from pg_database where datname='{0}'", DBParam.Name),
                    (NpgsqlConnection)cnn);
                long result = (long)cmd.ExecuteScalar();
                if (result != 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cnn != null)
                {
                    cnn.Dispose();
                }
            }
            return false;
        }

        public override void SetupConnection(NHibernate.Cfg.Configuration cfg)
        {
            cfg.SetProperty("dialect", "NHibernate.Dialect.PostgreSQLDialect");
            cfg.SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider");
            cfg.SetProperty("connection.driver_class", "NHibernate.Driver.NpgsqlDriver");
            cfg.SetProperty("query.substitutions", "");
            cfg.SetProperty("connection.connection_string", GetConnectionString());
            cfg.SetProperty("proxyfactory.factory_class", "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle");
        }

        public override string GetConnectionString()
        {
            return string.Format("Server={0};Database={1};User ID={2};Password={3}",
                DBParam.Server, DBParam.Name, DBParam.Login, DBParam.Password);
        }

        public override DbConnection GetDbConnnection()
        {
            return new NpgsqlConnection(GetConnectionString());
        }

        public override void CreateDatabase()
        {
            DbConnection cnn = new NpgsqlConnection(
                string.Format("Server={0};User ID={1};Password={2}", DBParam.Server, DBParam.Login, DBParam.Password));
            cnn.Open();
            DbCommand cmd = new NpgsqlCommand(string.Format("CREATE DATABASE {0};", DBParam.Name), (NpgsqlConnection)cnn);
            cmd.ExecuteNonQuery();
        }
        public override void DropDatabase()
        {
            DbConnection cnn = new NpgsqlConnection(
                string.Format("Server={0};User ID={1};Password={2}", DBParam.Server, DBParam.Login, DBParam.Password));
            cnn.Open();
            DbCommand cmd = new NpgsqlCommand(string.Format("DROP DATABASE IF EXISTS {0};", DBParam.Name), (NpgsqlConnection)cnn);
            cmd.ExecuteNonQuery();
        }
        public override DbParameter CreateParameter()
        {
            return new NpgsqlParameter();
        }
    }
}
