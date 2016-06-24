using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Web;

namespace Chatter
{
    class MainClass
    {
        public static Dictionary<string, DateTime> SeenNicks = new Dictionary<string, DateTime>();
        public static YoutubeResolver Youtube = new YoutubeResolver();
        static Random Random = new Random();
        static ConfigStore Config = new ConfigStore();

        public static void Main(string[] args)
        {
            Config.Add("parse_links", false);
            Config.Add("autojoin", true);
            Config.Add("respond_chance", 0.05);
            Config.Add("conversation_length", 2);
            Config.Add("conversation_decay_chance", 0.8);
            Config.Add("conversation_decay_time", 20);
            Config.Add("conversation_start_chance_highlight", 0.5);
            Config.Add("conversation_start_chance_command", 0.7);
            Config.Add("conversation_start_chance_question", 0.7);

            Config.ProtectAdded = false;

            string join_default = "#topkek-test";

            QuestionAggregator aggre = new QuestionAggregator();
            aggre.Load("/home/hexafluoride/log-4"); // replace this with your IRC log

            IrcClient client = new IrcClient();
            client.Connect();

            string conversing_with = "";
            int conversation_length = 0;
            DateTime last_conversed = DateTime.Now;

            while (true)
            {
                string raw = client.ReceiveRawMessage();
                IrcMessage msg = new IrcMessage(raw);

                if(msg.CommandId == 376)
                    client.Join(join_default);

                if (msg.CommandName == "PRIVMSG")
                {
                    string target = msg.Parameters.Split(' ')[0];
                    string message = string.Join(":", msg.Parameters.Split(':').Skip(1));
                    string source = msg.Prefix.Nick;
                    Console.WriteLine("{0} -> {1} from {2}", message, target, source);

                    if (!SeenNicks.ContainsKey(source))
                    {
                        Console.WriteLine("Added {0} to recent nick list.", source);
                    }

                    SeenNicks[source] = DateTime.Now;

                    bool authed = msg.Prefix.Host == "fluoride" && msg.Prefix.Nick == "h";

                    if (message.Contains("http") && ((bool)Config.Get("parse_links")))
                    {
                        try
                        {
                            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

                            Regex regex = new Regex(@"(?<Protocol>\w+):\/\/(?<Domain>[\w@][\w-.:@]+)\/?[\w\.~?=%&=\-@/$,]*");

                            if (regex.IsMatch(message))
                            {
                                string url = regex.Matches(message)[0].Value;

                                var task = LinkResolver.GetSummary(url);
                                task.Wait();
                                string summary = task.Result.Value;

                                if (summary != "-")
                                {
                                    summary = HttpUtility.HtmlDecode(summary);

                                    sw.Stop();

                                    client.SendPrivateMessage(target, string.Format("{0} ({1}s)", summary, sw.Elapsed.TotalSeconds.ToString("0.00")));
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    if (MatchesHealRequest(message))
                    {
                        string nick = msg.Prefix.Nick;

                        double rnd = Random.NextDouble();
                        if (message.ToLower().EndsWith("slut") || message.ToLower().EndsWith("whore") || message.ToLower().EndsWith("bitch"))
                        {
                            if (rnd < 0.9 && nick != "h")
                                client.SendAction(target, string.Format("h-heals {0} (3+{1} HP~!)", nick, Random.Next(90, 110)));
                            else
                                client.SendAction(target, string.Format("h-heals {0} (3+{1} HP~! Critical heal~!)", nick, Random.Next(250, 300)));
                        }
                        else
                        {
                            if (rnd < 0.9 && nick != "h")
                                client.SendAction(target, string.Format("heals {0} (3+{1} HP!)", nick, Random.Next(90, 110)));
                            else
                                client.SendAction(target, string.Format("heals {0} (3+{1} HP! Critical heal!)", nick, Random.Next(250, 300)));
                        }

                        return;
                    }
                    if (message.StartsWith(".yt") || message.StartsWith(".youtube"))
                    {
                        string rest = string.Join(" ", message.Split(' ').Skip(1));
                        var result = Youtube.Search(rest);

                        if (result != "-")
                        {
                            client.SendPrivateMessage(target, result);
                        }
                    }
                    if (message.StartsWith(".config"))
                    {
                        var words = message.Split(' ').Skip(1).ToList();
                        string name = words.Skip(1).FirstOrDefault();
                        string arg = string.Join(" ", words.Skip(2)).Trim();

                        if (words[0] == "get")
                        {
                            var value = Config.Get(name);
                            if (value != null)
                                client.SendPrivateMessage(target, name + " is " + value.ToString());
                            else
                                client.SendPrivateMessage(target, name + " doesn't exist. Try using .config set " + name + " <value>."); 
                        }
                        if (words[0] == "set")
                        {
                            var option = Config.GetOption(name);

                            if (option == null)
                                Config.Add(name, arg);
                            else if((option.Protected && authed) || !option.Protected)
                                Config.Set(name, arg);
                            else
                                client.SendPrivateMessage(target, name + " is a protected value!");
                        }
                        if (words[0] == "dump")
                        {
                            if (authed)
                            {
                                foreach (var option in Config.Options)
                                {
                                    client.SendPrivateMessage(target, option.Name + " is \"" + option.Value.ToString() + "\"" + (option.Protected ? " (protected)" : ""));
                                }
                            }
                        }
                    }
                    if (message.StartsWith(".answer"))
                    {
                        string question = message.Substring(".answer".Length).Trim();
                        var pair = aggre.GetPair(question);

                        if (pair != null && pair.Response != null)
                        {
                            client.SendPrivateMessage(target, Nickify(pair.Response.Content.Replace("{0}", source)));
                            conversing_with = source;
                            if(Random.NextDouble() < (double)Config.Get("conversation_start_chance_command"))
                                conversation_length = (int)Config.Get("conversation_length");
                            last_conversed = DateTime.Now;
                        }
                    }
                    else if (message.Contains("topkek_2003"))
                    {
                        var pair = aggre.GetPair(message);

                        if (pair != null && pair.Response != null)
                        {
                            client.SendPrivateMessage(target, Nickify(pair.Response.Content.Replace("{0}", source)));
                            conversing_with = source;
                            if(Random.NextDouble() < (double)Config.Get("conversation_start_chance_highlight"))
                                conversation_length = (int)Config.Get("conversation_length");
                            last_conversed = DateTime.Now;
                        }
                    }
                    else if (new LogLine() { Content = message }.IsQuestion())
                    {
                        if (Random.NextDouble() < (double)Config.Get("respond_chance"))
                        {
                            var pair = aggre.GetPair(message);

                            if (pair != null && pair.Response != null)
                            {
                                client.SendPrivateMessage(target, Nickify(pair.Response.Content.Replace("{0}", source)));
                                conversing_with = source;
                                if(Random.NextDouble() < (double)Config.Get("conversation_start_chance_question"))
                                    conversation_length = (int)Config.Get("conversation_length");
                                last_conversed = DateTime.Now;
                            }
                        }
                    }
                    else if (conversation_length > 0 && (DateTime.Now - last_conversed).TotalSeconds < (int)Config.Get("conversation_decay_time"))
                    {
                        if (source == conversing_with)
                        {
                            var pair = aggre.GetPair(message);

                            if (pair != null && pair.Response != null)
                            {
                                client.SendPrivateMessage(target, Nickify(pair.Response.Content.Replace("{0}", source)));

                                if (Random.NextDouble() < (double)Config.Get("conversation_decay_chance"))
                                    conversation_length--;
                            }
                        }
                    }
                    if (message.StartsWith(".join"))
                    {
                        client.Join(message.Substring(".join".Length).Trim());
                    }
                    if (message == ".stopfollow")
                    {
                        if (source == conversing_with)
                            conversing_with = "";
                    }
                }
                if (msg.CommandName == "PING")
                {
                    client.SendIrcMessage(new IrcMessage("PONG", msg.Parameters));
                }
            }

//            while (true)
//            {
//                var pair = aggre.GetPair(Console.ReadLine());
//                Console.WriteLine(pair.Response.Content);
//            }
        }

        static bool MatchesHealRequest(string msg)
        {
            msg = msg.ToLower();

            if (msg.Contains("heal"))
            {
                int heal_index = msg.IndexOf("heal");

                if (ContainsBefore(msg, "heal", "don't") || ContainsBefore(msg, "heal", "do not"))
                    return false;

                if (ContainsBefore(msg, "heal", "pls") ||
                    ContainsBefore(msg, "pls", "heal") ||
                    ContainsBefore(msg, "heal", "please") ||
                    ContainsBefore(msg, "me", "heal"))
                    return true;

                if (msg.Split(' ').Count() < 5 && msg.Split(' ').Any(w => w.ToLower() == "heal"))
                    return true;
            }
            else if (msg == "medic")
                return true;
            else if (msg.Contains(":(") || msg.Contains(";_;") || msg.Contains(";-;") || msg.Contains(";~;"))
                return true;

            return false;
        }

        static bool ContainsBefore(string haystack, string needle, string second)
        {
            if (!haystack.Contains(needle) || !haystack.Contains(second))
                return false;

            return haystack.IndexOf(needle) > haystack.IndexOf(second);
        }

        public static string Nickify(string text)
        {
            var temp = new List<KeyValuePair<string, DateTime>>();

            foreach (var pair in SeenNicks)
                if ((DateTime.Now - pair.Value).TotalHours > 1)
                    temp.Add(pair);

            foreach (var pair in temp)
            {
                Console.WriteLine("Removed {0} from seen nicks list.", pair.Key);
                SeenNicks.Remove(pair.Key);
            }

            var matches = Regex.Matches(text, "\\{r(.*?.*?.*?)\\}");

            List<string> strings = new List<string>();

            foreach (Match match in matches)
            {
                strings.Add(match.Groups[0].ToString());
            }

            strings = strings.Distinct().ToList();
            var nicks = SeenNicks.ToList();

            var str_new = strings.Select<string, KeyValuePair<string, string>>(s =>
            {
                if(!nicks.Any())
                {
                    if(!SeenNicks.Any())
                        return new KeyValuePair<string, string>("", "");
                    else
                        return new KeyValuePair<string, string>(s, SeenNicks.ElementAt(Random.Next(SeenNicks.Count)).Key);
                }

                var ret = nicks.First().Key;
                nicks.RemoveAt(0);
                return new KeyValuePair<string, string>(s, ret);
            });

            foreach (var pair in str_new)
            {
                if (pair.Key == "")
                    continue;

                text = text.Replace(pair.Key, pair.Value);
            }

            return text;
        }
    }
}
