using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace PodcastDownloaderCli
{
    class Program
    {
        public static string root = GetRootPath();
        public static int count = 0;
        public static List<string> Episodes = new List<string>();

        static void Main(string[] args)
        {
            string file = "podcasts.txt";
            string[] lines = ReadFile(file);
            

            foreach (string line in lines)
            {
                ParseLine(line);
            }

            if (Episodes.Count > 0) {

                Console.WriteLine("New episodes:");

                foreach (string item in Episodes)
                {
                    Console.WriteLine(item);
                }

                //Process.Start(root);

            }

            Console.WriteLine("Done! Press any key to exit program.");            
            Console.ReadKey();
            Process.Start(root);
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

            if (!File.Exists(filePath))
            {
                WebClient client = new WebClient();
                Console.WriteLine("New episode of {0} found! Downloading {1}", podcast, fileName);
                client.DownloadFile(uri, filePath);
                count++;
                client.Dispose();
                Episodes.Add(podcast + ": " + fileName);
            }
            else // if filename exist, check size of file
            {
                WebClient client = new WebClient();
                long length = new System.IO.FileInfo(filePath).Length;
                Stream myStream = client.OpenRead(uriString);
                Int64 bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                myStream.Close();

                if (bytes_total != length)
                {
                    //Console.WriteLine("File exists but difference in size! Checking with unique filename!");

                    string uniqueFilename = feedItem.PublishDate.ToString("yyyy-MM-dd ") + fileName;
                    string uniqueFilenamePath = Path.Combine(root, podcast, uniqueFilename);

                    if (!File.Exists(uniqueFilenamePath))
                    {
                        Console.WriteLine("New episode of {0} found! Downloading {1}!", podcast, uniqueFilename);                        
                        client.DownloadFile(uri, uniqueFilenamePath);
                        count++;
                        Episodes.Add(podcast + ": " + uniqueFilename);
                    } else
                    {
                        Console.WriteLine("No new episode found!");
                    }
                }
                else
                {
                    Console.WriteLine("No new episode found!");
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
