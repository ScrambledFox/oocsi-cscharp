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
    internal class SocketClient {

        private static readonly string SELF = "SELF";

        private static int DEFAULT_PORT = 4444;
        private static int MULTICAST_PORT = 4448;
        private static IPAddress MULTICAST_GROUP = IPAddress.Parse("224.0.0.144");

        private string _name;
        private bool _shouldReconnect = false;

        private Dictionary<string, Handler> _channels;
        private Dictionary<string, Responder> _services;

        private OOCSIClient.LoggingDelegate _loggingDelegate;

        public bool IsConnected => this._runner != null && this._runner.Connected;
        public bool ShouldReconnect {
            get { return _shouldReconnect; }
            set {
                _shouldReconnect = value;
                if ( this._runner != null ) {
                    this._runner.ShouldReconnect = _shouldReconnect;
                }
            }
        }

        public struct UdpState {
            public UdpClient client;
            public IPEndPoint ipEndpoint;
        }

        private SocketClientRunner _runner;

        /// <summary>
        /// Creates a new socket with a given name.
        /// </summary>
        /// <param name="name">Client name</param>
        /// <param name="channels"></param>
        /// <param name="services"></param>
        public SocketClient ( string name, Dictionary<string, Handler> channels, Dictionary<string, Responder> services, OOCSIClient.LoggingDelegate loggingDelegate ) {
            this._name = name;
            this._channels = channels;
            this._services = services;
            this._loggingDelegate = loggingDelegate;
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
            if ( this._runner != null ) {
                this._runner.Disconnect();
            }

            this._runner = new SocketClientRunner(this._name, hostname, port, this._channels, this._services, this._loggingDelegate);
            this._runner.ShouldReconnect = this._shouldReconnect;

            while ( this._runner.IsConnecting ) {
                // BUG:? Suspends the calling thread instead of the runner thread?
                this._runner.Sleep(100);
            }

            return this._runner.Connected;
        }

        /// <summary>
        /// Subscribe to a channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        public void Subscribe ( string channel, Handler handler ) {
            if ( this._runner != null && !this.IsSubscribed(channel) ) {
                this._runner.Subscribe(channel);
            }

            this.AddHandler(channel, handler);
        }

        private void AddHandler ( string channel, Handler handler ) {
            if ( this._channels.ContainsKey(channel) ) {
                Handler h = this._channels[channel];
                if ( h is MultiHandler ) {
                    MultiHandler mh = (MultiHandler)h;
                    mh.Add(handler);
                }
            } else {
                this._channels.Add(channel, new MultiHandler(handler));
            }
        }

        private bool IsSubscribed ( string channel ) => this._channels.ContainsKey(channel);

        /// <summary>
        /// Subscribe to own channel.
        /// </summary>
        /// <param name="handler"></param>
        public void SubscribeToSelf ( Handler handler ) {
            if ( this._runner != null ) {
                this._runner.Send($"subscribe {this._name}");
            }

            if ( this._channels.ContainsKey(SELF) ) {
                this.Log($"Renewed subscription for {this._name}");
            }

            this._channels.Add(SELF, handler);
        }

        /// <summary>
        /// Sends a raw message without serialisation.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="message"></param>
        public void Send ( string channelName, string message ) {
            if ( this._runner != null ) {
                this._runner.Send($"sendraw {channelName} {message}");
            }
        }

        public void Disconnect () {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieve the connected clients on the server.
        /// </summary>
        /// <returns></returns>
        public string GetClients () {
            return this._runner != null ? this._runner.SendSyncPoll("clients") : "";
        }

        /// <summary>
        /// Retrieve the current channels on the server.
        /// </summary>
        /// <returns></returns>
        public string GetChannels () {
            return this._runner != null ? this._runner.SendSyncPoll("channels") : "";
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
