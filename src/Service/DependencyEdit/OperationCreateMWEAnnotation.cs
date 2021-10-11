using ChaKi.Common.SequenceMatcher;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    /// <summary>
    /// MWEアノテーションの作成
    /// Segment, Groupを生成するが、依存関係はLinkとしては生成しない.
    /// 依存関係はBunsetsu Segmentの係り受けの変更によって行う.
    /// </summary>
    internal class OperationCreateMWEAnnotation : Operation
    {
        private MatchingResult m_Match;

        private const string MWETag = "MWE";

        private List<ChangeLinkInfo> m_ChangeLinks = new List<ChangeLinkInfo>();

        public OperationCreateMWEAnnotation(MatchingResult match)
        {
            m_Match = match;
        }

        public override void Execute(DepEditContext ctx)
        {
            // 前提条件
            var n = m_Match.CharRangeList.Count;
            if (n != m_Match.MWE.Items.Count)
            {
                throw new Exception($"#Ranges={n} != #MWEItems={m_Match.MWE.Items.Count}");
            }
            //重複MWE Segmentのチェック
            for (var i = 0; i < m_Match.CharRangeList.Count; i++)
            {
                var range = m_Match.CharRangeList[i];
                var docid = ctx.Sen.ParentDoc.ID;
                var start = range.Start.Value + ctx.Sen.StartChar;
                var end = range.End.Value + ctx.Sen.StartChar;
                // 既にMWE Segmentが同じ個所にないかをチェック
                if (FindSegmentBetween(ctx, docid, start, end, MWETag) != null)
                {
                    throw new DuplicatedAnnotationException();
                }
            }

            // m_CharRangeにそれぞれMWE Segmnetを作成し、全体をGroupとする
            var bunsetsulist = new List<Segment>();
            var stag = ctx.Proj.FindTag(Tag.SEGMENT, MWETag);
            if (stag == null)
            {
                var t = new Tag(Tag.SEGMENT, MWETag) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(t);
                stag = t;
                ctx.Save(t);
            }
            // Groupを生成
            var grp = new Group();
            grp.Tag = ctx.Proj.FindTag(Tag.GROUP, MWETag);
            if (grp.Tag == null)
            {
                var t = new Tag(Tag.GROUP, MWETag) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(t);
                grp.Tag = t;
                ctx.Save(t);
            }
            grp.Proj = ctx.Proj;
            grp.User = ctx.User;
            grp.Version = grp.Tag.Version;
            ctx.Save(grp);
            var attr = new GroupAttribute()
            {
                Target = grp,
                Proj = ctx.Proj,
                User = ctx.User,
                Version = grp.Tag.Version,
                Key = "POS",
                Value = m_Match.MWE.Lex.PartOfSpeech.ToString(),
            };
            ctx.Save(attr);
            grp.Attributes.Add(attr);
            ctx.Flush();
            // Segmentを生成し、Groupにadd
            for (var i = 0; i < m_Match.CharRangeList.Count; i++)
            {
                var range = m_Match.CharRangeList[i];
                var docid = ctx.Sen.ParentDoc.ID;
                var start = range.Start.Value + ctx.Sen.StartChar;
                var end = range.End.Value + ctx.Sen.StartChar;

                var seg = new Segment();
                seg.Sentence = ctx.Sen;
                seg.Tag = stag;
                seg.Doc = ctx.Sen.ParentDoc;
                seg.StartChar = start;
                seg.EndChar = end;
                seg.Proj = ctx.Proj;
                seg.User = ctx.User;
                seg.Version = seg.Tag.Version;
                ctx.Save(seg);
                grp.Tags.Add(seg);
                // Placeholderの場合は、POS attributeを付加
                if (m_Match.MWE.Items[i].NodeType == MWENodeType.Placeholder)
                {
                    var segattr = new SegmentAttribute()
                    {
                        Target = seg,
                        Proj = ctx.Proj,
                        User = ctx.User,
                        Version = seg.Tag.Version,
                        Key = "POS",
                        Value = m_Match.MWE.Items[i].POS,
                    };
                    ctx.Save(segattr);
                    seg.Attributes.Add(segattr);
                }
                ctx.Flush();

                // 同じ範囲にあるBunsetsu Segmentを見つけておく
                var bunsetsu = FindSegmentBetween(ctx, docid, start, end);
                if (bunsetsu == null)
                {
                    throw new Exception($"No Bunsetsu found at: {range.ToString()}");
                }
                bunsetsulist.Add(bunsetsu);
            }
            if (bunsetsulist.Count != n)
            {
                throw new Exception($"Found only {bunsetsulist.Count} Bunsetsu. Expected:{n}");
            }
            ctx.Flush();

            // BunsetsuのLink(係り受け)をMWE.DepToに基づいて変更する.
            for (int i = 0; i < n; i++)
            {
                var mweitem = m_Match.MWE.Items[i];
                if (mweitem.DependsTo < 0 || mweitem.DependsTo >= m_Match.CharRangeList.Count)
                {
                    continue;
                }
                var buns = bunsetsulist[i];
                var link = FindLinkFrom(ctx, buns);
                var newrange = m_Match.CharRangeList[mweitem.DependsTo];
                var to_buns = FindSegmentBetween(ctx, buns.Doc.ID,
                    newrange.Start.Value + ctx.Sen.StartChar,
                    newrange.End.Value + ctx.Sen.StartChar);
                if (to_buns == null)
                {
                    throw new Exception($"No Bunsetsu found for {i}-th word: {newrange.ToString()}");
                }

                // Linkの操作情報を作成
                var cli = new ChangeLinkInfo(buns.Doc.ID,
                    link.From.StartChar, link.From.EndChar, link.From.Tag.Name,
                    link.To.StartChar, link.To.EndChar, link.To.Tag.Name,
                    to_buns.StartChar, to_buns.EndChar, to_buns.Tag.Name,
                    link.Tag.Name, mweitem.DependsAs);
                var link_modified = false;
                // Link ToSegの付け替え
                if (link.To.StartChar != to_buns.StartChar || link.To.EndChar != to_buns.EndChar)
                {
                    link.To = to_buns;
                    ctx.Flush();
                    //var q = ctx.Session.CreateSQLQuery($"UPDATE link SET to_segment_id={to_buns.ID} WHERE id={link.ID}");
                    //q.ExecuteUpdate();
                    link_modified = true;
                }
                // Link Tagの付け替え
                if (cli.OldLinkTagName != cli.NewLinkTagName
                 && (string.IsNullOrEmpty(cli.NewLinkTagName) || cli.NewLinkTagName != "_"))
                {
                    if (cli.NewLinkTagName == null)
                    {
                        cli.NewLinkTagName = string.Empty;
                    }
                    var newlinktag = ctx.Proj.FindTag(Tag.LINK, cli.NewLinkTagName);
                    if (newlinktag == null)
                    {
                        var t = new Tag(Tag.LINK, cli.NewLinkTagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                        ctx.Proj.TagSetList[0].AddTag(t);
                        newlinktag = t;
                        ctx.Save(t);
                    }
                    link.Tag = newlinktag;
                    ctx.Flush();
                    link_modified = true;
                }

                if (link_modified)
                {
                    this.m_ChangeLinks.Add(cli);
                }
            }
        }

        public override void UnExecute(DepEditContext ctx)
        {
            if (m_Match.CharRangeList.Count == 0)
            {
                throw new Exception("CharRangeList is empty.");
            }
            var segs = new HashSet<Segment>();
            foreach (var range in m_Match.CharRangeList)
            {
                var docid = ctx.Sen.ParentDoc.ID;
                var start = range.Start.Value + ctx.Sen.StartChar;
                var end = range.End.Value + ctx.Sen.StartChar;
                // この範囲にあるMWE Segmentを見つける
                Segment seg = null;
                try
                {
                    seg = FindSegmentBetween(ctx, docid, start, end, MWETag);
                }
                catch (Exception ex)
                {
                    throw new Exception("Duplicated segments disturb undoing!");
                }
                if (seg == null)
                {
                    throw new Exception($"No MWE Segment found at: {range.ToString()}");
                }
                segs.Add(seg);
            }
            // 最初のSegmentを含むGroupを見つける
            var grp = FindGroupCovering(ctx, segs, MWETag);
            if (grp == null)
            {
                throw new Exception($"No MWE Group found.");
            }
            foreach (var attr in grp.Attributes)
            {
                ctx.Delete(attr);
                ctx.Flush();
            }
            grp.Attributes.Clear();
            ctx.Delete(grp);
            foreach (var seg in segs)
            {
                foreach (var attr in seg.Attributes)
                {
                    ctx.Delete(attr);
                    ctx.Flush();
                }
                seg.Attributes.Clear();
                ctx.Delete(seg);
            }
            // Linkのつけなおし
            foreach (var cli in m_ChangeLinks)
            {
                var fromBuns = FindSegmentBetween(ctx, cli.DocId, cli.LinkFromSegStartPos, cli.LinkFromSegEndPos);
                if (fromBuns == null)
                {
                    throw new Exception($"No From-Bunsetsu found : {cli.NewFromSegStartPos}-{cli.NewFromSegEndPos}");
                }
                var oldToBuns = FindSegmentBetween(ctx, cli.DocId, cli.NewFromSegStartPos, cli.NewFromSegEndPos);
                if (oldToBuns == null)
                {
                    throw new Exception($"No To-Bunsetsu(old) found : {cli.NewFromSegStartPos}-{cli.NewFromSegEndPos}");
                }
                var newToBuns = FindSegmentBetween(ctx, cli.DocId, cli.NewToSegStartPos, cli.NewToSegEndPos);
                if (newToBuns == null)
                {
                    throw new Exception($"No To-Bunsetsu(new) found : {cli.NewToSegStartPos}-{cli.NewToSegEndPos}");
                }
                var link = FindLinkFrom(ctx, fromBuns);
                if (newToBuns == null)
                {
                    throw new Exception($"No Link found starting at {cli.LinkFromSegStartPos}-{cli.LinkFromSegEndPos}");
                }
                link.To = oldToBuns;
                ctx.Flush();
                var orglinktag = ctx.Proj.FindTag(Tag.LINK, cli.OldLinkTagName);
                if (orglinktag == null)
                {
                    var t = new Tag(Tag.LINK, cli.NewLinkTagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                    ctx.Proj.TagSetList[0].AddTag(t);
                    orglinktag = t;
                    ctx.Save(t);
                }
                link.Tag = orglinktag;
                ctx.Flush();
            }
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return "#svc.CreateMWEAnnotation() : not supported";
        }

        public override string ToString()
        {
            return "{{CreateMWEAnnotation}}";
        }
    }
}
