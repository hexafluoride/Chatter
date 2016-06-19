using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chatter
{
    public class QuestionAggregator
    {
        public string Filename { get; set; }
        public List<QuestionResponse> Responses = new List<QuestionResponse>();
        public Dictionary<string, int> CommonNicks = new Dictionary<string, int>();
        public Random Random = new Random();

        public QuestionAggregator()
        {
        }

        public void Load(string filename)
        {
            Parse(filename);
            SanitizeNicks();

            CommonNicks.Clear();
        }

        public void Parse(string filename)
        {
            var lines = File.ReadLines(filename).Count();
            int current = 0;
            int meaningful = 0;
            int matched = 0;
            int answers = 0;

            StreamReader sr = new StreamReader(filename);

            int question_scope = 0;
            LogLine last_question = null;

            while (!sr.EndOfStream)
            {
                var line = LogLine.Parse(sr.ReadLine());
                current++;

                if (current % 100 == 0)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("{0:0.00}% done, {3} matched lines, {6} answers, {4} meaningful lines({5:0.00}%), {1}/{2}", ((double)current / (double)lines) * 100d, current, lines, matched, meaningful, ((double)meaningful / (double)lines) * 100d, answers);
                }

                if (line == null)
                    continue;

                meaningful++;
                if (question_scope > 0 &&
                    line.Nick != last_question.Nick &&
                    !line.IsQuestion())
                {
                    question_scope--;
                    if (line.Content.Contains(last_question.Nick, StringComparison.InvariantCultureIgnoreCase))
                    {
                        answers++;
                        line.Content = line.Content.Replace(last_question.Nick, "{0}", StringComparison.InvariantCultureIgnoreCase);
                        Responses.Add(new QuestionResponse(last_question, line));
                    }
                }

                if (!CommonNicks.ContainsKey(line.Nick))
                    CommonNicks[line.Nick] = 1;
                else
                    CommonNicks[line.Nick]++;

                if (!line.IsQuestion() && Random.NextDouble() < 0.995)
                    continue;

                question_scope = 5;
                last_question = line;
                matched++;
                //Console.WriteLine(line);
                //Console.ReadKey();
            }
        }

        public void SanitizeNicks()
        {
            int nicks_max = CommonNicks.Values.Max();
            var common_nicks_list = CommonNicks
                .Where(p => p.Value > nicks_max / 1000 && p.Key.Length > 3)
                .Select(p => p.Key)
                .OrderByDescending(n => n.Length)
                .ToList();

            Console.WriteLine("{0} common nicks.", common_nicks_list.Count);

            int processed_pairs = 0;
            String lock_str = "";

            Parallel.ForEach(Responses, pair =>
            {
                int index = 0;
                processed_pairs++;

                if(pair.Sanitized)
                    return;

                foreach (var nick in common_nicks_list)
                {
                    index++;
                    if (pair.Question.Content.Contains(nick, StringComparison.InvariantCultureIgnoreCase))
                        pair.Question.Content = pair.Question.Content.Replace(nick, "{r" + index + "}", StringComparison.InvariantCultureIgnoreCase);

                    if (pair.Response.Content.Contains(nick, StringComparison.InvariantCultureIgnoreCase))
                        pair.Response.Content = pair.Response.Content.Replace(nick, "{r" + index + "}", StringComparison.InvariantCultureIgnoreCase);
                }

                if (processed_pairs % 100 == 0)
                {
                    lock(lock_str)
                    {
                        Console.SetCursorPosition(0, 1);
                        Console.WriteLine("Sanitized {0} pairs out of {1}", processed_pairs, Responses.Count);
                    }
                }

                pair.Sanitized = true;
            });
        }

        public bool WordMatch(IEnumerable<string> list, string needle)
        {
            needle = new string(needle.Where(c => !char.IsPunctuation(c)).ToArray());

            return list.Contains(needle);
        }

        public QuestionResponse GetPair(string question)
        {
            var q_words = question.Split(' ').Select(w => new string(w.ToLower().Where(c => !char.IsPunctuation(c)).ToArray()));

            var viable = Responses.AsParallel().Select(r =>
            {
                var words = r.Question.Content.Split(' ').Select(w => w.ToLower());
                var matches = q_words.Where(word => WordMatch(words, word));
                return new KeyValuePair<QuestionResponse, double>(r, ((double)matches.Count() / (double)words.Count()) + matches.Count());
            }).Where(p => p.Value > 0.1);

            viable = viable.OrderByDescending(p => p.Value);
            viable = viable.Take(15);
            viable = viable.Select(p => new KeyValuePair<QuestionResponse, double>(p.Key, Math.Pow(p.Value, 3)));
            //var resps = responses.Where(r => r.Question.Content.Contains(question));

            Console.WriteLine("{0} viable responses, top 15:", viable.Count());
            foreach (var resp in viable.Take(15))
            {
                Console.WriteLine("    {0} (match value {1})", resp.Key.Question.Content, resp.Value);
            }

            Console.WriteLine("--------------------------------");
            Console.WriteLine();

            double sum = viable.Select(p => p.Value).Sum();
            double selected = Random.NextDouble() * sum;

            double current_rnd = 0;

            foreach (var pair in viable)
            {
                current_rnd += pair.Value;
                if (current_rnd > selected)
                {
                    Console.WriteLine("Selected question-response pair:");
                    Console.WriteLine(pair.Key.Question.Content);
                    Console.WriteLine(pair.Key.Response.Content);
                    Console.WriteLine();

                    return pair.Key;
                }
            }

            return null;
        }
    }
}

