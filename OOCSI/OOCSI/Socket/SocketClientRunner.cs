using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using OOCSI.Services;
using OOCSI.Protocol;

namespace OOCSI.Sockets {
    internal class SocketClientRunner {

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
            byte[] buffer = new byte[100];
            if ( this._socket.Connected ) {
                int bytesReceived = this._socket.Receive(buffer);
                string msgReceived = Encoding.ASCII.GetString(buffer, 0, bytesReceived);

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
