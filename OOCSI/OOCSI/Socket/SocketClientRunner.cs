using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using OOCSI.Services;
using OOCSI.Protocol;

namespace OOCSI.Sockets {
    internal class SocketClientRunner {

        private static readonly string SELF = "SELF";

        private string _name;
        private string _hostname;
        private int _port;

        // IO
        private Socket _socket;
        private OOCSIClient.LoggingDelegate _loggingDelegate;

        // Connection Flags
        private bool _connected = false;
        private bool _shouldReconnect = false;

        private int _reconnectCountDown = 100;
        private bool _relinquished = false;
        private bool _hasPrintedServerInfo = false;

        // Testing
        private bool _noPing = false;
        private bool _noProcess = false;

        // Management
        private Dictionary<string, Handler> _channels;
        private Dictionary<string, Responder> _services;
        private List<OOCSICall> _openCalls;
        private Queue<string> _tempIncomingMessages;

        // Thread Pool
        private Thread _executor;

        // Properties
        public bool ShouldReconnect { get => this._shouldReconnect; set { this._shouldReconnect = value; } }
        public bool IsConnecting { get => !this._connected && this._reconnectCountDown > 0; }
        public bool Connected => this._connected;

        public SocketClientRunner (
            string name,
            string hostname,
            int port,
            Dictionary<string, Handler> channels,
            Dictionary<string, Responder> services,
            OOCSIClient.LoggingDelegate loggingDelegate = null ) {

            this._name = name;
            this._hostname = hostname;
            this._port = port;
            this._channels = channels;
            this._services = services;

            this._openCalls = new List<OOCSICall>();
            this._tempIncomingMessages = new Queue<string>();

            this._executor = new Thread(new ThreadStart(this.Run));
            this._executor.Start();
            this._loggingDelegate = loggingDelegate;
        }

        /// <summary>
        /// Used for testing.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="channels"></param>
        /// <param name="services"></param>
        /// <param name="noPing"></param>
        /// <param name="noProcess"></param>
        public SocketClientRunner (
            string name,
            string hostname,
            int port,
            Dictionary<string, Handler> channels,
            Dictionary<string, Responder> services,
            bool noPing,
            bool noProcess,
            OOCSIClient.LoggingDelegate loggingDelegate = null
            )
            : this(name, hostname, port, channels, services, loggingDelegate) {

            this._noPing = noPing;
            this._noProcess = noProcess;
        }

        public void Run () {

            if ( !this.Connected ) {
                this.Connect();
            }

        }

        public bool Connect () {
            this._socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try {
                this._socket.Connect(this._hostname, this._port);

                this.Send($"{this._name}(JSON)");
            } catch ( Exception e ) {
                this.Log($"Connecting to OOCSI server failed: {e.Message}");
                return false;
            }

            return this.DoConnectionHandshake();
            
            this.Log($"Connected to OOCSI server at {this._hostname}:{this._port}");
            return this._connected = true;
        }

        public bool DoConnectionHandshake () {
            // Short timeout time.
            this._socket.ReceiveTimeout = 5000;

            // Check if we are welcome
            if ( this._socket.Connected ) {
                string msgReceived = this.Receive();

                if ( !msgReceived.Contains($"welcome {this._name}") ) {
                    this.Disconnect();
                    this.Log("OOCSI Server didn't reply to our hello. :(");
                    // Throw exception?
                }

                this.Log($"Successfully connected to OOCSI server as {this._name}");

                this._socket.ReceiveTimeout = 20000;

                this._connected = true;

                foreach ( string channel in this._channels.Keys ) {
                    this.Subscribe(channel);
                }
            }

            return true;
        }

        /// <summary>
        /// Main communication loop.
        /// </summary>
        private void RunCommunication () {
            try {
                string fromServer;
                int cyclesSinceRead = 0;
                while ( this._socket.Connected ) {

                    while ( this._socket.Available > 0 && !this._noProcess ) {
                        this.HandleMessage(fromServer = this.Receive());
                        cyclesSinceRead = 0;
                    }

                    // Sleep if nothing to read.
                    Thread.Sleep(10);

                    // If no data comes in for more than 20 seconds, kill and reconnect.
                    if ( cyclesSinceRead++ > 2000 ) {
                        this.CloseSocket();
                        this.Log($"OOCSI disconnected (application level timeout) for {this._name}");
                        break;
                    } else if ( cyclesSinceRead++ > 1000 ) {
                        // After 10 seconds send a ping to seek for connection.
                        this.Send("ping");
                    }


                }
            } catch ( Exception e ) {
                this.CloseSocket();
                this.Log($"OOCSI disconnected (server unavailable) for {this._name}");
            }
        }

        public void HandleMessage ( string fromServer ) {

            // JSON Handler
            if ( fromServer.StartsWith("{") ) {
                Dictionary<string, object> dic = this.ParseData(fromServer);
                string channel = dic.ContainsKey("recipient") ? dic.Remove("recipient").ToString() : "";
                
                Handler handler = null;
                this._channels.TryGetValue(channel, out handler);
                var regex = new Regex(Regex.Escape(":.*"));
                if ( handler == null && channel.Equals(regex.Replace(this._name, "", 1) ) ) {
                    this._channels.TryGetValue(SELF, out handler);
                }

                string sender = dic.ContainsKey("sender") ? dic.Remove("sender").ToString() : "";
                string timestamp = dic.ContainsKey("timestamp") ? dic.Remove("timestamp").ToString() : "";

                this.HandleMappedData(channel, fromServer, timestamp, sender, handler, dic);
            }
        }

        private void HandleData (string channel,string data, string timestamp, string sender, Handler handler ) {
            throw new NotImplementedException();
        }

        private void HandleMappedData ( string channel, string data, string timestamp, string sender, Handler handler, Dictionary<string, object> dataDictionary ) {
            throw new NotImplementedException();
        }

        private Dictionary<string, object> ParseData ( string data ) {
            Dictionary<string, object> dataDictionary = new Dictionary<string, object>();
            try {
                dataDictionary = Handler.ParseData(data);
            } catch ( Exception e ) {
                dataDictionary = null;
            }

            return dataDictionary;
        }

        private void Subscribe ( string channel ) {
            if ( !this._socket.Connected ) return;

            this.Send($"subscribe {channel}");

            if ( this._channels.ContainsKey("channel") ) {
                this.Log($"Reconnected subscription to {channel}");
            }
        }

        public void Send ( string message ) {
            if ( !this._socket.Connected ) return;

            this._socket.Send(Encoding.ASCII.GetBytes(message));
        }

        private string Receive () {
            byte[] buffer = new byte[255];
            int bytesReceived = this._socket.Receive(buffer);
            return Encoding.ASCII.GetString(buffer, 0, bytesReceived);
        }

        public string SendSyncPoll (string msg) {
            this._tempIncomingMessages.Clear();
            this.Send(msg);
            return this.SyncPoll();
        }

        private string SyncPoll () {
            return this.SyncPoll(1000);
        }

        private string SyncPoll (int timeout) {
            long start = System.DateTime.Now.Millisecond;

            try {
                while ( this._tempIncomingMessages.Count == 0 || start + timeout > System.DateTime.Now.Millisecond ) {
                    Thread.Yield();
                    Thread.Sleep(50);
                }
                return this._tempIncomingMessages.Count > 0 ? this._tempIncomingMessages.Dequeue() : null;
            } catch ( Exception e ) {
            }

            return null;
        }

        public void Disconnect () {
            this._connected = false;

            this._shouldReconnect = false;
            this._reconnectCountDown = 0;
            this._relinquished = true;

            if ( this._socket.Connected ) {
                // Say goodbye if possible.
                this.Send("quit");
            }

            this.CloseSocket();
        }

        public void Kill () {
            this._connected = false;
            this.CloseSocket();
            this.Log("OOCSI connection closed (killed)");
        }

        private void CloseSocket () {
            if ( this._socket == null )
                return;

            this._socket.Close();
            this._socket.Dispose();
            this._socket = null;
        }

        public void Sleep ( int ms ) {
            try {
                Thread.Sleep(ms);
            } catch ( Exception e ) {
                throw e;
            }
        }

        private void Log ( string msg ) {
            this._loggingDelegate?.Invoke(msg);
        }

    }
}
