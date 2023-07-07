using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using OOCSI.Protocol;
using OOCSI.Services;
using OOCSI.Client;

namespace OOCSI.Sockets {
    internal class SocketClient : SocketClientRunner {

        private static readonly string SELF = "SELF";

        private static int DEFAULT_PORT = 4444;
        private static int MULTICAST_PORT = 4448;
        private static IPAddress MULTICAST_GROUP = IPAddress.Parse("224.0.0.144");

        public bool IsConnected => this._connected;
        public bool ShouldReconnect { get; set; }

        public struct UdpState {
            public UdpClient client;
            public IPEndPoint ipEndpoint;
        }

        /// <summary>
        /// Creates a new socket with a given name.
        /// </summary>
        /// <param name="name">Client name</param>
        /// <param name="channels"></param>
        /// <param name="services"></param>
        public SocketClient ( string name, OOCSIClient.LoggingDelegate loggingDelegate ) : base(loggingDelegate) {
            this._name = name;

            this._channels = new Dictionary<string, Handler>();
        }

        /// <summary>
        /// Start pinging for a multi-cast lookup.
        /// </summary>
        /// <returns>Returns true if succesfully connected to a found server.</returns>
        public bool StartMulticastLookup () {
            try {
                using ( UdpClient udpClient = new UdpClient(MULTICAST_PORT) ) {
                    udpClient.JoinMulticastGroup(MULTICAST_GROUP);
                    udpClient.EnableBroadcast = true;

                    for ( int i = 0; !this.IsConnected && i < 5; i++ ) {
                        this.Log($"Trying to connect to {MULTICAST_GROUP}:{MULTICAST_PORT}...");

                        CancellationTokenSource cts = new CancellationTokenSource();
                        //Task.Run(() => ConnectFromMulticast(udpClient), cts.Token);

                        ConnectFromMulticast(udpClient);

                        Thread.Sleep(1000);
                        cts.Cancel();
                    }

                    if ( this.IsConnected ) this.Log($"Connected to {MULTICAST_GROUP}:{MULTICAST_PORT}.");
                    else this.Log($"Connection attempt failed 5 times.");

                    udpClient.Dispose();
                    udpClient.Close();
                    return this.IsConnected;
                }
            } catch ( Exception e ) {
                throw e;
            }
        }

        /// <summary>
        /// Connects to a multi-cast server.
        /// </summary>
        /// <param name="socket"></param>
        private void ConnectFromMulticast ( UdpClient udpClient ) {
            //try {
            //    byte[] buffer = new byte[256];
            //    IPEndPoint endPoint = null;

            //    buffer = udpClient.Receive(ref endPoint);
            //    string data = Encoding.UTF8.GetString(buffer);
            //    if ( data.StartsWith("OOCSI@") ) {
            //        string[] parts = data.Replace("OOCSI@", "").Replace("\\(.*\\)", "").Split(':');
            //        if ( parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0 ) {
            //            this.Connect(parts[0], int.Parse(parts[1]));
            //        }
            //    }

            //} catch ( Exception e ) {
            //    Log(e.Message);
            //}

            try {

                UdpState state = new UdpState();
                state.client = udpClient;
                state.ipEndpoint = new IPEndPoint(MULTICAST_GROUP, MULTICAST_PORT);
                udpClient.BeginReceive(new AsyncCallback(OnMulticastCallback), state);

            } catch ( Exception e ) {
                Log($"  - SocketError: {e.Message}");
            }

        }

        private void OnMulticastCallback ( IAsyncResult result ) {
            try {
                return;
            } catch ( Exception e ) {
                Log($"  - SocketError: {e.Message}");
            }
        }

        /// <summary>
        /// Connect to an OOCSI server at {hostname} at port 4444.
        /// </summary>
        /// <param name="hostname">Host Address/IP-Address</param>
        /// <returns>True if successfully connected.</returns>
        public bool Connect ( string hostname ) {
            return this.Connect(hostname, DEFAULT_PORT);
        }

        /// <summary>
        /// Connect to an OOCSI server at {hostname} at {port}
        /// </summary>
        /// <param name="hostname">Host Address/IP-Address</param>
        /// <param name="port">Port</param>
        /// <returns>True if sucessfully connected.</returns>
        public bool Connect ( string hostname, int port ) {
            this._hostname = hostname;
            this._port = port;

            while ( this.IsConnecting ) {
                // BUG:? Suspends the calling thread instead of the runner thread?
                this.Sleep(100);
            }

            return this.IsConnected;
        }

        /// <summary>
        /// Subscribe to a channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="callback"></param>
        public void Subscribe ( string channel, MessageCallback callback ) {
            if ( !this.IsSubscribed(channel) ) {
                this.NotifyOfSubscription(channel);
            }

            this.AddHandler(channel, new BasicHandler(callback));
        }

        /// <summary>
        /// Subscribe to own channel.
        /// </summary>
        /// <param name="handler"></param>
        public void SubscribeToSelf ( MessageCallback callback) {
            this.NotifyOfSubscription(SELF);

            if ( this._channels.ContainsKey(SELF) ) {
                this.Log($"Renewed subscription for {SELF}");
            }

            this.AddHandler(SELF, new BasicHandler(callback));
        }


        /// <summary>
        /// Add Handler
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        private void AddHandler ( string channel, Handler handler ) {
            if ( this.IsSubscribed(channel) ) {
                Handler h = this.GetHandler(channel);
                if ( h is MultiHandler ) {
                    MultiHandler mh = (MultiHandler)h;
                    mh.Add(handler);
                }
            } else {
                this._channels.Add(channel, handler);
            }
        }

        /// <summary>
        /// Sends a raw message without serialisation.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        public void Send ( string channel, string message ) {
            this.SendRaw(channel, message);
        }

        /// <summary>
        /// Internal log handler.
        /// </summary>
        /// <param name="msg"></param>
        private void Log ( string msg ) {
            this._loggingDelegate?.Invoke(msg);
        }

    }
}
