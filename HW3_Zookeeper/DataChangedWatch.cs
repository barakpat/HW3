using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;
using Org.Apache.Zookeeper.Data;
using System.Threading;

namespace HW3_Zookeeper
{
    class DataChangedWatch : IWatcher
    {
        private ZooKeeper zk;
        private AutoResetEvent autoResetEvent;
        private String path;

        public DataChangedWatch(ZooKeeper zk, String path)
        {
            this.zk = zk;
            this.autoResetEvent = new AutoResetEvent(false);
            this.path = path;
            this.zk.Exists(this.path, this);
        }

        public bool Wait()
        {
            this.autoResetEvent.WaitOne();
            return true;
        }
        
        public void Process(WatchedEvent @event)
        {
            if (@event.Type == EventType.NodeDataChanged)
            {
                this.autoResetEvent.Set();
            }
            else
            {
                this.zk.Exists(this.path, this);
            }
        }
    }
}
