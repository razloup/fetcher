using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using Nett;
using System.IO;

namespace Fetcher
{
    public class Program
    {
        public static void Main()
        {
            var configPath = ConfigurationManager.AppSettings["configPath"];
            var repoPath = ConfigurationManager.AppSettings["repoPath"];
            var rpmsFolder = ConfigurationManager.AppSettings["rpmsFolder"];
            var rpmsZipPath = ConfigurationManager.AppSettings["rpmsZipPath"];
            var binariesPath = ConfigurationManager.AppSettings["binariesPath"];
            var binariesZipPath = ConfigurationManager.AppSettings["binariesZipPath"];
            var config = ReadConfig(configPath);
            var masterHash = Bash($"git ls-remote https://github.com/{config.Repo}.git HEAD");
            masterHash = masterHash.Split("  ")[0];
            //var jsonString = GetRepoJson(repoName, lastHash).Result;
            //var json = JObject.Parse(jsonString);
            //var date = json["commit"]["commiter"]["date"].Value<string>();
            //DateTime myDate = DateTime.ParseExact(date, "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(config.LastHash) && config.LastHash.Equals(masterHash))
                return;

            // Get repo as zip
            var contents = GetRequestByteArray($"https://github.com/{config.Repo}/archive/master.zip").Result;
            File.WriteAllBytes(repoPath, contents);

            // Get rpm packages and zip them together
            foreach (var rpm in config.Rpms)
            {
                Bash($"Yumdownloader {rpm} --destdir {rpmsFolder} --resolve");
            }
            ZipFile.CreateFromDirectory(rpmsFolder, rpmsZipPath);
            
            // Zip all binaries to one zip (includes repo.zip + rpms.zip)
            ZipFile.CreateFromDirectory(binariesPath, binariesZipPath);
            
            // Update configuration
            config.LastHash = masterHash;
            UpdateConfig(configPath, config);
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

        //private static async Task<string> GetRepoJson(string repoName, string lastCommit)
        //{
        //    HttpClient client = new HttpClient();
        //    var responseString = await client.GetStringAsync($"https://api.github.com/repos/{repoName}/commits/{lastCommit}");
        //    return responseString;
        //}

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
