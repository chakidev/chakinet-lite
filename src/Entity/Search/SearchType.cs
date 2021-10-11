using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public enum SearchType
    {
        Undefined = 0,
        SentenceSearch,
        StringSearch,
        TagSearch,
        DepSearch,
        TagWordList,
        DepWordList,
        Collocation,
    }
}
