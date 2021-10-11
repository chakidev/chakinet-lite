using System;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Corpora.Annotations;
using System.Collections.Generic;

namespace ChaKi.Service.Annotations
{
    public interface IAnnotationService
    {
        /// <summary>
        /// 取得する対象のSeg, Link, Groupの名前を設定する.
        /// </summary>
        /// <param name="seglist"></param>
        /// <param name="linklist"></param>
        /// <param name="grplist"></param>
        void SetTagNameFilter(IList<string> seglist, IList<string> linklist, IList<string> grplist);

        /// <summary>
        /// KwicListに含まれるSentenceに関連するAnnotationをすべてCorpusより取得する
        /// </summary>
        /// <param name="src"></param>
        /// <param name="result"></param>
        void Load(KwicList src, AnnotationList result, Action<int> callback, ref bool cancelFlag);
    }
}
