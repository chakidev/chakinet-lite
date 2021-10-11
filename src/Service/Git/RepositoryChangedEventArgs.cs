using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Git
{
    public class RepositoryChangedEventArgs : EventArgs
    {
        public string RepositoryName { get; private set; }

        public string FileName { get; private set; }

        public RepositoryChangedEventArgs(string repoName, string file)
        {
            this.RepositoryName = repoName;
            this.FileName = file;
        }
    }
}
