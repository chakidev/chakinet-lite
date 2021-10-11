using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using NHibernate;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Readers;
using System.Data;
using ChaKi.Common.Settings;
using System.IO;
using ChaKi.Common;
using System.Threading.Tasks;

namespace ChaKi.Service.Import
{
    public class ImportService : IImportService
    {
        public ImportService()
        {
        }

        public Task ImportAnnotationAsync(Corpus corpus, string file, int projid, IProgress progress)
        {
            return Task.Factory.StartNew(() =>
            {
                var ctx = SetupContext(corpus, projid);
                try
                {
                    // 先頭行にコメントがあるかを調べる
                    using (var sr = new StreamReader(file, Encoding.UTF8))
                    {
                        var line = sr.ReadLine();
                        if ((line != "## ExportBunsetsuSegmentsAndLinks" && GitSettings.Current.ExportBunsetsuSegmentsAndLinks)
                         || (line == "## ExportBunsetsuSegmentsAndLinks" && !GitSettings.Current.ExportBunsetsuSegmentsAndLinks))
                        {
                            // Bunsetsuが含まれない".ann"ファイルから文節を含めてインポートしようとした場合、
                            // Bunsetsuが含まれた".ann"ファイルから文節以外のアノテーションだけをインポートしようとした場合はエラーにする.
                            throw new Exception($"File({file}) does not contain BunsetsuSeg/Link. Check Git Settings: ExportBunsetsuSegmentsAndLinks.");
                        }
                    }
                    RemoveAnnotations(ctx, projid);
                    ctx.Session.Flush();
                    var rdr = new AnnotationReader(ctx, progress);
                    rdr.ReadFromFileSLA(file);
                    ctx.Trans.Commit();
                    ctx.Trans.Dispose();
                    ctx.Trans = null;
                }
                catch (Exception ex)
                {
                    ctx.Trans.Rollback();
                    ctx.Trans.Dispose();
                    ctx.Trans = null;
                    throw ex;
                }
                finally
                {
                    ctx.DBService.Unlock();
                    ctx.Dispose();
                    progress?.EndWork();
                }
            });
        }

        private static ImportOperationContext SetupContext(Corpus corpus, int projid)
        {
            var ctx = ImportOperationContext.Create(corpus.DBParam, UnlockRequestHandler);
            var q = ctx.Session.CreateQuery($"from Project p where p.ID={projid}");
            ctx.Proj = q.UniqueResult<Project>();
            var tset = ctx.Proj.TagSetList[0];
            q = ctx.Session.CreateQuery("from TagSetVersion v where v.ID=0");
            var tver = q.UniqueResult<TagSetVersion>();
            ctx.TagSet = tset;
            ctx.TSVersion = tver;

            // ユーザー認証（ProjectにCurrentUserが属しているかの確認）
            foreach (var u in ctx.Proj.Users)
            {
                if (u.Name == User.Current.Name && u.Password == User.Current.Password)
                {
                    ctx.User = u;
                }
            }
            return ctx;
        }

        private static bool UnlockRequestHandler(Type requestingService)
        {
            if (requestingService == typeof(IImportService))
            {
                // 自分自身からのUnlockリクエストは無視する.
                return true;
            }
            return false;
        }

        private void RemoveAnnotations(ImportOperationContext ctx, int projid)
        {
            string w1 = null;
            if (!GitSettings.Current.ExportBunsetsuSegmentsAndLinks)
            {
                var btag = FindBunsetsuTag(ctx);
                if (btag == null)
                {
                    throw new Exception("Bunsetsu tag not found");
                }
                // インポートデータに文節seg&linkが含まれていない場合は、それらを削除しないで保持する
                w1 = $"tag_definition_id<>{btag.ID}";
            }
            var w2 = $"project_id={projid}";

            IDbCommand q;
            using (q = ctx.Session.Connection.CreateCommand())
            {
                q.CommandText = $"DELETE FROM group_attribute WHERE {w2}";
                q.ExecuteNonQuery();
            }
            using (q = ctx.Session.Connection.CreateCommand())
            {
                q.CommandText = $"DELETE FROM group_member WHERE {w2}";
                q.ExecuteNonQuery();
            }
            using (q = ctx.Session.Connection.CreateCommand())
            {
                q.CommandText = $"DELETE FROM group_element WHERE {w2}";
                q.ExecuteNonQuery();
            }

            // 削除対象のLinkをリストする -- SQLiteではDELETEにJOINが使えないのでコードで絞り込む
            using (q = ctx.Session.Connection.CreateCommand())
            {
                if (w1 == null)
                {
                    q.CommandText =
                        "SELECT l.id FROM link l INNER JOIN segment s ON l.from_segment_id=s.id "
                        + $"WHERE l.{w2}";
                }
                else
                {
                    q.CommandText =
                        "SELECT l.id FROM link l INNER JOIN segment s ON l.from_segment_id=s.id "
                        + $"WHERE s.{w1} AND l.{w2}";
                }
                using (var rdr = q.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var l = rdr.GetInt64(0);
                        using (var q2 = ctx.Session.Connection.CreateCommand())
                        {
                            q2.CommandText = $"DELETE FROM link_attribute WHERE link_id={l}";
                            q2.ExecuteNonQuery();
                        }
                        using (var q2 = ctx.Session.Connection.CreateCommand())
                        {
                            q2.CommandText = $"DELETE FROM link WHERE id={l}";
                            q2.ExecuteNonQuery();
                        }
                    }
                }
            }
            // 削除対象のSegmentをリストする
            using (q = ctx.Session.Connection.CreateCommand())
            {
                if (w1 == null)
                {
                    q.CommandText = $"SELECT id FROM segment WHERE {w2}";
                }
                else
                {
                    q.CommandText = $"SELECT id FROM segment WHERE {w1} AND {w2}";
                }
                using (var rdr = q.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var l = rdr.GetInt64(0);
                        using (var q2 = ctx.Session.Connection.CreateCommand())
                        {
                            q2.CommandText = $"DELETE FROM segment_attribute WHERE segment_id={l}";
                            q2.ExecuteNonQuery();
                        }
                        using (var q2 = ctx.Session.Connection.CreateCommand())
                        {
                            q2.CommandText = $"DELETE FROM segment WHERE id={l}";
                            q2.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private static Tag FindBunsetsuTag(ImportOperationContext ctx)
        {
            IQuery query = ctx.Session.CreateQuery("from Tag tag where tag.Type='Segment' and tag.Name='Bunsetsu'");
            return query.UniqueResult<Tag>();
        }

    }
}
