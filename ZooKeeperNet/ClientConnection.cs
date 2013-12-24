/*
 *  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */

using System.Net.Sockets;

namespace ZooKeeperNet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using log4net;
    using Org.Apache.Jute;
    using Org.Apache.Zookeeper.Proto;

    public class ClientConnection : IClientConnection
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ClientConnection));

        //TODO find an elegant way to set this parameter
        public const int packetLen = 4096 * 1024;
        internal static readonly bool disableAutoWatchReset = false;
        private static readonly TimeSpan defaultConnectTimeout = new TimeSpan(0, 0, 0, 0, 500);
        internal const int maxSpin = 30;

        //static ClientConnection()
        //{
        //    // this var should not be public, but otw there is no easy way
        //    // to test
        //    var zkSection = (System.Collections.Specialized.NameValueCollection)System.Configuration.ConfigurationManager.GetSection("zookeeper");
        //    if (zkSection != null)
        //    {
        //        Boolean.TryParse(zkSection["disableAutoWatchReset"], out disableAutoWatchReset);
        //    }
        //    if (LOG.IsDebugEnabled)
        //    {
        //        LOG.DebugFormat("zookeeper.disableAutoWatchReset is {0}",disableAutoWatchReset);
        //    }
        //    //packetLen = Integer.getInteger("jute.maxbuffer", 4096 * 1024);
        //    packetLen = 4096 * 1024;
        //}

        internal string hosts;
        internal readonly ZooKeeper zooKeeper;
        internal readonly ZKWatchManager watcher;
        internal readonly List<IPEndPoint> serverAddrs = new List<IPEndPoint>();
        internal readonly List<AuthData> authInfo = new List<AuthData>();
        internal TimeSpan readTimeout;
        
        private int isClosed;
        public bool IsClosed
        {
            get
            {
                return Interlocked.CompareExchange(ref isClosed, 0, 0) == 1;
            }
        }
        internal ClientConnectionRequestProducer producer;
        internal ClientConnectionEventConsumer consumer;


        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnection"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="zooKeeper">The zoo keeper.</param>
        /// <param name="watcher">The watch manager.</param>
        public ClientConnection(string connectionString, TimeSpan sessionTimeout, ZooKeeper zooKeeper, ZKWatchManager watcher):
            this(connectionString, sessionTimeout, zooKeeper, watcher, 0, new byte[16], defaultConnectTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnection"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="zooKeeper">The zoo keeper.</param>
        /// <param name="watcher">The watch manager.</param>
        /// <param name="connectTimeout">Connection Timeout.</param>
        public ClientConnection(string connectionString, TimeSpan sessionTimeout, ZooKeeper zooKeeper, ZKWatchManager watcher, TimeSpan connectTimeout) :
            this(connectionString, sessionTimeout, zooKeeper, watcher, 0, new byte[16], connectTimeout)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnection"/> class.
        /// </summary>
        /// <param name="hosts">The hosts.</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="zooKeeper">The zoo keeper.</param>
        /// <param name="watcher">The watch manager.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="sessionPasswd">The session passwd.</param>
        public ClientConnection(string hosts, TimeSpan sessionTimeout, ZooKeeper zooKeeper, ZKWatchManager watcher, long sessionId, byte[] sessionPasswd)
            : this(hosts, sessionTimeout, zooKeeper, watcher, 0, new byte[16], defaultConnectTimeout)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnection"/> class.
        /// </summary>
        /// <param name="hosts">The hosts.</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="zooKeeper">The zoo keeper.</param>
        /// <param name="watcher">The watch manager.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="sessionPasswd">The session passwd.</param>
        /// <param name="connectTimeout">Connection Timeout.</param>
        public ClientConnection(string hosts, TimeSpan sessionTimeout, ZooKeeper zooKeeper, ZKWatchManager watcher, long sessionId, byte[] sessionPasswd, TimeSpan connectTimeout)
        {
            this.hosts = hosts;
            this.zooKeeper = zooKeeper;
            this.watcher = watcher;
            SessionTimeout = sessionTimeout;
            SessionId = sessionId;
            SessionPassword = sessionPasswd;
            ConnectionTimeout = connectTimeout;

            // parse out chroot, if any
            hosts = SetChrootPath();
            GetHosts(hosts);
            SetTimeouts(sessionTimeout);
            CreateConsumer();
            CreateProducer();
        }

        private void CreateConsumer()
        {
            consumer = new ClientConnectionEventConsumer(this);
        }

        private void CreateProducer()
        {
            producer = new ClientConnectionRequestProducer(this);
        }

        private string SetChrootPath()
        {
            int off = hosts.IndexOf(PathUtils.PathSeparatorChar);
            if (off >= 0)
            {
                string path = hosts.Substring(off);
                // ignore "/" chroot spec, same as null
                if (path.Length == 1)
                    ChrootPath = null;
                else
                {
                    PathUtils.ValidatePath(path);
                    ChrootPath = path;
                }
                hosts = hosts.Substring(0, off);
            }
            else
                ChrootPath = null;
            return hosts;
        }

        private void GetHosts(string hostLst)
        {
            string[] hostsList = hostLst.Split(',');
            List<IPEndPoint> nonRandomizedServerAddrs = new List<IPEndPoint>();
            foreach (string h in hostsList)
            {
                string host = h;
                int port = 2181;
                int pidx = h.LastIndexOf(':');
                if (pidx >= 0)
                {
                    // otherwise : is at the end of the string, ignore
                    if (pidx < h.Length - 1)
                    {
                        port = Int32.Parse(h.Substring(pidx + 1));
                    }
                    host = h.Substring(0, pidx);
                }

                // Handle dns-round robin or hostnames instead of IP addresses
                var hostIps = ResolveHostToIpAddresses(host);
                foreach (var ip in hostIps)
                {
                    nonRandomizedServerAddrs.Add(new IPEndPoint(ip, port));
                }
            }
            IEnumerable<IPEndPoint> randomizedServerAddrs 
                = nonRandomizedServerAddrs.OrderBy(s => Guid.NewGuid()); //Random order the servers

            serverAddrs.AddRange(randomizedServerAddrs);
        }

        private IEnumerable<IPAddress> ResolveHostToIpAddresses(string host)
        {
            var hostEntry = Dns.GetHostEntry(host);
            return hostEntry.AddressList.Where(x => 
                !x.IsIPv6LinkLocal && !x.IsIPv6SiteLocal && !x.IsIPv6Multicast && !x.IsIPv6Teredo);
        }

        private void SetTimeouts(TimeSpan sessionTimeout)
        {
            //since we have no need of it just remark it
            //connectTimeout = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(sessionTimeout.TotalMilliseconds / serverAddrs.Count));
            readTimeout = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(sessionTimeout.TotalMilliseconds * 2 / 3));
        }

        /// <summary>
        /// Gets or sets the session timeout.
        /// </summary>
        /// <value>The session timeout.</value>
        public TimeSpan SessionTimeout { get; private set; }


        /// <summary>
        /// Gets or sets the connection timeout.
        /// </summary>
        /// <value>The connection timeout.</value>
        public TimeSpan ConnectionTimeout { get; private set; }


        /// <summary>
        /// Gets or sets the session password.
        /// </summary>
        /// <value>The session password.</value>
        public byte[] SessionPassword { get; internal set; }

        
        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        /// <value>The session id.</value>
        public long SessionId { get; internal set; }

        /// <summary>
        /// Gets or sets the chroot path.
        /// </summary>
        /// <value>The chroot path.</value>
        public string ChrootPath { get; private set; }

        public void Start()
        {
            consumer.Start();
            producer.Start();
        }

        public void AddAuthInfo(string scheme, byte[] auth)
        {
            if (!zooKeeper.State.IsAlive())
                return;
            authInfo.Add(new AuthData(scheme, auth));
            QueuePacket(new RequestHeader(-4, (int)OpCode.Auth), null, new AuthPacket(0, scheme, auth), null, null, null, null, null, null);
        }

        public ReplyHeader SubmitRequest(RequestHeader h, IRecord request, IRecord response, ZooKeeper.WatchRegistration watchRegistration)
        {
            ReplyHeader r = new ReplyHeader();
            Packet p = QueuePacket(h, r, request, response, null, null, watchRegistration, null, null);
            
            if (!p.WaitUntilFinishedSlim(SessionTimeout))
            {
                throw new TimeoutException(new StringBuilder("The request ").Append(request).Append(" timed out while waiting for a response from the server.").ToString());
            }
            return r;
        }

        public Packet QueuePacket(RequestHeader h, ReplyHeader r, IRecord request, IRecord response, string clientPath, string serverPath, ZooKeeper.WatchRegistration watchRegistration, object callback, object ctx)
        {
            return producer.QueuePacket(h, r, request, response, clientPath, serverPath, watchRegistration);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        private void InternalDispose()
        {
            if(Interlocked.CompareExchange(ref isClosed,1,0) == 0)
            {
                //closing = true;
                if (LOG.IsDebugEnabled)
                    LOG.DebugFormat("Closing client for session: 0x{0:X}", SessionId);

                try
                {
                    SubmitRequest(new RequestHeader { Type = (int)OpCode.CloseSession }, null, null, null);
                    SpinWait spin = new SpinWait();
                    DateTime start = DateTime.Now;
                    while (!producer.IsConnectionClosedByServer)
                    {
                        spin.SpinOnce();
                        if (spin.Count > maxSpin)
                        {
                            if (DateTime.Now.Subtract(start) > SessionTimeout)
                            {
                                throw new TimeoutException(
                                    string.Format("Timed out in Dispose() while closing session: 0x{0:X}", SessionId));
                            }
                            spin.Reset();
                        }
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // ignore, close the send/event threads
                }
                catch (Exception ex)
                {
                    LOG.WarnFormat("Error disposing {0} : {1}", this.GetType().FullName, ex.Message);
                }
                finally
                {
                    producer.Dispose();
                    consumer.Dispose();
                }

            }
        }
        public void Dispose()
        {
            InternalDispose();
            GC.SuppressFinalize(this);
        }

        ~ClientConnection()
        {
            InternalDispose();
        }

        /// <summary>
        /// Returns a <see cref="System.string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("sessionid:0x").AppendFormat("{0:X}", SessionId)
                .Append(" lastZxid:").Append(producer.lastZxid)
                .Append(" xid:").Append(producer.xid)
                .Append(" sent:").Append(producer.sentCount)
                .Append(" recv:").Append(producer.recvCount)
                .Append(" queuedpkts:").Append(producer.OutgoingQueueCount)
                .Append(" pendingresp:").Append(producer.PendingQueueCount)
                .Append(" queuedevents:").Append(consumer.waitingEvents.Count);

            return sb.ToString();
        }

        internal class AuthData
        {
            public string Scheme
            {
                get;
                private set;
            }

            private byte[] data;

            public byte[] GetData()
            {
                return data;
            }

            public AuthData(string scheme, byte[] data)
            {
                this.Scheme = scheme;
                this.data = data;
            }

        }

        internal class WatcherSetEventPair
        {
            public IEnumerable<IWatcher> Watchers
            {
                get;
                private set;
            }
            public WatchedEvent WatchedEvent
            {
                get;
                private set;
            }

            public WatcherSetEventPair(IEnumerable<IWatcher> watchers, WatchedEvent @event)
            {
                this.Watchers = watchers;
                this.WatchedEvent = @event;
            }
        }
    }
}