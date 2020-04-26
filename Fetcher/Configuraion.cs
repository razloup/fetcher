using System.Collections.Generic;

namespace Fetcher
{
    public class Configuraion
    {
        public string LastHash { get; set; }
        public string Repo { get; set; }
        public List<string> Rpms { get; set; }
        
        public Configuraion()
        {

        }

        public Configuraion(string lastHash, string repo, List<string> rpms)
        {
            LastHash = lastHash;
            Repo = repo;
            Rpms = new List<string>(rpms);
        }
    }
}
