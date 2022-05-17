using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using OOCSI.Protocol;
using OOCSI.Services;

namespace OOCSI.Sockets {
    internal class SocketClient {
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
                        Task.Run(() => ConnectFromMulticast(udpClient), cts.Token);

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
            try {
                byte[] buffer = new byte[256];
                IPEndPoint endPoint = null;

                buffer = udpClient.Receive(ref endPoint);
                string data = System.Text.Encoding.UTF8.GetString(buffer);
                if ( data.StartsWith("OOCSI@") ) {
                    string[] parts = data.Replace("OOCSI@", "").Replace("\\(.*\\)", "").Split(':');
                    if ( parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0 ) {
                        this.Connect(parts[0], int.Parse(parts[1]));
                    }
                }

            } catch ( Exception e ) {
                throw e;
            }
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

        public void Disconnect () {
            throw new NotImplementedException();
        }

        private void Log ( string msg ) {
            this._loggingDelegate?.Invoke(msg);
        }

    }
}
