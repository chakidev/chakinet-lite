using ChaKi.Entity.Corpora;
using System.Collections.Generic;
using System;

namespace ChaKi.Service.SentenceEdit
{
    public interface ISentenceEditService
    {
        void Open(Corpus cps, UnlockRequestCallback callback);
        void Close();
        void Commit();

        /// <summary>
        /// Sentence境界を指定した長さの所で切り直す.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="targetLineLength"></param>
        void ChangeBoundaries(IList<Sentence> current, IList<int> targetLineLength);

        /// <summary>
        /// 単一のSentenceを指定した長さに切り詰め、残りは新規Sentenceとする.
        /// コーパス全体の文の数は+1される.
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="splitPos"></param>
        /// <returns></returns>
        Sentence SplitSentence(int sid, int splitPos);

        /// <summary>
        /// Sentenceとその次のSentenceをマージする.
        /// コーパス全体の文の数は-1される.
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="splitPos"></param>
        /// <returns></returns>
        void MergeSentence(int sid);

        /// <summary>
        /// Documentの持つTextに対して指定位置に文字列の挿入を行う
        /// </summary>
        /// <param name="docID"></param>
        /// <param name="pos"></param>
        /// <param name="stringToInsert"></param>
        [Obsolete("文内容の変更はSchema#8以降は許可されなくなりました.（以前はEOS除去のためにのみ使用されていました.）", true)]
        void InsertDocumentText(int docID, int pos, string stringToInsert);

        /// <summary>
        /// Documentの持つTextに対して指定位置から指定数の文字列を削除する
        /// </summary>
        /// <param name="docID"></param>
        /// <param name="pos"></param>
        /// <param name="lengthToDelete"></param>
        [Obsolete("文内容の変更はSchema#8以降は許可されなくなりました.（以前はEOS除去のためにのみ使用されていました.）", true)]
        void RemoveDocumentText(int docID, int pos, int lengthToDelete);

    }
}
