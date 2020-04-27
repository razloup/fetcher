using System.IO.Compression;
using System.Configuration;
using log4net;

namespace Fetcher
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string getRepoCmdStart = "https://github.com";
        private const string getRepoCmdEnd = "archive/master.zip";
        private const string getCommitHashCmdStart = "https://api.github.com/repos";
        private const string getCommitHashCmdEnd = "commits/master";

        // TODO: Run as service every night
        public static void Main()
        {
            Utils.SetupLogger();

            var configPath = ConfigurationManager.AppSettings["configPath"];
            var config = Utils.ReadConfig(configPath);

            // Get last updated commit hash
            var getCommitUrl = $"{getCommitHashCmdStart}/{config.Repo}/{getCommitHashCmdEnd}";
            if (!Utils.TryGetCommitHash(getCommitUrl, out string commitHash))
                return;

            // Change check
            if (!string.IsNullOrWhiteSpace(config.LastHash) && config.LastHash.Equals(commitHash))
            {
                log.Debug("Repo has not changed since last checked");
                return;
            }

            // Get repo as zip
            var getRepoUrl = $"{getRepoCmdStart}/{config.Repo}/{getRepoCmdEnd}";
            if (!Utils.TryGetRepoZip(getRepoUrl, config.RepoPath))
                return;

            // Get rpm packages to folder
            if (!Utils.TryGetRpms(config.Rpms, config.RpmsFolder))
                return;

            // Check all rpms necessary are downloaded
            if (!Utils.CheckRpms(config.Rpms, config.RpmsFolder))
                return;

            // Zip all rpms 
            ZipFile.CreateFromDirectory(config.RpmsFolder, config.RpmsZipPath);
            log.Debug("Zipped rpms");

            // Zip all binaries (includes repo.zip + rpms.zip)
            ZipFile.CreateFromDirectory(config.BinariesPath, config.BinariesZipPath);
            log.Debug("Zipped binaries");

            // Update configuration
            config.LastHash = commitHash;
            Utils.UpdateConfig(configPath, config);
        }
    }
}
