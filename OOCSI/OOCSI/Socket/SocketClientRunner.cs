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

        private bool _disconnected = false;
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
        public bool IsConnecting { get => !this._disconnected && !this._connected && this._reconnectCountDown > 0; }
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
            this.Log("Logging");
        }

        public void Connect () {
            throw new NotImplementedException();
        }

        public void Disconnect () {
            throw new NotImplementedException();
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
