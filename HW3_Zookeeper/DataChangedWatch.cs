using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;
using Org.Apache.Zookeeper.Data;
using System.Threading;

namespace HW3_Zookeeper
{
    class DataChangedWatch : IWatcher, IDisposable
    {
        private ZooKeeper zk;
        private AutoResetEvent connectedSignal = new AutoResetEvent(false);
        private AutoResetEvent autoResetEvent;
        private String path;

//        public DataChangedWatch(String address, String path)
        public DataChangedWatch(ZooKeeper zk, String path)
        {
//            this.zk = new ZooKeeper(address, new TimeSpan(1, 0, 0, 0), this);
            this.zk = zk;
//            this.connectedSignal.WaitOne();
            this.autoResetEvent = new AutoResetEvent(false);
            this.path = path;
//            this.zk.Exists(this.path, true);
            this.zk.Exists(this.path, this);
        }

        public bool Wait()
        {
            this.autoResetEvent.WaitOne();
            return true;
        }
        
        public void Process(WatchedEvent @event)
        {
            Console.WriteLine(@event.State);
            Console.WriteLine(@event.Type);

            if (@event.State == KeeperState.SyncConnected && @event.Type == EventType.None)
            {
                this.connectedSignal.Set();
            }
            if (@event.Type == EventType.NodeDataChanged)
            {
                this.autoResetEvent.Set();
            }
            else
            {
//                this.zk.Exists(this.path, true);
                this.zk.Exists(this.path, this);
            }
        }

        public void Dispose()
        {
            //this.zk.Dispose();
        }
    }
}
