using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VttSrtConverter.Core;

namespace MSTV
{
    class Program
    {
        public static string GsessionCookie;
        public static List<string> VideoQueue = new List<string>();
        static ProgressBar progress;

        static void Main(string[] args)
        {
            loginCredentialsCheck();

            string url = "";
            //if no arguments then prompt for url and assign. no checks in place though
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a video or collection url.");
                url = Console.ReadLine();
            }
            // if arguments is equal to 1, then assume its a url, again, no checks.
            else if (args.Length == 1)
            {
                url = args[0];
            }
            //if more than one argument thow this message and close
            else if (args.Length > 1)
            {
                Console.WriteLine("To many arguments, please enter ONE video or collection url.");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                Environment.Exit(0);
            }

            //attempt login, issue with 422 error at the moment, for some reason although all login info would be correct, it would fail, so as a work around,
            //ill just retry loggin in until its sucessful
            Console.WriteLine("Logging in...");
            bool loginsuccess = false;
            while (loginsuccess == false)
            {
                loginsuccess = login();
            }

            //simple check to see if url is a single video or collection, probably not a great way of doing it. 
            if (url.Contains("/videos/"))
            {
                //single video
                downloadVideo(url);
            }
            else
            {
                //assume collection and parse
                CollectionParse(url, 1);

                //make sure some videos were actually found
                if (VideoQueue.Count == 0)
                {
                    Console.WriteLine("No videos found...");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine(VideoQueue.Count + " videos found");

                    //loop though each video in queue and download. 
                    foreach (var video in VideoQueue)
                    {
                        downloadVideo(video);
                    }
                }
            }

            Console.WriteLine("Downloads finished. Closing in 5 seconds.");
            Thread.Sleep(5000);
            Environment.Exit(0);
        }

        static bool login()
        {
            //deserialise login info
            string credjson = File.ReadAllText("credentials.json");
            Credentials credentials = JsonConvert.DeserializeObject<Credentials>(credjson);

            try
            {
                //make request to get auth code from login page
                HttpWebRequest getAuthRequest = (HttpWebRequest)WebRequest.Create("https://www.marthastewart.tv/login");
                getAuthRequest.CookieContainer = new CookieContainer();
                HttpWebResponse getAuthResponse = (HttpWebResponse)getAuthRequest.GetResponse();

                string sessionscookie = "";
                foreach (Cookie cook in getAuthResponse.Cookies)
                {
                    if (cook.Name == "_session")
                    {
                        sessionscookie = cook.Value;
                    }
                }

                var doc = new HtmlDocument();
                doc.Load(getAuthResponse.GetResponseStream());
                string loginauthtoken = doc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']").GetAttributeValue("content", null);

                //wait for a few seconds, random login issue where the server returns 422 error despite everything being ok. usually works after a few retries. 
                //im kinda assuming its a timing issue regarding the login form and what not, but i have no idea. 
                Thread.Sleep(3000);

                //send login request using auth code and credentials
                HttpWebRequest loginSessionRequest = (HttpWebRequest)WebRequest.Create("https://www.marthastewart.tv/login");
                loginSessionRequest.KeepAlive = true;
                loginSessionRequest.Headers.Add("Origin", @"https://www.marthastewart.tv");
                loginSessionRequest.ContentType = "application/x-www-form-urlencoded";
                loginSessionRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4143.0 Safari/537.36";
                loginSessionRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                loginSessionRequest.Headers.Add("Sec-Fetch-Site", @"same-origin");
                loginSessionRequest.Referer = "https://www.marthastewart.tv/login";
                loginSessionRequest.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
                loginSessionRequest.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
                loginSessionRequest.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
                loginSessionRequest.Headers.Set(HttpRequestHeader.Cookie, @"_device=Windows%3AChrome%3ABtsQwYHJ8W361SIgmE5X6Q; _session=" + sessionscookie + "; referrer_url=https%3A%2F%2Fwww.marthastewart.tv%2F");
                loginSessionRequest.AllowAutoRedirect = false;
                loginSessionRequest.Method = "POST";

                string body = @"email=" + credentials.email + "&authenticity_token=" + loginauthtoken + "&utf8=%E2%9C%93&password=" + credentials.password;
                byte[] postBytes = Encoding.UTF8.GetBytes(body);
                loginSessionRequest.ContentLength = postBytes.Length;
                Stream stream = loginSessionRequest.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                loginSessionRequest.CookieContainer = new CookieContainer();
                HttpWebResponse loginSessionResponse = (HttpWebResponse)loginSessionRequest.GetResponse();

                //loop through cookiees and get session cookie.
                foreach (Cookie cook in loginSessionResponse.Cookies)
                {
                    if (cook.Name == "_session")
                    {
                        GsessionCookie = cook.Value;
                    }
                }
                return true;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    Console.WriteLine("Login failed due to error 422, retrying.");
                    //I have no idea how to fix this 422 issue, if u know, please let me know, its annoying. 
                    return false;
                }
                else
                {
                    errorDump(ex);
                    throw;
                }
            }
        }

        static void downloadVideo(string url)
        {
            try
            {
                //make request to video page to get vhx embedded video url using login session cookie
                HttpWebRequest vhxRequest = (HttpWebRequest)WebRequest.Create(url);
                vhxRequest.Headers.Set(HttpRequestHeader.Cookie, @"_device=Windows%3AChrome%3ABtsQwYHJ8W361SIgmE5X6Q; _session=" + GsessionCookie + ";");
                HttpWebResponse vhxResponse = (HttpWebResponse)vhxRequest.GetResponse();
                var doc = new HtmlDocument();
                doc = new HtmlDocument();
                doc.Load(vhxResponse.GetResponseStream());
                string vhxembed = doc.DocumentNode.SelectSingleNode("//iframe").GetAttributeValue("src", null);

                //make sure filename is valid for windows. 
                string videoTitle = MakeValidFileName(doc.DocumentNode.SelectSingleNode("//h1[@class='head primary site-font-primary-color site-font-primary-family margin-bottom-small collection-title video-title']//strong").InnerText);
                Console.WriteLine("Downloading; " + videoTitle);

                //get vimeo link
                HttpWebRequest vimeoRequest = (HttpWebRequest)WebRequest.Create(vhxembed);
                HttpWebResponse vimeoResponse = (HttpWebResponse)vimeoRequest.GetResponse();
                doc = new HtmlDocument();
                doc.Load(vimeoResponse.GetResponseStream());
                string vhxjson = doc.DocumentNode.SelectSingleNode("//script[contains(.,'window.OTTData')]").InnerText.Replace("window.OTTData = ", "");
                VHX.json vhxobj = JsonConvert.DeserializeObject<VHX.json>(vhxjson);
                string vimeoLink = vhxobj.config_url;

                //create new progress bar.
                progress = new ProgressBar();

                //get video info and final mp4 link
                Task.Run(async () =>
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Headers["Referer"] = vhxembed;
                        client.DownloadProgressChanged += wc_DownloadProgressChanged;

                        string result = client.DownloadString(vimeoLink);
                        Vimeo.Json vimeojson = JsonConvert.DeserializeObject<Vimeo.Json>(result);

                        //check if video has subtitles, if so, download them and convert to srt file.
                        //i chose to convert them to srt simply because it seems more widely used than the original VTT or any other format.
                        //not all episodes have subtitles for reason, infact, earlier episodes seem to have subtitles
                        //and the latests episodes dont. #logic
                        if (vimeojson.request.text_tracks != null)
                        {
                            WebvttSubripConverter converter = new WebvttSubripConverter();
                            foreach (var subtrack in vimeojson.request.text_tracks)
                            {
                                using (WebClient subclient = new WebClient())
                                {
                                    String vttsub = subclient.DownloadString(subtrack.url);
                                    converter.ConvertToSubrip(vttsub, videoTitle + "." + subtrack.lang + ".srt");
                                }
                            }
                        }
                        
                        // used to find highest quality video available. probably can be done simpler
                        double max = vimeojson.request.files.progressive.Max(t => t.height);
                        int index = vimeojson.request.files.progressive.FindIndex(t => t.height == max);
                        string mp4Url = vimeojson.request.files.progressive[index].url;

                        //download mp4 file with name of episode if deosnt already exist.
                        //talking of names of episodes, wtf is up with this website and there naming conventions
                        //having shit like "MSL Season 7 Episode 433V" as the public title of a video is a peciulier move to say the least. 
                        //like what is that, msl?, season 7 makes sense, episode 433!!! There is not 433 episodes in season 7, and wtf is the "V" for?
                        //also. I noticed that tvdb only seems to have a half complete season one lisiting, the other seasons are blank, 
                        //naming conventions like that probably dont help the matter. 
                        //The closest thing this website has to actualy information about the episode is a hardcoded air date
                        //and a little bit of info about whats in the episode.  yay -_-

                        if (!File.Exists(videoTitle + ".mp4"))
                        {
                            await client.DownloadFileTaskAsync(mp4Url, videoTitle + ".mp4");
                        }
                        else
                        {
                            Console.WriteLine(videoTitle + ".mp4 already exists, skipping.");
                        }
                    }
                }).GetAwaiter().GetResult();
                Console.WriteLine();
            }
            catch (WebException ex)
            {
                errorDump(ex);
                throw;
            }

        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "");
        }

        static void CollectionParse(string pageUrl, int pagenum)
        {
            try
            {
                //fetch collection page as json
                WebClient client = new WebClient();
                client.Headers["Accept"] = "application/json, text/javascript, */*; q=0.01";
                client.Headers["X-Requested-With"] = "XMLHttpRequest";

                //get html from json file
                Collection pagejson = JsonConvert.DeserializeObject<Collection>(client.DownloadString(pageUrl + "?page=" + pagenum));
                string html = pagejson.partial;
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                //select and loop through all videos in html
                HtmlNodeCollection videoUrls = doc.DocumentNode.SelectNodes("//div[@class='browse-item-card']//div[@class='grid-item-padding']//a[@class='browse-item-link'][@href]");
                foreach (var video in videoUrls)
                {
                    //add video url to downloads list
                    VideoQueue.Add(video.GetAttributeValue("href", string.Empty));
                }

                //check if json load more is true, if it is, rerun this method with incremented page number to fetch all videos from collection.
                if (pagejson.load_more == true)
                {
                    CollectionParse(pageUrl, pagenum + 1);
                }
            }
            catch (WebException ex)
            {
                errorDump(ex);
                throw;
            }
        }

        static void loginCredentialsCheck()
        {
            //checks if a credentials file exists, if it doesnt, prompts for email / pass and saves it for future use
            if (!File.Exists("credentials.json"))
            {
                Console.WriteLine("Login info not found.");

                //get email
                Console.WriteLine("Please enter your MSTV email address");
                string email = Console.ReadLine();

                //get password
                Console.WriteLine("Please enter your MSTV password");
                string password = Console.ReadLine();
                
                //dump into local json
                string json = "{\"email\":\"" + email + "\",\"password\":\"" + password + "\"}";
                File.WriteAllText(@"credentials.json", json);

                Console.WriteLine("Created credentials.json file.");
            }
        }

        static void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progress.Report((double)e.ProgressPercentage / 100);
        }

        static void errorDump(WebException ex)
        {
            Console.WriteLine("An error has occured.");

            using (StreamWriter sw = new StreamWriter("error.txt", true))
            {
                sw.WriteLine("=============Error Logging ===========");
                sw.WriteLine("===========Start============= " + DateTime.Now);
                sw.WriteLine("Error Status Code: " + ex.Status);
                sw.WriteLine("Error Message: " + ex.Message);
                sw.WriteLine("Stack Trace: " + ex.StackTrace);
                sw.WriteLine("===========End============= " + DateTime.Now);
            }

            Console.WriteLine("Error saved to error.txt");
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }
}
