using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace githupdater
{

    #region Generated JSON Classes
    public class Author
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }

    }

    public class Uploader
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }

    }

    public class Asset
    {
        public string url { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string name { get; set; }
        public object label { get; set; }
        public Uploader uploader { get; set; }
        public string content_type { get; set; }
        public string state { get; set; }
        public int size { get; set; }
        public int download_count { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string browser_download_url { get; set; }

    }

    public class Release
    {
        public string url { get; set; }
        public string assets_url { get; set; }
        public string upload_url { get; set; }
        public string html_url { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string tag_name { get; set; }
        public string target_commitish { get; set; }
        public string name { get; set; }
        public bool draft { get; set; }
        public Author author { get; set; }
        public bool prerelease { get; set; }
        public DateTime created_at { get; set; }
        public DateTime published_at { get; set; }
        public List<Asset> assets { get; set; }
        public string tarball_url { get; set; }
        public string zipball_url { get; set; }
        public string body { get; set; }

    }
    #endregion

    class Program
    {
        static void Main(string[] args)
        {
            string repo = "", exe = "", currentVersion = "";
            foreach (string line in File.ReadAllLines("version.txt"))
            {
                string attribute = line.Split(':')[0];
                string value = line.Split(':')[1];
                switch (attribute)
                {
                    case "github repository name":
                        repo = value.Trim();
                        break;
                    case "application name":
                        exe = value.Trim();
                        break;
                    case "current version":
                        currentVersion = value.Trim();
                        break;
                    case "do not check for updates":
                        if (value.Trim().ToLower() == "true" || value.Trim().ToLower() == "yes")
                            return; // quit
                        break;
                }
            }
            Console.WriteLine($"Checking updates for {repo}");

            // Make API request to github
            var latestRelease = GetLatestReleaseFromGithub(repo);
            if (latestRelease.tag_name == currentVersion)
                return; // no new release; quit
            Console.WriteLine($"New release found! (new: {latestRelease.tag_name} current: {currentVersion})");

            // Kill main application process
            Console.WriteLine($"Waiting for {exe} to close...");
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)))
                process.Kill();

            // Download zip file
            string archiveFile = DownloadLatestReleaseFiles(latestRelease);
            // Delete updater.exe from zip file
            DeleteUpdaterFromZipFile(archiveFile);
            // extract zip file into same directory as this .exe
            Console.WriteLine($"Extracting zip...");
            ExtractUpdates(archiveFile);

            Console.WriteLine($"Starting {exe}...");
            Process.Start(exe);
        }

        private static void ExtractUpdates(string archiveFile)
        {
            if (Directory.Exists("update_files"))
                Directory.Delete("update_files", true);
            ZipFile.ExtractToDirectory(archiveFile, "update_files");
            string updateFilesDir = Directory.GetDirectories("update_files")[0];
            MoveDirectory(updateFilesDir, ".");
            Directory.Delete("update_files", true);
            File.Delete(archiveFile);
        }
        #region Move Directory
        public static void MoveDirectory(string source, string target)
        {
            var stack = new Stack<Folders>();
            stack.Push(new Folders(source, target));

            while (stack.Count > 0)
            {
                var folders = stack.Pop();
                Directory.CreateDirectory(folders.Target);
                foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
                {
                    string targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
                    if (File.Exists(targetFile))
                    {
                        while (true)
                        {
                            try
                            {
                                File.Delete(targetFile);
                                break;
                            }
                            catch
                            {
                                Console.WriteLine($"Failed to delete {targetFile}. Retrying after 3 seconds...");
                                Thread.Sleep(3000);
                                continue;
                            }
                        }
                    }
                    File.Move(file, targetFile);
                }

                foreach (var folder in Directory.GetDirectories(folders.Source))
                {
                    stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }
            Directory.Delete(source, true);
        }
        public class Folders
        {
            public string Source { get; private set; }
            public string Target { get; private set; }

            public Folders(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }

        #endregion
        private static Release GetLatestReleaseFromGithub(string repo)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(@"https://api.github.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.DefaultRequestHeaders.Add("User-Agent", "FunOrange-Updater");

            Release latestRelease = null;
            try
            {
                var getTask = client.GetAsync($"/repos/{repo}/releases/latest");
                getTask.Wait();
                var response = getTask.Result;
                if (response.IsSuccessStatusCode)
                {
                    var readTask = response.Content.ReadAsStringAsync();
                    readTask.Wait();
                    string responseJson = readTask.Result;
                    return latestRelease = JsonConvert.DeserializeObject<Release>(responseJson);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Update check failed. Check for updates here: https://github.com/{repo}/releases/latest", "Error");
            }
            return null;
        }
        // Returns the file name of the zip containing update files
        private static string DownloadLatestReleaseFiles(Release latestRelease)
        {
            string downloadLink = latestRelease.assets[0].browser_download_url;
            var client = new WebClient();
            Console.WriteLine($"Downloading file: {downloadLink}...");
            client.DownloadFile(downloadLink, "update.zip");
            return "update.zip";
        }
        private static void DeleteUpdaterFromZipFile(string archiveFile)
        {
            // Limitation: updater cannot update itself
            var updaterFiles = new string[]
            {
                "updater.exe",
                "updater.pdb",
                "updater.exe.config",
                "Newtonsoft.Json.dll",
                "Newtonsoft.Json.xml",
            };
            using (FileStream fs = new FileStream(archiveFile, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Update))
            {
                bool complete = false;
                while (!complete)
                {
                    complete = true;
                    foreach (var item in archive.Entries)
                    {
                        Console.WriteLine(item);
                        if (updaterFiles.Contains(item.Name))
                        {
                            item.Delete();
                            complete = false;
                            break;
                        }
                    }
                }
            }
        }
    }
}
