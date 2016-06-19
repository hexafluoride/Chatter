using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chatter
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            string join_default = "#topkek-test";

            QuestionAggregator aggre = new QuestionAggregator();
            aggre.Load("/home/hexafluoride/log-4"); // replace this with your IRC log

            IrcClient client = new IrcClient();
            client.Connect();

            Random rnd = new Random();

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

                    if (message.StartsWith(".answer"))
                    {
                        string question = message.Substring(".answer".Length).Trim();
                        var pair = aggre.GetPair(question);

                        if (pair != null && pair.Response != null)
                        {
                            client.SendPrivateMessage(target, pair.Response.Content.Replace("{0}", source));
                            conversing_with = source;
                            if(rnd.NextDouble() > 0.5)
                                conversation_length = 1;
                            last_conversed = DateTime.Now;
                        }
                    }
                    else if (message.Contains("topkek_2003"))
                    {
                        var pair = aggre.GetPair(message);

                        if (pair != null && pair.Response != null)
                        {
                            client.SendPrivateMessage(target, pair.Response.Content.Replace("{0}", source));
                            conversing_with = source;
                            if(rnd.NextDouble() > 0.7)
                                conversation_length = 1;
                            last_conversed = DateTime.Now;
                        }
                    }
                    else if (new LogLine() { Content = message }.IsQuestion())
                    {
                        if (rnd.NextDouble() < 0.05)
                        {
                            var pair = aggre.GetPair(message);

                            if (pair != null && pair.Response != null)
                            {
                                client.SendPrivateMessage(target, pair.Response.Content.Replace("{0}", source));
                                conversing_with = source;
                                if(rnd.NextDouble() > 0.5)
                                    conversation_length = 1;
                                last_conversed = DateTime.Now;
                            }
                        }
                    }
                    else if (conversation_length > 0 && (DateTime.Now - last_conversed).TotalSeconds < 20)
                    {
                        if (source == conversing_with)
                        {
                            var pair = aggre.GetPair(message);

                            if (pair != null && pair.Response != null)
                            {
                                client.SendPrivateMessage(target, pair.Response.Content.Replace("{0}", source));

                                if (rnd.NextDouble() > 0.2)
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
    }
}
