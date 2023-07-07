using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using OOCSI.Services;
using OOCSI.Protocol;
using OOCSI.Client;

namespace OOCSI.Sockets {
    internal class SocketClientRunner {

        private static readonly string SELF = "SELF";

        protected string _name;
        protected string _hostname;
        protected int _port;

        // IO
        private Socket _socket;
        protected OOCSIClient.LoggingDelegate _loggingDelegate;

        // Connection Flags
        protected bool _connected = false;
        protected bool _shouldReconnect = false;

        private int _reconnectCountDown = 100;
        private bool _relinquished = false;
        private bool _hasPrintedServerInfo = false;

        // Testing
        private bool _noPing = false;
        private bool _noProcess = false;

        // Management
        protected Dictionary<string, Handler> _channels;
        private List<OOCSICall> _openCalls;
        private Queue<string> _tempIncomingMessages;

        // Thread Pool
        private Thread _executor;

        // Properties
        protected bool IsConnecting { get => !this._connected && this._reconnectCountDown > 0; }

        protected SocketClientRunner ( OOCSIClient.LoggingDelegate loggingDelegate = null ) {
            this._openCalls = new List<OOCSICall>();
            this._tempIncomingMessages = new Queue<string>();

            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this._executor = new Thread(new ThreadStart(this.Run));
            this._executor.Start();
            this._loggingDelegate = loggingDelegate;
        }

        protected void Run () {

            try {
                int connections = 0;

                // SESSIONS
                while ( this._shouldReconnect || connections == 0 ) {
                    connections++;

                    // ATTEMPTS
                    this._reconnectCountDown = 100;
                    while ( this._reconnectCountDown-- > 0 ) {
                        if ( this._hostname != null && this.AttemptToConnect() ) {
                            break;
                        }

                        this.Sleep(1000);
                    }

                    if ( this._connected ) {
                        this.RunCommunication();
                    } else {
                        this.Sleep(5000);
                    }

                }
            } catch ( Exception e ) {
                this.Disconnect();
            } finally {
                this._relinquished = true;
            }

        }

        protected bool AttemptToConnect () {

            try {
                this.ConnectSocket();

                this.Send($"{this._name}(JSON)");
            } catch ( Exception e ) {
                this.Log($"Connecting to OOCSI server failed: {e.Message}");
                return false;
            }

            return this.DoConnectionHandshake();
        }

        private void ConnectSocket () {
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.NoDelay = true;
            // Change packet priority? Socket.TrafficClass.

            this._socket.Connect(this._hostname, this._port);
        }

        private bool MessageArrayContainsWelcome ( string[] messages ) {
            foreach ( string msg in messages) {
                if ( msg.Contains($"welcome {this._name}") ) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// TODO: Don't just check first received message?
        /// </summary>
        /// <returns></returns>
        protected bool DoConnectionHandshake () {
            // Short timeout time.
            this._socket.ReceiveTimeout = 5000;

            // Check if we are welcome
            if ( this._socket.Connected ) {
                string[] msgsReceived = null;
                try {
                    msgsReceived = this.Receive();
                } catch ( Exception e ) {
                    this.Log($"OOCSI Server receiving failed: {e.Message}");
                }

                // Check if one of the messages contains our welcome.
                if ( msgsReceived == null || !this.MessageArrayContainsWelcome(msgsReceived)) {
                    this.Disconnect();
                    this.Log("OOCSI Server didn't reply to our hello. :(");
                    return false;
                }

                this.Log($"Successfully connected to OOCSI server as {this._name}");

                this._socket.ReceiveTimeout = 20000;

                this._connected = true;

                // Notify server of resubscriptions
                foreach ( string channel in this._channels.Keys ) {
                    this.NotifyOfSubscription(channel);
                }
            }

            return true;
        }

        /// <summary>
        /// Main communication loop.
        /// </summary>
        private void RunCommunication () {
            try {
                int cyclesSinceRead = 0;
                while ( this._socket.Connected ) {
                    while ( this._socket.Available > 0 && !this._noProcess ) {
                        string[] msgs = this.Receive();
                        foreach ( string msg in msgs ) {
                            this.HandleMessage(msg);
                        }

                        cyclesSinceRead = 0;
                    }

                    // Sleep if nothing to read.
                    this.Sleep(10);

                    // If no data comes in for more than 20 seconds, kill and reconnect.
                    if ( cyclesSinceRead++ > 2000 ) {
                        this.CloseSocket();
                        this.Log($"OOCSI disconnected (application level timeout) for {this._name}");
                        break;
                    } else if ( cyclesSinceRead++ > 1500 ) {
                        // After 15 seconds send a ping to seek for connection.
                        this.Log("We haven't received a ping from the server in a long time! Sending our own ping.");
                        this.Send("ping");
                    }


                }
            } catch ( Exception e ) {
                this.CloseSocket();
                this.Log($"OOCSI disconnected (server unavailable) for {this._name}");
                this.Log($"We got an error: {e.Message}");
            }
        }

        protected void HandleMessage ( string fromServer ) {
            this.Log($"TRAFFIC RECEIVED: {fromServer}");

            var regex = new Regex(Regex.Escape(":.*"));

            // JSON Handler
            if ( fromServer.StartsWith("{") ) {
                Dictionary<string, object> dic = this.ParseData(fromServer);
                object channelObject = null;
                dic.TryGetValue("recipient", out channelObject);
                dic.Remove("recipient").ToString();

                string channel = "";
                if ( channelObject != null ) {
                    channel = channelObject.ToString();
                }

                Handler handler = null;
                this._channels.TryGetValue(channel, out handler);
                this.Log(handler == null ? "NOT SUBSCRIBED" : "SUBSCRIBED");
                if ( handler == null &Equals(regex.Replace(this._name, "", 1)) ) {
                    this._channels.TryGetValue(SELF, out handler);
                }

                string sender = dic.ContainsKey("sender") ? dic.Remove("sender").ToString() : "";
                string timestamp = dic.ContainsKey("timestamp") ? dic.Remove("timestamp").ToString() : "";

                this.HandleMappedData(channel, fromServer, timestamp, sender, handler, dic);
                return;
            } else if ( !fromServer.StartsWith("send") && !this._noPing ) {
                // Any other no send message
                this._tempIncomingMessages.Enqueue(fromServer);
                this.Send(".");
                return;
            }

            // Parse server output
            string[] tokens = fromServer.Split('\u0020');
            if ( tokens.Length != 5 ) {
                return;
            }

            // Get channel
            string chn = tokens[1];
            Handler chnHandler = this.GetChannel(chn);

            if ( chnHandler == null && chn.Equals(regex.Replace(this._name, "", 1)) ) {
                chnHandler = this.GetChannel(SELF);
            }

            // Parse Data
            this.HandleData(chn, tokens[2], tokens[3], tokens[4], chnHandler);
        }

        private void HandleData ( string channel, string data, string timestamp, string sender, Handler handler ) {
            Dictionary<string, object> dataDict = this.ParseData(data);
            if ( dataDict != null ) {
                this.HandleMappedData(channel, data, timestamp, sender, handler, dataDict);
            } else if ( handler != null ) {
                Task.Run(() => handler.Send(sender, data, timestamp, channel));
            }
        }

        private void HandleMappedData ( string channel, string data, string timestamp, string sender, Handler handler, Dictionary<string, object> dataDictionary ) {
            OOCSIEvent oocsiEvent = new OOCSIEvent(channel, dataDictionary, sender);

            handler.Receive(sender, dataDictionary, 0x00, channel);
        }

        private Dictionary<string, object> ParseData ( string data ) {
            Dictionary<string, object> dataDictionary = new Dictionary<string, object>();
            try {
                dataDictionary = Handler.ParseData(data);
            } catch ( Exception e ) {
                throw new Exception("Data parsing from server failed", e);
            }

            return dataDictionary;
        }

        protected void NotifyOfSubscription ( string channel ) {
            if ( !this._socket.Connected ) return;

            this.Send($"subscribe {channel}");

            if ( this._channels.ContainsKey(channel) ) {
                this.Log($"Reconnected subscription to {channel}");
            }
        }

        protected bool IsSubscribed ( string channel ) => this._channels.ContainsKey(channel);

        protected Handler GetHandler ( string channel ) => this._channels[channel];

        protected void Send ( string message ) {
            if ( this._socket == null || !this._socket.Connected ) return;

            byte[] bytes = Encoding.ASCII.GetBytes(message);
            List<byte> buffer = bytes.ToList();

            this.Log($"TRAFFIC SENT: {message}");

            // Add line feed ending
            buffer.Add(0x0a);

            this._socket.Send(buffer.ToArray());
        }

        protected void SendRaw ( string channel, string message ) {
            this.Send($"sendraw {channel} {message}");
        }

        /// <summary>
        /// Receives messages from the socket.
        /// </summary>
        /// <returns>Array of messages.</returns>
        private string[] Receive () {
            byte[] buffer = new byte[1024];
            int bytesReceived = this._socket.Receive(buffer);

            string raw = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            string[] lines = raw.Split( new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for ( int i = 0; i < lines.Length; i++ ) {
                lines[i] = lines[i].Replace("\n", "").Replace("\r", "");
            }

            return lines.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }

        public string SendSyncPoll ( string msg ) {
            this._tempIncomingMessages.Clear();
            this.Send(msg);
            return this.SyncPoll();
        }

        private string SyncPoll () {
            return this.SyncPoll(1000);
        }

        private string SyncPoll ( int timeout ) {
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

        /// <summary>
        /// Retrieve the connected clients on the server.
        /// </summary>
        /// <returns></returns>
        public string GetClients () {
            return this._connected ? this.SendSyncPoll("clients") : "";
        }
        
        /// <summary>
        /// Retrieve the current channels on the server.
        /// </summary>
        /// <returns></returns>
        public string GetChannels () {
            return this._connected ? this.SendSyncPoll("channels") : "";
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

        protected void Kill () {
            this._connected = false;
            this.CloseSocket();
            this.Log("OOCSI connection closed (killed)");
        }

        private void CloseSocket () {
            if ( this._socket == null )
                return;

            this._socket.Close();
        }

        protected void Sleep ( int ms ) {
            try {
                Thread.Sleep(ms);
            } catch ( Exception e ) {
                throw e;
            }
        }

        private Handler GetChannel ( string channel ) {
            Handler handler = null;
            this._channels.TryGetValue(channel, out handler);
            return handler;
        }

        private void Log ( string msg ) {
            this._loggingDelegate?.Invoke(msg);
        }

    }
}
