using HW1c;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirlineServer
{
    class Cache
    {
        private static Object cacheLock = new Object();
        private const int maxSize = 100;
        private static Hashtable hash = new Hashtable();
        private static Queue queue = new Queue();

        private class CasheItem
        {
            public String key;
            public ConnectionFlights flights;
        }

        public void clearCache()
        {
            lock (cacheLock)
            {
                Console.WriteLine("cleare cache!");
                hash.Clear();
                queue.Clear();
            }
        }


        public void insertToCache(string query, ConnectionFlights flights)
        {
            CasheItem item = new CasheItem();
            item.key = query;
            item.flights = flights;

            lock (cacheLock)
            {
                if (maxSize >= 100)
                {
                    deleteOldest();
                }

                Console.WriteLine("query inserted into the cache!" );
                hash.Add(item.key, item);
                queue.Enqueue(item);
            }
        }

        public ConnectionFlights getFromCache(String query)
        {
            CasheItem result = null;
            String key = query;

            lock (cacheLock)
            {
                if (!hash.ContainsKey(key))
                    return null;
                    
                result = (CasheItem)hash[key];
            }
            Console.WriteLine("query result retrievded from the cache!");
            return result.flights;
        }   

        private void deleteOldest()
        {
            lock (cacheLock)
            {
                while (hash.Count >= maxSize)
                {
                    CasheItem item = (CasheItem)queue.Dequeue();
                    hash.Remove(item.key);
                }
            }
        }
    }
}

