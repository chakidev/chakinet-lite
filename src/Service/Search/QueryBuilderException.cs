using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Search
{
    public class QueryBuilderException : Exception
    {
        public QueryBuilderException(string msg)
            : base(msg)
        {
        }
    }
}
