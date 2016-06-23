using System;
using System.Collections.Generic;
using System.Linq;

namespace Chatter
{
    public class ConfigStore
    {
        public List<Option> Options = new List<Option>();
        public bool ProtectAdded = true;

        public ConfigStore()
        {
        }

        public void Add(string name, object value)
        {
            Option option = new Option(name, value) { Protected = ProtectAdded };
            Options.Add(option);
        }

        public void Set(string name, object value)
        {
            var option = GetOption(name);
            option.Value = Convert.ChangeType(value, option.Value.GetType());
        }

        public void Remove(string name)
        {
            if (GetOption(name) != null)
                Options.Remove(GetOption(name));
        }

        public Option GetOption(string name)
        {
            return Options.FirstOrDefault(o =>
            {
                return o.Name == name;
            });
        }

        public object Get(string name)
        {
            return Options.FirstOrDefault(o =>
            {
                return o.Name == name;
            })?.Value;
        }
    }

    public class Option
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public bool Protected { get; set; }

        public Option(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    public interface IOption
    {
    }
}

