using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using Nett;
using System.IO;
using log4net;
using System;

namespace Fetcher
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static void Main()
        {
            const string repoSite = "https://github.com";
            var configPath = ConfigurationManager.AppSettings["configPath"];
            var config = ReadConfig(configPath);
            string masterHash;

            // Get last updated commit hash
            try
            {
                masterHash = Bash($"git ls-remote {repoSite}/{config.Repo}.git HEAD");
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return;
            }
            masterHash = masterHash.Split("  ")[0];
            if (!string.IsNullOrWhiteSpace(config.LastHash) && config.LastHash.Equals(masterHash))
                return;

            // Get repo as zip
            var contents = GetRequestByteArray($"{repoSite}/{config.Repo}/archive/master.zip").Result;
            File.WriteAllBytes(config.RepoPath, contents);
            log.Info("Downloaded repo as zip");

            // Get rpm packages to folder
            foreach (var rpm in config.Rpms)
            {
                try
                {
                    Bash($"Yumdownloader {rpm} --destdir {config.RpmsFolder} --resolve");
                }
                catch(Exception ex)
                {
                    log.Error(ex.Message);
                    return;
                }
            }
            // Zip all rpms 
            ZipFile.CreateFromDirectory(config.RpmsFolder, config.RpmsZipPath);
            log.Info("Zipped rpms");

            // Zip all binaries (includes repo.zip + rpms.zip)
            ZipFile.CreateFromDirectory(config.BinariesPath, config.BinariesZipPath);
            log.Info("Zipped binaries");

            // Update configuration
            config.LastHash = masterHash;
            UpdateConfig(configPath, config);
            log.Info("Commit hash updated in configuration");
        }

        private static Configuraion ReadConfig(string configPath)
        {
            var configFile = Toml.ReadFile(configPath);
            var config = configFile.Get("Configuration");
            return config.Get<Configuraion>();
        }

        private static void UpdateConfig(string configPath, Configuraion newConfig)
        {
            var configFile = Toml.ReadFile(configPath);
            configFile.Update("Configuration", newConfig);
            Toml.WriteFile(configFile, configPath);
        }

        private static async Task<byte[]> GetRequestByteArray(string url)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(url);
        }

        private static string Bash(string cmd)
        {
            string result = "";
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            using (process)
            {
                process.Start();
                result = process.StandardOutput.ReadToEnd();
            }

            return result;
        }
    }
}
