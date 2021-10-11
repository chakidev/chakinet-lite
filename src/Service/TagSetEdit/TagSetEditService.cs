using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate.Criterion;
using System.Collections;
using ChaKi.Service.Search;
using System.Data;

namespace ChaKi.Service.TagSetEdit
{
    public class TagSetEditService : IDisposable
    {
        public Corpus Corpus { get; private set; }
        public TagSet TagSet { get; private set; }
        public TagSetVersion CurrentVersion { get; private set; }

        private string m_TagSetName;
        private ISessionFactory m_SessionFactory;
        private ISession m_Session;
        private ITransaction m_Trans;

        public TagSetEditService(Corpus c, string tagsetname)
        {
            this.Corpus = c;
            Initialize(tagsetname);
        }

        public IList<KeyValuePair<string, bool>> GetVersionList()
        {
            var result = new List<KeyValuePair<string, bool>>();
            IQuery q = m_Session.CreateQuery(string.Format("select v from TagSetVersion v where v.TagSet.ID={0}",
                this.TagSet.ID));
            foreach (var version in q.List<TagSetVersion>())
            {
                result.Add(new KeyValuePair<string,bool>(version.Version, version.IsCurrent));
            }
            return result;
        }

        // TODO: このメソッドにはversionNameが重ならないことが必要
        public TagSetVersion SetVersion(string versionName)
        {
            TagSetVersion ret = null;
            foreach (TagSetVersion ver in this.TagSet.Versions)
            {
                if (ver.Version == versionName)
                {
                    ret = ver;
                    this.CurrentVersion = ver;
                    ver.IsCurrent = true;
                }
                else
                {
                    ver.IsCurrent = false;
                }

            }
            return ret;
        }

        public DataTable QueryTags(string tagType)
        {
            IQuery q = m_Session.CreateQuery(string.Format("from Tag t where t.Type='{0}' and t.Parent.ID={1} and t.Version.ID={2}",
                tagType, this.TagSet.ID, this.CurrentVersion.ID));
            DataTable table = new DataTable(tagType);
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            foreach (Tag tag in q.List<Tag>())
            {
                DataRow row = table.NewRow();
                row["Name"] = tag.Name;
                row["Description"] = tag.Description;
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Current TagSetのTagをすべて与えられたDataTableの内容で置換する
        /// </summary>
        /// <param name="segs"></param>
        /// <param name="links"></param>
        /// <param name="groups"></param>
        public void UpdateTags(DataTable segs, DataTable links, DataTable groups)
        {
            // 既に存在していたCurrent Tagをすべて一旦削除
            m_Session.Delete(string.Format("from Tag t where t.Parent.ID={0} and t.Version.ID={1}",
                this.TagSet.ID, this.CurrentVersion.ID));

            int tagid = m_Session.CreateQuery(string.Format("select max(t.ID) from Tag t")).UniqueResult<int>();

            foreach (var table in new Dictionary<string, DataTable>() { { Tag.SEGMENT, segs }, { Tag.LINK, links }, {Tag.GROUP, groups }})
            {
                foreach (DataRow row in table.Value.Rows)
                {
                    Tag t = new Tag(table.Key, (string)row["Name"])
                    {
                        ID = ++tagid,
                        Description = (row["Description"] != DBNull.Value)?(string)row["Description"]:null,
                        Parent = this.TagSet,
                        Version = this.CurrentVersion
                    };
                    this.TagSet.AddTag(t);
                    m_Session.Save(t);
                }
            }
        }

        public void CreateNewVersion(string name)
        {
            // ID選定
            int rev = m_Session.CreateQuery(string.Format("select max(v.Revision) from TagSetVersion v where v.TagSet.ID={0}", this.TagSet.ID)).UniqueResult<int>();
            int verid = m_Session.CreateQuery(string.Format("select max(v.ID) from TagSetVersion v")).UniqueResult<int>();
            int tagid = m_Session.CreateQuery(string.Format("select max(t.ID) from Tag t")).UniqueResult<int>();

            // 新しいSetVersionを生成
            TagSetVersion newVersion = new TagSetVersion(name, rev + 1, true) { ID = verid + 1, TagSet = this.TagSet };
            m_Session.Save(newVersion);
            this.TagSet.AddVersion(newVersion);

            // CurrentVersionのタグを全てコピー
            IQuery q = m_Session.CreateQuery(string.Format("from Tag t where t.Parent.ID={0} and t.Version.ID={1}",
                this.TagSet.ID, this.CurrentVersion.ID));
            foreach (Tag t in q.List<Tag>())
            {
                Tag newTag = new Tag(t.Type, t.Name) { ID = ++tagid, Parent = this.TagSet, Version = newVersion };
                this.TagSet.AddTag(newTag);
                m_Session.Save(newTag);
            }

            // 新しいVersionをCurrentに
            this.CurrentVersion.IsCurrent = false;
            this.CurrentVersion = newVersion;
        }

        public bool DeleteCurrentVersion()
        {
            // TagがCorpus中で使用されていないかをチェックする.
            IQuery q = m_Session.CreateQuery(string.Format("from Tag t where t.Parent.ID={0} and t.Version.ID={1}",
                this.TagSet.ID, this.CurrentVersion.ID));
            foreach (Tag t in q.List<Tag>())
            {
                q = m_Session.CreateQuery(string.Format("select count(*) from Segment seg where seg.Tag.ID={0}", t.ID));
                if (q.UniqueResult<long>() != 0) {
                    return false;
                }
                q = m_Session.CreateQuery(string.Format("select count(*) from Link link where link.Tag.ID={0}", t.ID));
                if (q.UniqueResult<long>() != 0) {
                    return false;
                }
                q = m_Session.CreateQuery(string.Format("select count(*) from Group grp where grp.Tag.ID={0}", t.ID));
                if (q.UniqueResult<long>() != 0) {
                    return false;
                }
            }

            BeginTransaction();
            m_Session.Delete(string.Format("from Tag t where t.Parent.ID={0} and t.Version.ID={1}",
                this.TagSet.ID, this.CurrentVersion.ID));
            q = m_Session.CreateSQLQuery(string.Format("DELETE from tagset_version where version_id={0}", this.CurrentVersion.ID));
            q.ExecuteUpdate();
            this.TagSet.Versions.Remove(this.CurrentVersion);
            m_Session.Flush();
            m_Session.Update(this.TagSet);
            foreach (var version in this.TagSet.Versions)
            {
                m_Session.Update(version);
            }
            this.TagSet.CurrentVersion = this.TagSet.Versions[this.TagSet.Versions.Count - 1];
            this.CurrentVersion = this.TagSet.CurrentVersion;
            this.CurrentVersion.IsCurrent = true;
            Commit();
            return true;
        }

        public void ChangeTagSetName(string newname)
        {
            using (var trans = m_Session.BeginTransaction())
            {
                var q = m_Session.CreateSQLQuery(string.Format("UPDATE tagset SET tagset_name='{0}' WHERE id={1}", newname, this.TagSet.ID));
                q.ExecuteUpdate();
                m_TagSetName = newname;
                trans.Commit();
            }
        }

        public void BeginTransaction()
        {
            if (m_Trans == null)
            {
                m_Trans = m_Session.BeginTransaction();
            }
        }

        public void Commit()
        {
            if (m_Trans != null)
            {
                m_Trans.Commit();
                m_Session.Close();
                StartSession();
                m_Trans = null;
            }
        }

        public void Rollback()
        {
            if (m_Trans != null)
            {
                m_Trans.Rollback();
                m_Session.Close();
                StartSession();
                m_Trans = null;
            }
        }

        private void Initialize(string tagsetname)
        {
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            SearchConfiguration.GetInstance().Initialize();
            DBService dbs = DBService.Create(this.Corpus.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            m_SessionFactory = cfg.BuildSessionFactory();
            m_TagSetName = tagsetname;

            StartSession();
        }

        private void StartSession()
        {
            m_Session = m_SessionFactory.OpenSession();
            m_Trans = null;

            // TagSetオブジェクトを検索
            IQuery q = m_Session.CreateQuery(string.Format("from TagSet t where t.Name='{0}'", m_TagSetName));
            IList<TagSet> results = q.List<TagSet>();
            if (results.Count == 0)
            {
                Dispose();
                throw new Exception(string.Format("TagSet not found for name: {0}", m_TagSetName));
            }
            else if (results.Count > 1)
            {
                Dispose();
                throw new Exception(string.Format("Multiple TagSet found for name: {0}", m_TagSetName));
            }
            this.TagSet = results[0];

            q = m_Session.CreateQuery("from TagSetVersion v where v.IsCurrent=true");
            this.CurrentVersion = q.UniqueResult<TagSetVersion>();
            // CurrentVersionがなければ設定を修正して最もIDの大きいものをCurrentにする
            if (this.CurrentVersion == null)
            {
                TagSetVersion lastver = null;
                foreach (TagSetVersion v in this.TagSet.Versions)
                {
                    v.IsCurrent = false;
                    lastver = v;
                }
                if (lastver != null)
                {
                    lastver.IsCurrent = true;
                }
                this.CurrentVersion = lastver;
            }
        }

        public void Dispose()
        {
            try
            {
                if (m_Trans != null)
                {
                    m_Trans.Dispose();
                }
                if (m_Session != null)
                {
                    m_Session.Dispose();
                }
            }
            finally
            {
                m_Trans = null;
                m_Session = null;
            }
        }
    }
}
