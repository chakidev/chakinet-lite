using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora
{
    public class User
    {
        public virtual int ID { get; set; }

        public virtual string Name { get; set; }

        [XmlIgnore]
        public virtual string Password { get; set; }

        [XmlIgnore]
        public virtual IList<Project> Projects { get; set; }

        [XmlIgnore]
        public virtual int Privilege { get; set; }

        [XmlIgnore]
        public virtual string Comments { get; set; }

        public User()
        {
            this.ID = 0;
            this.Projects = new List<Project>();
        }

        /// <summary>
        /// ログオン中のユーザー（アプリケーションに対して一つセットされる）.
        /// DBと無関係にName,PasswordがセットされているTransient Objectであり、
        /// 各CorpusDBに対してアクセス時に認証を行う目的以外には使用しない.
        /// </summary>
        public static User Current = null;

        public static string DefaultName = "root";
    }
}
