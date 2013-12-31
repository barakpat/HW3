using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;
using System.Threading;

namespace HW3_Zookeeper
{
    class Barrier : IWatcher, IDisposable
    {
        private ZooKeeper zk;
        private AutoResetEvent connectedSignal = new AutoResetEvent(false);
        private String root;
        private String name;
        private byte[] data;
        private int size;
        
        private readonly Object mutex = new Object();

        public Barrier(String address, String root, String name, byte[] data, int size)
        {
            this.zk = new ZooKeeper(address, new TimeSpan(1, 0, 0, 0), this);
            this.connectedSignal.WaitOne();
            this.root = root;
            this.name = name;
            this.data = data;
            this.size = size;
       }

        public bool Enter()
        {
            zk.Create(this.root + "/" + this.name, this.data, Ids.OPEN_ACL_UNSAFE, CreateMode.Ephemeral);
            while (true)
            {
                lock (this.mutex)
                {
                    IEnumerable<String> list = this.zk.GetChildren(this.root, true);
                    if (list.Count() < this.size)
                    {
                        System.Threading.Monitor.Wait(this.mutex);
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        public bool Leave()
        {
            zk.Delete(this.root + "/" + this.name, -1);
            while (true)
            {
                lock (this.mutex)
                {
                    IEnumerable<String> list = this.zk.GetChildren(this.root, true);
                    if (list.Count() > 0)
                    {
                        System.Threading.Monitor.Wait(this.mutex);
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        public void Process(WatchedEvent @event)
        {
            if (@event.State == KeeperState.SyncConnected && @event.Type == EventType.None)
            {
                this.connectedSignal.Set();
            }
            if (@event.Type == EventType.NodeChildrenChanged)
            {
                lock (this.mutex)
                {
                    System.Threading.Monitor.Pulse(this.mutex);
                }

            }
        }

        public void Dispose()
        {
            this.zk.Dispose();
        }

    }
}
