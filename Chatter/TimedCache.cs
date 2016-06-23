using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter
{
    public class TimedCache
    {
        public List<CacheItem> List = new List<CacheItem>();
        public static TimeSpan DefaultExpiry = TimeSpan.FromSeconds(30);

        public TimedCache()
        {
            Task.Factory.StartNew(PurgeLoop);
        }

        private void PurgeLoop()
        {
            while(true)
            {
                List.RemoveAll(item => item.Expired());
                Thread.Sleep(1000);
            }
        }

        public void Add(string id, string content, TimeSpan expiry)
        {
            var item = Get(id);

            if (item != null)
            {
                item = new CacheItem(id, content, expiry);
                return;
            }

            List.Add(new CacheItem(id, content, expiry));
        }

        public CacheItem Get(string id)
        {
            foreach (var item in List)
                if (item.ID == id)
                    return item;

            return null;
        }

        public bool GetAndExecute(string id, Action<CacheItem> action)
        {
            var item = Get(id);

            if (item == null)
                return false;

            action(item);
            return true;
        }
    }

    public class CacheItem
    {
        public string ID { get; set; }
        public string Content { get; set; }
        public DateTime Added { get; set; }
        public TimeSpan Expiry { get; set; }

        public CacheItem(string id, string content, TimeSpan expires)
        {
            ID = id;
            Content = content;
            Added = DateTime.Now;
            Expiry = expires;
        }

        public bool Expired()
        {
            return (Added.Add(Expiry)) < DateTime.Now;
        }
    }
}
