﻿using System;
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

			Console.WriteLine("Would you like to include bot games? Y/N: ");
			var yn = Console.ReadLine();
			var botgames = yn == "Y" || yn == "y";

            Console.WriteLine("Collecting your games, this may take a little while, we don't want to hit the server too hard!");

            try
            {
                var pid = GetPlayerID(un);
                var games = PlayerGameList(pid, count, botgames);

                for (var i=0;i<games.Count();i++)
                {
					var sgf = DownloadSGF(games[i].Value);
                    Thread.Sleep(500);
					File.WriteAllText(games[i].Key + ".sgf",sgf);
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
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (o,c,ch,er) => true;
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

		public static KeyValuePair<string,string>[] PlayerGameList(string id, int count, bool bots)
        {
            var page = 1;

			var gameList = new List<KeyValuePair<string,string>>();

            var gameCount = 0;

            while (count > gameCount)
            {
                Thread.Sleep(500);

                var url = "http://online-go.com/api/v1/players/" + id + "/games?ordering=-id&page=" + page;
                var ds = JsonGet(url);
                var games = ds["results"];

                foreach (var g in games.Children())//.Select(g => g["id"].ToString()).TakeWhile(gid => count > gameCount))
                {
                    if (count <= gameCount) break;
                    var gid = g["id"].ToString();
                    if (g["ended"].ToString() == "") continue;
					if(!bots){
					if (g ["players"] ["white"] ["ui_class"].ToString () == "bot") continue;
					if (g ["players"] ["black"] ["ui_class"].ToString () == "bot") continue;
					}

					string title = "";
					title += g["players"]["black"]["username"];
					title += " vs ";
					title += g["players"]["white"]["username"];
					var winner = g ["black_lost"].Value<bool>() ? " W+" : " B+";
					title += winner + g ["outcome"].ToString();

					gameCount++;
					gameList.Add(new KeyValuePair<string,string>(title,gid));
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
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (o,c,ch,er) => true;
            var wr = WebRequest.Create(url);

            var hr = (HttpWebResponse)wr.GetResponse();
            var stream = hr.GetResponseStream();
            var reader = new StreamReader(stream);
            return reader;
        }
    }
}
