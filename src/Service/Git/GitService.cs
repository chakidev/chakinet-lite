using ChaKi.Entity.Corpora;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Git
{
    public class GitService : IGitService
    {
        private Corpus m_Corpus;

        public string Name { get; private set; }

        public string RepositoryPath { get; private set; }

        private Repository m_Repository;

        private Identity m_User;

        public Func<string, Tuple<string, string>> CredentialProvider { get; set; }

        public bool CheckRepositoryExists()
        {
            return Directory.Exists(this.RepositoryPath);
        }

        public GitService(Corpus crps)
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new Exception("This OS is not supported by libgit2.");
            }
            this.m_Corpus = crps;
            this.Name = crps.Name;
            this.RepositoryPath = GitRepositories.GetRepositoryPath(this.Name);

            this.m_User = new Identity("default", "default");
        }

        public void CreateRepository()
        {
            if (!Directory.Exists(this.RepositoryPath))
            {
                Directory.CreateDirectory(this.RepositoryPath);
            }
            Repository.Init(this.RepositoryPath);
        }

        public void OpenRepository()
        {
            m_Repository = new Repository(this.RepositoryPath);
            string name, email;
            GetIdentity(out name, out email);
            if (string.IsNullOrEmpty(name))
            {
                name = "default";
            }
            if (string.IsNullOrEmpty(email))
            {
                email = "default";
            }
            this.m_User = new Identity(name, email);
        }

        public void Commit(string message)
        {
            var annFile = Path.Combine(this.RepositoryPath, this.m_Corpus.Name + ".ann");
            if (!File.Exists(annFile))
            {
                File.Create(annFile).Close();
            }
            Commands.Stage(m_Repository, annFile);
            var sig = new Signature(this.m_User, DateTime.Now);
            m_Repository.Commit(message, sig, sig);
        }

        public bool CheckRemoteUrl(string url)
        {
            try
            {
                var list = Repository.ListRemoteReferences(url,
                    (_url, usernameFromUrl, types) =>
                    {
                        if (types != SupportedCredentialTypes.UsernamePassword)
                        {
                            throw new Exception($"Unsupported CredentialType: {types}");
                        }
                        if (this.CredentialProvider != null)
                        {
                            var cred = this.CredentialProvider(_url);
                            return new UsernamePasswordCredentials()
                            {
                                Username = cred.Item1,
                                Password = cred.Item2
                            };
                        }
                        return null;
                    });
                return true;
            }
            catch(LibGit2SharpException)
            {
                return false;
            }
        }

        public void SetRemoteUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                m_Repository.Network.Remotes.Remove("origin");
            }
            else
            {
                m_Repository.Network.Remotes.Add("origin", url);
            }
        }

        public string GetRemoteUrl()
        {
            var rem = m_Repository.Network.Remotes.FirstOrDefault(r => r.Name == "origin");
            if (rem == null)
            {
                return string.Empty;
            }
            return rem.Url;
        }

        public void SetIdentity(string name, string email)
        {
            m_Repository.Config.Set("user.name", name);
            m_Repository.Config.Set("user.email", email);
            this.m_User = new Identity(name, email);
        }

        public void GetIdentity(out string name, out string email)
        {
            var nameent = m_Repository.Config.Get<string>("user.name");
            name = (nameent != null) ? nameent.Value : string.Empty;
            var emailent = m_Repository.Config.Get<string>("user.email");
            email = (emailent != null) ? emailent.Value : string.Empty;
        }

        public void Push()
        {
            var branch = m_Repository.Head;
            var options = new PushOptions()
            {
                CredentialsProvider = new CredentialsHandler(
                    (url, usernameFromUrl, types) =>
                    {
                        var pair = this.CredentialProvider(url);
                        if (pair != null)
                        {
                            return new UsernamePasswordCredentials() { Username = pair.Item1, Password = pair.Item2 };
                        }
                        throw new Exception("Failed to get credential.");
                    })
            };
            m_Repository.Network.Push(m_Repository.Network.Remotes["origin"],
                $"{branch.CanonicalName}:{branch.CanonicalName}", options);
        }

        public bool IsClean()
        {
            var annFile = Path.Combine(this.RepositoryPath, this.m_Corpus.Name + ".ann");
            var status = m_Repository.RetrieveStatus(annFile);
            return status == FileStatus.Unaltered;
        }
    }
}

