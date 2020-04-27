using log4net;
using log4net.Repository;
using Nett;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Fetcher
{
    public class Utils
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static async Task<byte[]> GetRequestByteArray(string url)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(url);
        }

        public static string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.UserAgent = "Github commit fetch";

            using var response = (HttpWebResponse)request.GetResponse();
            using Stream stream = response.GetResponseStream();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string Bash(string cmd)
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

        public static bool TryGetRpms(List<string> rpms, string folder)
        {
            foreach (var rpm in rpms)
            {
                try
                {
                    Bash($"Yumdownloader {rpm} --destdir {folder} --resolve");
                }
                catch (Exception ex)
                {
                    log.Error($"Error in {rpm} rpm package download ");
                    log.Error(ex.Message);
                    return false;
                }
            }
            return true;
        }

        public static bool CheckRpms(List<string> rpms, string rpmsFolder)
        {
            string result;
            try
            {
                result = Bash($"ls -l {rpmsFolder}");
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return false;
            }
            foreach (var rpm in rpms)
            {
                if (!result.Contains(rpm))
                {
                    log.Error($"Not all rpms downloaded correctly");
                    return false;
                }
            }
            return true;
        }

        public static Configuration ReadConfig(string configPath)
        {
            var configFile = Toml.ReadFile(configPath);
            var config = configFile.Get("Configuration");
            log.Debug($"configuration file at {configPath} has been red");
            return config.Get<Configuration>();
        }

        public static void UpdateConfig(string configPath, Configuration newConfig)
        {
            var configFile = Toml.ReadFile(configPath);
            configFile.Update("Configuration", newConfig);
            Toml.WriteFile(configFile, configPath);
            log.Debug($"Commit hash updated in configuration file: {configPath}");
        }

        public static bool TryGetRepoZip(string url, string repoPath)
        {
            try
            {
                var contents = GetRequestByteArray(url).Result;
                File.WriteAllBytes(repoPath, contents);
                log.Debug($"Downloaded repo as zip in {repoPath}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error("Error in repo download");
                log.Error(ex.Message);
                return false;
            }
        }

        public static bool TryGetCommitHash(string url, out string hash)
        {
            try
            {
                var responseString = Get(url);
                var json = JObject.Parse(responseString);
                hash = json["sha"].ToString();
                log.Debug($"Repos last commit: {hash}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                hash = string.Empty;
                return false;
            }
        }

        public static void SetupLogger()
        {
            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());

            var fileInfo = new FileInfo(@"log4net.config");

            log4net.Config.XmlConfigurator.Configure(repository, fileInfo);
        }
    }
}
