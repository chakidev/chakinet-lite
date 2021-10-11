using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using System.IO;
using ChaKi.Common.SequenceMatcher;

namespace ChaKi.Service.DependencyEdit
{
    public interface IDepEditService
    {
        /// <summary>
        /// 新しい文をDependencyTree Editの対象としてオープンする。
        /// </summary>
        /// <param name="cps">編集対象のコーパス</param>
        /// <param name="sid">編集対象の文番号</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">他の編集が完了していないうちに、二重に編集を行おうとした。</exception>
        Sentence Open(Corpus cps, int sid, UnlockRequestCallback unlockCallback);

        /// <summary>
        /// 中心語の開始文字位置（Document内絶対位置）
        /// Open後に設定する.
        /// </summary>
        int CenterWordStartAt { get; set; }

        /// <summary>
        /// Close直後に再度同じ文の編集を開始する。
        /// </summary>
        /// <param name="cps"></param>
        /// <param name="sid"></param>
        /// <returns></returns>
        Sentence ReOpen();

        /// <summary>
        /// 現在のTransactionを捨てて、新しいTransactionを開始する
        /// DB Exceptionが起きた場合に呼び出す
        /// </summary>
        void ResetTransaction();

        /// <summary>
        /// DBに存在するProjectをすべて得る.
        /// </summary>
        /// <returns></returns>
        IList<Project> RetrieveProjects();

        /// <summary>
        /// Projectなど、この編集作業に関わるEntityを用意する
        /// </summary>
        void SetupProject(int projID);

        /// <summary>
        /// m_Sentenceに関連した文節・係り受けを表すSegment, Linkのリストをすべて取得する.
        /// </summary>
        /// <param name="segs"></param>
        /// <param name="links"></param>
        void GetBunsetsuTags(out List<Segment> segs, out List<Link> links);

        /// <summary>
        /// m_Sentenceに関連する同格・並列Groupのリストをすべて取得する.
        /// </summary>
        /// <returns></returns>
        IList<Group> GetGroupTags();

        /// <summary>
        /// m_Sentenceに関連する、文節・同格・並列以外のSegmentのリストをすべて取得する.
        /// </summary>
        /// <returns></returns>
        IList<Segment> GetSegmentTags();

        /// <summary>
        /// m_Sentenceに関連する、文節に関わらないLinkのリストをすべて取得する.
        /// </summary>
        /// <returns></returns>
        IList<Link> GetLinkTags();

        /// <summary>
        /// m_Sentenceに関連する、"Nest"タグを持つSegmentのリストを取得する.
        /// </summary>
        /// <returns></returns>
        IList<Segment> GetNestTags();

        /// <summary>
        /// 現在の編集で用いられるTagSetを得る.
        /// </summary>
        /// <param name="segTags"></param>
        /// <param name="linkTags"></param>
        /// <param name="grpTags"></param>
        TagSet GetTagSet();

        /// <summary>
        /// アノテーションにコメントをセットする.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="c"></param>
        void ChangeComment(Annotation a, string c);

        /// <summary>
        /// 現在の状態をDBにコミットする。
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        void Commit();

        /// <summary>
        /// 現在の状態を捨てて、編集状態から抜ける。
        /// </summary>
        void Close();

        /// <summary>
        /// 現在の状態を捨てて、編集状態から抜ける。
        /// </summary>
        void Dispose();

        /// <summary>
        /// 指定した文節を指定した語位置で分離する。
        /// </summary>
        void SplitBunsetsu(int docid, int bpos, int epos, int spos);

        /// <summary>
        /// 指定した文節を次の文節と併合する。
        /// </summary>
        void MergeBunsetsu(int docid, int bpos, int epos, int spos);

        void ChangeLinkEnd(Link link, Segment oldseg, Segment newseg);

        void ChangeLinkTag(Link link, string oldtag, string newtag);

        /// <summary>
        /// 前回のCreateWordGroupと同じ種類のGroup Tagを生成する。
        /// </summary>
        /// <param name="range"></param>
        void CreateWordGroup(CharRange range);

        void CreateWordGroup(CharRange range, string newGroup);

        void AddItemToWordGroup(Group g, CharRange range);

        void RemoveItemFromWordGroup(Group grp, Segment seg);

        void RemoveItemFromWordGroupWithInOutLinks(Group grp, Segment seg);

        void ChangeWordGroupTag(Group grp, string oldTag, string newTag);

        void ChangeLexeme(int docid, int wpos, Lexeme newlex);

        void ChangeLexeme(int docid, int cpos, int newlexid);

        void CreateSegment(CharRange range, string tag);

        void DeleteSegment(Segment seg);

        void ChangeSegmentTag(Segment seg, string oldTag, string newTag);

        void DeleteSegmentWithInOutLinks(Segment seg);

        bool HasInOutLink(Segment seg);

        IOperation CreateMWEAnnotation(MatchingResult match, bool recordHistory);

        void CreateLink(Segment fromseg, Segment toseg, string tagname);

        void DeleteLink(Link link);

        /// <summary>
        /// 指定した語を指定した位置で分割し、それぞれに指定した表層形を持つデフォルトLexemeを割り当てる
        /// </summary>
        /// <returns></returns>
        void SplitWord(int docid, int bpos, int epos, int spos, string lex1surface, string lex2surface);

        /// <summary>
        /// 指定した語を次の語と併合し、指定した表層形を持つデフォルトLexemeを割り当てる
        /// </summary>
        void MergeWord(int docid, int bpos, int epos, int spos, string lexsurface);

        /// <summary>
        ///  Operationをまとめてヒストリに登録
        /// </summary>
        /// <param name="ops"></param>
        void PushHistories(IEnumerable<IOperation> ops);

        bool Undo();

        bool Redo();

        bool CanUndo();

        bool CanRedo();

        /// <summary>
        /// セーブできる状態であるか（＝Historが0位置以外か）
        /// </summary>
        /// <returns></returns>
        bool CanSave();

        /// <summary>
        /// セーブ不要の状態にする（Historyを0位置にする）
        /// </summary>
        void CannotSave();

        Corpus GetCorpus();

        string GetScriptingStatements();

        void WriteToDotFile(TextWriter wr);

        string SegmentToString(Segment seg);

        /// <summary>
        /// 参照用辞書を追加する
        /// </summary>
        /// <param name="cps"></param>
        /// <param name="sid"></param>
        void AddReferenceDictionary(Dictionary dict);

        /// <summary>
        /// Cabocha解析結果を用いて、現在アサインされている文節・係り受けをすべて修正する.
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="input"></param>
        void UpdateAllBunsetsu(Sentence sen, string input);

        /// <summary>
        /// 現在のSentenceの持つ "MWE"Group annotationおよびその範囲に付与された係り受けをすべて取得し、
        /// MWEオブジェクトに変換して返す.
        /// </summary>
        /// <param name="foundMWECallback"></param>
        /// <returns></returns>
        List<MWE> FindMWEAnnotations(Action<MWE, List<int>, List<int>> foundMWECallback);

        /// <summary>
        /// Cradle辞書にMWEを登録する.
        /// </summary>
        /// <param name="mwe"></param>
        /// <param name="deps"></param>
        void RegisterMWEToDictionary(MWE mwe);

        /// <summary>
        /// DBのTransaction Savepointを作る
        /// </summary>
        /// <param name="v"></param>
        void CreateSavePoint(string spname);

        /// <summary>
        /// opsを逆順にすべてUnExecute(Undo)する.
        /// </summary>
        /// <param name="v"></param>
        void Unexecute(List<IOperation> ops);

        /// <summary>
        /// SavepointをReleaseする
        /// </summary>
        /// <param name="v"></param>
        void ReleaseSavePoint(string spname);

    }
}

