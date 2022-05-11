using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

using OOCSI.Protocol;
using OOCSI.Services;

namespace OOCSI.Sockets {
    internal class SocketClient {
        private static int MULTICAST_PORT = 4448;
        private static string MULTICAST_GROUP = "224.0.0.144";

        private string _name;
        private bool _reconnect = false;

        private bool _isConnected = false;

        private Dictionary<string, Handler> _channels;
        private Dictionary<string, Responder> _services;

        //private SocketClientRunner _runner;

        /// <summary>
        /// Creates a new socket with a given name.
        /// </summary>
        /// <param name="name">Client name</param>
        /// <param name="channels"></param>
        /// <param name="services"></param>
        public SocketClient ( string name, Dictionary<string, Handler> channels, Dictionary<string, Responder> services ) {
            this._name = name;
            this._channels = channels;
            this._services = services;
        }

        /// <summary>
        /// Start pinging for a multi-cast lookup.
        /// </summary>
        /// <returns>Returns true if succesfully connected to a found server.</returns>
        public bool StartMulticastLookup () {
            try {
                UdpClient udpClient = new UdpClient(MULTICAST_PORT);
                udpClient.Connect(IPAddress.Parse(MULTICAST_GROUP), MULTICAST_PORT);

                for ( int i = 0; !this._isConnected && i < 5; i++ ) {
                    ConnectFromMulticast(udpClient);

                    Thread.Sleep(1000);
                }

                return this._isConnected;
            } catch ( Exception e ) {
                return false;
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

            }
        }

        public bool Connect ( string hostname, int port ) {

            return false;
        }

        public void Log ( string msg ) {

        }

    }
}
