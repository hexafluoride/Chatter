using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Chatter
{
    public class MediaInfo
    {
        public static string RunScript(string path, string args)
        {
            ProcessStartInfo psi;
            if(IsRunningOnMono())
                psi = new ProcessStartInfo("bash", path + " " + args);
            else
                psi = new ProcessStartInfo("cmd", path + " " + args);

            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            var proc = Process.Start(psi);

            string output = proc.StandardOutput.ReadToEnd();

            return output;
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public static string ParseFFmpegOutput(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return "-";
            if (!output.Contains("Duration"))
                return "-";

            string resolution = "";
            string duration = "";

            duration = output.Split('\n').First(l => l.Trim().StartsWith("Duration:")).Split(',')[0].Trim().Split(' ')[1].Split('.')[0];

            if (output.Contains("Video"))
            {
                resolution = output.Split('\n').First(l => l.Contains("Video:")).Split(',').First(s => s.Contains('x') && s.All(c => char.IsNumber(c) || c == 'x' || char.IsWhiteSpace(c))).Trim();
                return string.Format("11Video file({{0}}, {0}, {1} long)", resolution, duration);
            }
            else
            {
                return string.Format("11Audio file({{0}}, {0} long)", duration);
            }
        }

        public static string GetMediaInfo(string path)
        {
            if (IsRunningOnMono())
                return ParseFFmpegOutput(RunScript("bash", "./fileinf.sh \"" + path + "\""));
            return ParseFFmpegOutput(RunScript("cmd", "/c C:\\fileinf.bat \"" + path + "\""));
        }
    }
}
