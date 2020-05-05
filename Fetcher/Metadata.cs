using System.Collections.Generic;

namespace Fetcher
{
    public class Metadata
    {
        public string Date { get; set; }
        public string Hash { get; set; }
        public string Version { get; set; }
        public Dictionary<string, string> Rpms { get; set; }

        public Metadata(string date, string hash, string version, Dictionary<string, string> rpms)
        {
            Date = date;
            Hash = hash;
            Version = version;
            Rpms = rpms;
        }
    }
}
