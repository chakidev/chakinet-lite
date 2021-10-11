using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;

namespace ChaKi.Service.AttributeEditService
{
    public interface IAttributeEditService
    {
        void Open(Corpus corpus, object targetEntity, UnlockRequestCallback callback);

        void Close();

        IList<AttributeBase32> GetSentenceTagList();
        void UpdateAttributesForSentence(Dictionary<string, string> newSenAttrs, Dictionary<string, string> newDocAttrs);

        void UpdateAttributesForSegment(Dictionary<string, string> newData, Segment seg = null);
        void UpdateAttributesForLink(Dictionary<string, string> newData, Link link = null);
        void UpdateAttributesForGroup(Dictionary<string, string> newData, Group group = null);

        void Commit();
    }
}
