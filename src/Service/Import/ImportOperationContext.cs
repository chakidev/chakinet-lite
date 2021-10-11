using ChaKi.Entity;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Import
{
    public class ImportOperationContext : OpContext
    {
        public TagSet TagSet { get; set; }
        public TagSetVersion TSVersion { get; set; }

        private ImportOperationContext()
            : base()
        {
            this.TagSet = null;
            this.TSVersion = null;
        }

        public static ImportOperationContext Create(DBParameter param)
        {
            return Create(param, null, typeof(IImportService));
        }

        public static ImportOperationContext Create(DBParameter param, UnlockRequestCallback unlockCallback)
        {
            return Create(param, unlockCallback, typeof(IImportService));
        }

        public new static ImportOperationContext Create(DBParameter param, UnlockRequestCallback callback, Type requestingService)
        {
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            var ctx = new ImportOperationContext();
            ctx.DBService = DBService.Create(param, callback, requestingService);
            ctx.Session = ctx.DBService.OpenSession();
            ctx.Trans = ctx.Session.BeginTransaction();
            return ctx;
        }
    }
}
