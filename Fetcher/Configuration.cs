using System.Collections.Generic;

namespace Fetcher
{
    public class Configuration
    {
        public string LastHash { get; set; }
        public string Repo { get; set; }
        public List<string> Rpms { get; set; }
        public string RpmsFolder { get; set; }
        public string RepoPath { get; set; }
        public string RpmsZipPath { get; set; }
        public string BinariesPath { get; set; }
        public string BinariesZipPath { get; set; }


        public Configuration()
        {

        }

        public Configuration(string lastHash, string repo, List<string> rpms)
        {
            LastHash = lastHash;
            Repo = repo;
            Rpms = new List<string>(rpms);
        }
    }
}
