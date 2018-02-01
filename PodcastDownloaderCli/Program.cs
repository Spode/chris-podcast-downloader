using System;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;

namespace PodcastDownloaderCli
{
    class Program
    {
        public static string root = GetRootPath();

        static void Main(string[] args)
        {
            string file = "podcasts.txt";
            string[] lines = ReadFile(file);

            foreach (string line in lines)
            {
                ParseLine(line);
            }
        }

        private static string[] ReadFile(string podcastFile)
        {
            string[] podcastLines = File.ReadAllLines(podcastFile);

            return podcastLines;
        }

        private static void ParseLine(string line)
        {
            Console.WriteLine("Parsing line: " + line);
            XmlReader xmlreader = XmlReader.Create(line);
            SyndicationFeed feed = SyndicationFeed.Load(xmlreader);
            string podcast = feed.Title.Text;
            xmlreader.Close();
           
            string curPath = Path.Combine(root, podcast);
            if (!Directory.Exists(curPath))
            {
                Console.WriteLine("Creating folder: " + curPath);
                Directory.CreateDirectory(curPath);
            }

            SyndicationItem feedItem = feed.Items.ElementAt(0);
            Uri uri = feedItem.Links[1].Uri;
            string uriString = feedItem.Links[1].Uri.ToString();
            var last = uriString.Split('/').Last();
            string fileName = "";

            if (last.Contains("?"))
            {
                fileName = last.Split('?')[0];
            }
            else
            {
                fileName = last;
            }

            string filePath = Path.Combine(curPath, fileName);

            if (!File.Exists(curPath))
            {
                WebClient client = new WebClient();
                client.DownloadFile(uri, filePath);
                Console.WriteLine("New episode found! Downloading to: " + curPath);
                client.Dispose();
            }
            else // if filename exist, check size of file
            {
                Console.WriteLine("File exists! Checking local file size with buffer");
                WebClient client = new WebClient();
                long length = new System.IO.FileInfo(curPath).Length;
                Stream myStream = client.OpenRead(uriString);
                Int64 bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                myStream.Close();

                if (bytes_total != length)
                {
                    string uniqueTitle = Path.Combine(podcast, feedItem.PublishDate.ToString("yyyy-MM-dd "), fileName);

                    if (!File.Exists(uniqueTitle))
                    {
                        Console.WriteLine("New episode! Downloading to: " + uniqueTitle);
                        client.DownloadFile(uri, uniqueTitle);
                    }
                }
                else
                {
                    Console.WriteLine("No new episode!");
                }
                client.Dispose();
            }
        }

        private static string GetRootPath()
        {
            string[] config = File.ReadAllLines("config.txt");
            string rootLine = config[0];
            string rootPathSubstr = "root=";
            string rootPath = rootLine.Substring(rootPathSubstr.IndexOf(rootPathSubstr) + rootPathSubstr.Length);

            return rootPath;
        }
    }
}
