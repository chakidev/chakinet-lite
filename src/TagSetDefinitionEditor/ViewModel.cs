using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using System.Data;
using System.Linq;

namespace ChaKi.TagSetDefinitionEditor
{
    internal class ViewModel
    {
        internal enum Triggers
        {
            Initialize,
            BeginEdit,
            EndEdit,
        }

        internal enum States
        {
            Browsing,
            Editing,
        }

        internal ViewModel()
        {
            this.VersionList = new List<KeyValuePair<string, bool>>();
            this.State = States.Browsing;
        }

        public States State { get; set; }
        public Corpus Corpus { get; set; }
        public TagSet TagSet { get; set; }
        public IList<KeyValuePair<string, bool>> VersionList { get; set; }
        public TagSetVersion CurrentVersion { get; set; }

        public DataTable Segments { get; set; }
        public DataTable Links { get; set; }
        public DataTable Groups { get; set; }

        public int IndexOfCurrentVersion
        {
            get
            {
                for (int i = 0; i < this.VersionList.Count; i++)
                {
                    if (this.VersionList[i].Value)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        public IList<string> VersionListAsString
        {
            get
            {
                var result = new List<string>();
                foreach (var pair in this.VersionList)
                {
                    if (pair.Value)
                    {
                        result.Add(string.Format("{0} (current)", pair.Key));
                    }
                    else
                    {
                        result.Add(pair.Key);
                    }
                }
                return result;
            }
        }
    }
}
