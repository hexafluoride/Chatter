using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace Chatter
{
    public class IrcClient
    {
        [DllImport("botty", EntryPoint = "connect_irc")]
        public static extern int ConnectIrc();

        [DllImport("botty", EntryPoint = "write_str")]
        public static extern void WriteString(int fd, string str);

        [DllImport("botty", EntryPoint = "read_str")]
        public static extern string ReadString(int fd);

        public int FileDescriptor { get; set; }

        public IrcClient()
        {
        }

        public void Connect()
        {
            FileDescriptor = IrcClient.ConnectIrc();
        }

        public void SendRawMessage(string raw)
        {
            if (!raw.EndsWith("\r\n"))
                raw += "\r\n";
            IrcClient.WriteString(FileDescriptor, raw);
        }

        public string ReceiveRawMessage()
        {
            return IrcClient.ReadString(FileDescriptor);
        }

        public void SendIrcMessage(IrcMessage message)
        {
            SendRawMessage(message.ToString());
        }

        public void SendPrivateMessage(string target, string message)
        {
            SendIrcMessage(new IrcMessage("PRIVMSG", target + " :" + message));
        }

        public void Join(string channel)
        {
            SendIrcMessage(new IrcMessage("JOIN", channel));
        }
    }

    public class IrcPrefix
    {
        public string User { get; set; }
        public string Host { get; set; }
        public string Nick { get; set; }

        public IrcPrefix(string raw)
        {
            if (raw.StartsWith(":"))
                raw = raw.Substring(1);

            Nick = raw.Split('!').First().Trim();
            raw = raw.Split('!').Last();
            User = raw.Split('@').First().Trim();
            raw = raw.Split('@').Last();
            Host = raw.Trim();
        }

        public override string ToString()
        {
            string ret = "";

            if (Nick != null)
                ret += Nick;
            if (User != null)
                ret += "!" + User;
            if (Host != null)
                ret += "@" + Host;

            return ret;
        }
    }

    public class IrcMessage
    {
        public IrcPrefix Prefix { get; set; }
        public int CommandId { get; set; }
        public string CommandName { get; set; }
        public string Parameters { get; set; }

        public IrcMessage(string raw)
        {
            string command_proto = "";
            var parts = raw.Split(' ');

            if (parts[0].StartsWith(":"))
            {
                Prefix = new IrcPrefix(parts[0]);
                command_proto = parts[1];
            }
            else
                command_proto = parts[0];

            int id = 0;

            if (!int.TryParse(command_proto, out id))
                CommandName = command_proto.Trim();
            else
                CommandId = id;

            Parameters = string.Join(command_proto, raw.Split(new string[]{command_proto}, StringSplitOptions.None).Skip(1)).Trim();
        }

        public IrcMessage(IrcPrefix prefix, string command, int command_id, string parameters)
        {
            Prefix = prefix;
            CommandName = command;
            CommandId = command_id;
            Parameters = parameters;
        }

        public IrcMessage(string command, string parameters) :
            this(null, command, 0, parameters)
        {
        }

        public IrcMessage(int command, string parameters) :
            this(null, "", command, parameters)
        {
        }

        public override string ToString()
        {
            string ret = "";

            if (Prefix != null)
                ret += Prefix.ToString() + " ";

            if (CommandId > 0)
                ret += CommandId;
            else
                ret += CommandName;

            ret += " ";
            ret += Parameters;

            return ret;
        }
    }
}

