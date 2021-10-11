using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    public abstract class DictionaryAccessor : IDisposable
    {
        public abstract string Name { get; }

        public Uri Url { get; private set; }

        public bool CanSearchCompoundWord { get; private set; }

        public bool CanUpdateCompoundWord { get; private set; }

        protected DictionaryAccessor()
        {
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        public abstract bool IsConnected { get; }

        public static DictionaryAccessor Create(Dictionary dict)
        {
            DictionaryAccessor ctx = null;

            if (dict is Dictionary_DB)
            {
                ctx = new DictionaryAccessor_DB((Dictionary_DB)dict);
            }
            else if (dict is Dictionary_Cradle)
            {
                var cradle = (Dictionary_Cradle)dict;
                ctx = new DictionaryAccessor_Cradle(cradle);
                ctx.Url = cradle.Url;
            }
            ctx.CanSearchCompoundWord = dict.CanSearchCompoundWord;
            ctx.CanUpdateCompoundWord = dict.CanUpdateCompoundWord;

            return ctx;
        }

        public abstract IList<Lexeme> FindLexemeBySurface(string surface);

        public abstract IList<MWE> FindMWEBySurface(string surface);

        public abstract IList<MWE> FindMWEBySurface2(string surface);

        public abstract void RegisterMWE(MWE mwe);
    }
}
