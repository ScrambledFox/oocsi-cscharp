using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;

using OOCSI.Services;
using OOCSI.Protocol;

namespace OOCSI.Sockets {
    internal class SocketClientRunner : Task {

        private string _name;
        private string _hostname;
        private int _port;

        // IO
        private Socket _socket;

        // Connection Flags
        private bool _connectionEstablished = false;
        private bool _reconnect = false;

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
        private TaskFactory _executor;

        public SocketClientRunner ( string name, string hostname, int port, Dictionary<string, Handler> channels, Dictionary<string, Responder> services ) : base(null) {
            this._name = name;
            this._hostname = hostname;
            this._port = port;
            this._channels = channels;
            this._services = services;

            this._openCalls = new List<OOCSICall>();
            this._tempIncomingMessages = new Queue<string>();

            this._executor = Task.Factory;
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
            bool noPing, bool
            noProcess 
            )
            : this(name, hostname, port, channels, services) {

            this._noPing = noPing;
            this._noProcess = noProcess;
        }

    }
}
