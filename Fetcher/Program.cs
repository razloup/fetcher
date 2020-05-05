using System.Configuration;
using log4net;

namespace Fetcher
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string CommitHashCmd = "https://api.github.com/repos@commits/master";
        private const string repoCmd = "https://github.com@archive/master.zip";

        public static void Main()
        {
            Utils.SetupLogger();

            var configPath = ConfigurationManager.AppSettings["configPath"];
            var config = Utils.ReadConfig(configPath);

            // Get last updated commit hash
            var splittedCommitCmd = CommitHashCmd.Split('@');
            var getCommitUrl = $"{splittedCommitCmd[0]}/{config.Repo}/{splittedCommitCmd[1]}";
            if (!Utils.TryGetCommitHash(getCommitUrl, out string commitHash))
                return;

            // Check if the last commit in the repo has changed since last time
            if (!string.IsNullOrWhiteSpace(config.LastHash) && config.LastHash.Equals(commitHash))
            {
                log.Debug("Repo has not changed since last checked");
                return;
            }

            // Get repo as zip
            var splittedRepoCmd = repoCmd.Split('@');
            var getRepoUrl = $"{splittedRepoCmd[0]}/{config.Repo}/{splittedRepoCmd[1]}";
            if (!Utils.TryGetRepoZip(getRepoUrl, config.RepoPath))
                return;

            if (config.Rpms.Count > 0)
            {
                // Get rpm packages to folder
                if (!Utils.TryGetRpms(config.Rpms, config.RpmsFolder))
                    return;

                // Check all rpms necessary are downloaded
                if (!Utils.CheckRpms(config.Rpms, config.RpmsFolder))
                    return;
            }

            // Create metadata file
            if (!Utils.TryCreateMetadata(config.MetadataPath, commitHash, config.Version, config.RpmsFolder))
                return;

            // Zip rpms 
            if (!Utils.TryZip(config.RpmsFolder, config.RpmsZipPath))
                return;


            // Zip binaries (includes repo.zip + rpms.zip + metadata.json)
            if (!Utils.TryZip(config.BinariesPath, $"{config.BinariesZipPath}\\{commitHash}.zip"))
                return;

            // Update configuration
            config.LastHash = commitHash;
            Utils.UpdateConfig(configPath, config);
        }
    }
}
