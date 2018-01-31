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
        static void Main(string[] args)
        {
            string[] config = File.ReadAllLines("config.txt");
            string rootLine = config[0];
            string rootPathSubstr = "root=";
            string rootPath = rootLine.Substring(rootPathSubstr.IndexOf(rootPathSubstr) + rootPathSubstr.Length);
            Console.WriteLine("Root path config is: " + rootPath);

            string[] podcastLines = File.ReadAllLines("podcasts.txt");

            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);                
            }            

            foreach (string line in podcastLines)
            {
                string[] lineParts = line.Split(',');
                string downloadUrl = lineParts[1];
                Console.WriteLine("Parsing podcast: " + lineParts[0]);
                XmlReader reader = XmlReader.Create(downloadUrl);
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                string podcastNameFolder = feed.Title.Text;
                reader.Close();

                string curPath = Path.Combine(rootPath, podcastNameFolder);
                Console.WriteLine("Path is: " + curPath);

                if (!Directory.Exists(curPath))
                {                                       
                    Directory.CreateDirectory(curPath);                    
                }                

                SyndicationItem item = feed.Items.ElementAt(0);
                Uri uri = item.Links[1].Uri;
                string uriString = item.Links[1].Uri.ToString();
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

                string finalPath = Path.Combine(curPath,fileName);

                if (!File.Exists(finalPath))
                {
                    WebClient client = new WebClient();
                    client.DownloadFile(uri, finalPath);
                    Console.WriteLine("New episode found! Downloading to: " + finalPath);
                    client.Dispose();
                }
                else // if filename exist, check size of file
                {
                    Console.WriteLine("Checking file sizes with buffer");
                    WebClient client = new WebClient();
                    long length = new System.IO.FileInfo(finalPath).Length;
                    Stream myStream = client.OpenRead(uriString);                    
                    Int64 bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                    myStream.Close();

                    if (bytes_total != length)
                    {
                        Console.WriteLine("File size difference " + length + " vs " + bytes_total + ". Testing with unique name.");
                        string uniqueTitle = Path.Combine(podcastNameFolder, item.PublishDate.ToString("yyyy-MM-dd "), fileName);
                        
                        if (!File.Exists(uniqueTitle))
                        {
                            Console.WriteLine("Unique does not exist. Downloading to: " + uniqueTitle);
                            client.DownloadFile(uri, uniqueTitle);
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        Console.WriteLine("No new episode!");
                    }
                    client.Dispose();
                }              
            }

            Console.WriteLine("Finished downloading latest podcasts! Press any key.");
            Console.ReadKey();
        }
    }
}
