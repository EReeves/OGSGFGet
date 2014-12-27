using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OGSGFGet
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Input your username(case sensitive): ");
            var un = Console.ReadLine();
            Console.WriteLine("Input the amount of games to download(make sure you have this many!): ");
            var count = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Collecting your games, this may take a little while, we don't want to hit the server too hard!");

            try
            {
                var pid = GetPlayerID(un);
                var games = PlayerGameList(pid, count);

                for (var i=0;i<games.Count();i++)
                {
                    var sgf = DownloadSGF(games[i]);
                    Thread.Sleep(1000);
                    File.WriteAllText(games[i] + ".sgf",sgf);
                    Console.Clear();
                    Console.WriteLine("Games Collected " + (i+1) + " out of " + count);
                }

            }
            catch(Exception ex) { Console.WriteLine(ex); }



        }

        private static string DownloadSGF(string gid)
        {
            var url = "http://online-go.com/api/v1/games/" + gid + "/sgf";
            return WebRequestWrapper(url);
        }

        private static string WebRequestWrapper(string url)
        {
            var wr = WebRequest.Create(url);
            string result;

            using (var hr = (HttpWebResponse)wr.GetResponse())
            using (var stream = hr.GetResponseStream())
            using (var reader = new StreamReader(stream))
                result = reader.ReadToEnd();

            return result;
        }

        public static string GetPlayerID(string username)
        {
            var url = "http://online-go.com/api/v1/players?username=" + username;
            var ds = JsonGet(url);

            //Player not found : Player found
            return ds["results"][0]["id"] == null ? "" : ds["results"][0]["id"].ToString();
        }

        public static string[] PlayerGameList(string id, int count)
        {
            var page = 1;

            var gameList = new List<string>();

            var gameCount = 0;

            while (count > gameCount)
            {
                Thread.Sleep(1000);

                var url = "http://online-go.com/api/v1/players/" + id + "/games?ordering=-id&page=" + page;
                var ds = JsonGet(url);
                var games = ds["results"];

                foreach (var g in games.Children())//.Select(g => g["id"].ToString()).TakeWhile(gid => count > gameCount))
                {
                    if (count <= gameCount) break;
                    var gid = g["id"].ToString();
                    if (g["ended"].ToString() == "") continue;
                    gameCount++;
                    gameList.Add(gid);
                }
                page++;
            }

            return gameList.ToArray();
        }

        private static JToken JsonGet(string url)
        {
            using (var reader = WebRequestWrapperRaw(url))
            using (var jsonReader = new JsonTextReader(reader))
                return JToken.ReadFrom(jsonReader);
        }

        private static StreamReader WebRequestWrapperRaw(string url)
        {
            var wr = WebRequest.Create(url);

            var hr = (HttpWebResponse)wr.GetResponse();
            var stream = hr.GetResponseStream();
            var reader = new StreamReader(stream);
            return reader;
        }
    }
}
