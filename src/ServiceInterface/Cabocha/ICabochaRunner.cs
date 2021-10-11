using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Cabocha
{
    public interface ICabochaRunner
    {
        IList<string> Parse(string input);
    }
}
