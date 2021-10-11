using System;
using System.Collections.Generic;
using System.Text;
using NHibernate;
using NHibernate.Cfg;

namespace ChaKi.Service.Search
{
    public class SearchConfiguration
    {
        private static SearchConfiguration m_Instance = null;
        public NHibernate.Cfg.Configuration NHibernateConfig { get; set; }

        public static SearchConfiguration GetInstance()
        {
            if (m_Instance == null) {
                m_Instance = new SearchConfiguration();
                m_Instance.Initialize();
            }
            return m_Instance;
        }

        public void Initialize()
        {
            NHibernate.Cfg.Environment.Properties[NHibernate.Cfg.Environment.ShowSql] = "false";

            NHibernateConfig = new Configuration();
            NHibernateConfig.SetProperties(new Dictionary<string,string>());
            NHibernateConfig.Properties[NHibernate.Cfg.Environment.ConnectionProvider]
                = "NHibernate.Connection.DriverConnectionProvider";
            NHibernateConfig.Properties[NHibernate.Cfg.Environment.ConnectionDriver] = "NHibernate.Driver.SQLite20Driver";
            NHibernateConfig.Properties[NHibernate.Cfg.Environment.Dialect] = "NHibernate.Dialect.SQLiteDialect";
            NHibernateConfig.AddAssembly("ChaKiEntity");
        }

        private SearchConfiguration()
        {
        }
    }
}
