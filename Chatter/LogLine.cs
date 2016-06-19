using System;
using System.Collections.Generic;
using System.Linq;

namespace Chatter
{
    public class LogLine
    {
        public string Time { get; set; }
        public string Nick { get; set; }
        public string Content { get; set; }

        public LogLine()
        {
        }

        static List<string> Months = new List<string>()
        {
            "Jan",
            "Feb",
            "Mar",
            "Apr",
            "May",
            "Jun",
            "Jul",
            "Aug",
            "Sep",
            "Oct",
            "Nov",
            "Dec"
        };

        public static LogLine Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            if (!Months.Any(month => line.StartsWith(month)))
                return null;

            line = line.Replace('\t', ' ');

            LogLine ret = new LogLine();

            var parts = line.Split(' ');
            ret.Time = string.Join(" ", parts.Take(3));
            ret.Nick = parts[3].Trim('<', '>');

            if (ret.Nick.StartsWith("*") || ret.Nick.StartsWith("-"))
                return null;
            
            ret.Content = string.Join(" ", parts.Skip(4));

            return ret;
        }

        private List<string> QuestionPrefixes = new List<string>()
        {
            "what",
            "why",
            "who",
            "when"
        };

        public bool IsQuestion()
        {
            return Content.EndsWith("?") || QuestionPrefixes.Any(prefix => Content.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase));
        }

        public override string ToString()
        {
            return string.Format("[LogLine: Time={0}, Nick={1}, Content={2}]", Time, Nick, Content);
        }
    }
}

