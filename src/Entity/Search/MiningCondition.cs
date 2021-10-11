using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class MiningCondition : ISearchCondition, ICloneable
    {
        public MiningCondition()
        {
            Reset();
        }

        public MiningCondition(MiningCondition org)
        {
        }

        public object Clone()
        {
            return new MiningCondition(this);
        }

        public void Reset()
        {
        }

    }
}
