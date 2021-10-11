using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Git
{
    public interface IGitService
    {
        string Name { get; }

        string RepositoryPath { get; }

        Func<string, Tuple<string, string>> CredentialProvider { get; set; }

        bool CheckRepositoryExists();

        void CreateRepository();

        void OpenRepository();

        void Commit(string message);

        bool CheckRemoteUrl(string url);

        string GetRemoteUrl();

        void SetRemoteUrl(string url);

        void SetIdentity(string name, string email);

        void GetIdentity(out string name, out string email);

        void Push();

        bool IsClean();
    }
}
