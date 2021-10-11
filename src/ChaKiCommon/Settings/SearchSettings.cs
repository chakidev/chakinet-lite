using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Common.Settings
{
    public enum ContextCountUnit
    {
        Sentence = 0,
    }

    public class SearchSettings : ICloneable
    {
        public uint ContextRetrievalRange { get; set; }
        public ContextCountUnit ContextRetrievalUnit { get; set; }

        public bool RetrieveExtraWordProperty { get; set; }

        public bool UseRemote { get; set; }
        public string RemoteAddress { get; set; }

        public static SearchSettings Current = new SearchSettings();

        private SearchSettings()
        {
            this.UseRemote = false;
            this.RemoteAddress = "localhost";

            ContextRetrievalRange = 3;  // 3 sentences
            ContextRetrievalUnit = ContextCountUnit.Sentence;
            this.RetrieveExtraWordProperty = false;
        }

        public SearchSettings Copy()
        {
            SearchSettings obj = new SearchSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(SearchSettings src)
        {
            this.UseRemote = src.UseRemote;
            this.RemoteAddress = string.Copy(src.RemoteAddress);

            this.ContextRetrievalRange = src.ContextRetrievalRange;
            this.ContextRetrievalUnit = src.ContextRetrievalUnit;
            this.RetrieveExtraWordProperty = src.RetrieveExtraWordProperty;
        }

        #region ICloneable メンバ
        public object Clone()
        {
            SearchSettings obj = new SearchSettings();
            obj.UseRemote = this.UseRemote;
            obj.RemoteAddress = string.Copy(this.RemoteAddress);
            obj.ContextRetrievalRange = this.ContextRetrievalRange;
            obj.ContextRetrievalUnit = this.ContextRetrievalUnit;
            obj.RetrieveExtraWordProperty = this.RetrieveExtraWordProperty;
            return obj;
        }
        #endregion
    }
}
