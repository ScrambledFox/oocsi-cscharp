using System;
using System.Collections.Generic;
using System.Reflection;

using OOCSI.Sockets;
using OOCSI.Protocol;
using OOCSI.Services;
using OOCSI.Data;

namespace OOCSI {
    public class OOCSIClient {
        private SocketClient _socketClient;
        private string _name;

        private Dictionary<string, Handler> _channels = new Dictionary<string, Handler>();
        private Dictionary<string, Responder> _services = new Dictionary<string, Responder>();

        public string Name { get => _name; }
        public bool IsConnected => _socketClient.IsConnected;
        public bool IsReconnecting => _socketClient.ShouldReconnect;

        public delegate void LoggingDelegate ( string msg );
        private LoggingDelegate _logHandler;

        /// <summary>
        /// Creates a new OOCSI client with a random name.
        /// </summary>
        public OOCSIClient () : this(null, null) { }

        /// <summary>
        /// Creates a new OOCSI client with a set name.
        /// </summary>
        /// <param name="name">Name to use for our client.</param>
        public OOCSIClient ( string name ) : this(name, null) { }

        /// <summary>
        /// Creates a new OOCSI client with a random name and a set Logging method.
        /// </summary>
        /// <param name="logHandler">Logging delegate method.</param>
        public OOCSIClient ( LoggingDelegate loggingHandler ) : this(null, loggingHandler) { }

        /// <summary>
        /// Creates a new OOCSI client with a set name and also set a method for logging.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logHandler">Logging delegate method.</param>
        public OOCSIClient ( string name, LoggingDelegate logHandler ) {
            // Setup logging method
            this._logHandler = logHandler;

            // Set a standard random name if none has been set yet.
            if ( name == null || name.Length == 0 ) {
                name = "OOCSI_CSHARP_"; /*Guid.NewGuid().ToString();*/
            }

            if ( name.Contains(" ") ) {
                this.Log("[ERROR] OOCSI name cannot contain spaces");
                this.Log(" - OOCSI connection aborted");
                return;
            }

            this._name = name;
            _socketClient = new SocketClient(name, _channels, _services, logHandler);

            var assembly = Assembly.GetExecutingAssembly();
            var informationVersion = assembly.
                GetCustomAttribute<AssemblyInformationalVersionAttribute>().
                InformationalVersion;

            this.Log("OOCSI-CSHARP client v" + informationVersion + " started with client handle: " + name);
        }

        /// <summary>
        /// Connects to OOCSI network based on multi-cased messages broadcasting server ip.
        /// </summary>
        /// <returns>True if client connected succesfully.</returns>
        public bool Connect () {
            return this._socketClient.StartMulticastLookup();
        }

        /// <summary>
        /// Connects to {hostName} on port 4444.
        /// </summary>
        /// <param name="hostname">Server URL to connect to.</param>
        /// <param name="port">Port where the server is running on.</param>
        /// <returns>True if client connected succesfully.</returns>
        public bool Connect ( string hostname ) {
            return this._socketClient.Connect(hostname);
        }

        /// <summary>
        /// Connects to {hostName} on port {port}.
        /// </summary>
        /// <param name="hostname">Server URL to connect to.</param>
        /// <param name="port">Port where the server is running on.</param>
        /// <returns>True if client connected succesfully.</returns>
        public bool Connect ( string hostname, int port ) {
            return this._socketClient.Connect(hostname, port);
        }

        /// <summary>
        /// Disconnects from the OOCSI Server.
        /// </summary>
        /// <returns>True if client disconnected succesfully.</returns>
        public void Disconnect () {
            this._socketClient.Disconnect();
        }

        ///// <summary>
        ///// Force quit the connection to the server!
        ///// 
        ///// This is used for testing. In normal circumstances, use Disconnect();
        ///// </summary>
        public void Kill () {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reconnects the connection to the OOCSI server.
        /// </summary>
        public void Reconnect () {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set whether we should try reconnecting when losing connection.
        /// </summary>
        /// <param name="reconnect"></param>
        public void SetShouldReconnect ( bool reconnect ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribe to a OOCSI channel.
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <param name="handler">Callback handler for incoming data.</param>
        public void Subscribe ( string channel, Handler handler ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribe to self (own channel).
        /// </summary>
        /// <param name="handler">Callback handler for incoming data.</param>
        public void SubscribeToSelf ( Handler handler ) {
            this._socketClient.SubscribeToSelf(handler);
        }

        /// <summary>
        /// Unsubscribe from the channel with the given name.
        /// </summary>
        /// <param name="channelName">Channel to unsubscribe from.</param>
        public void Unsubscribe ( string channelName ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register a call with the socket client.
        /// </summary>
        /// <param name="call"></param>
        public void Register ( OOCSICall call ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register a responder with the socket client with a given handle "callName".
        /// </summary>
        /// <param name="callName"></param>
        /// <param name="responder"></param>
        public void Register ( string callName, Responder responder ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register a responder with the socket client with a given handle "callName" on channel "channelName"
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="callName"></param>
        /// <param name="responder"></param>
        public void Register ( string channelName, string callName, Responder responder ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Unregister a responder with the socket client with a given handle "callName"
        /// </summary>
        /// <param name="callName"></param>
        public void Unregister ( string callName ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Unregister a responder with the socket client with a given handle "callName" on channel "channelName"
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="callName"></param>
        public void Unregister ( string channelName, string callName ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Send a string message to a channel.
        /// </summary>
        /// <param name="channelName">Channel to send the message to.</param>
        /// <param name="message">Message to send.</param>
        public void Send ( string channelName, string message ) {
            if ( channelName != null && channelName.Trim().Length > 0 ) {
                this._socketClient.Send(channelName, message);
            }
        }

        /// <summary>
        /// Send a data object to a channel.
        /// </summary>
        /// <param name="channelName">Channel to sent the data to.</param>
        /// <param name="data">Data to send as a dictionary with a string key and a general object value.</param>
        public void Send ( string channelName, Dictionary<string, Object> data ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an OOCSI Device instance with the client's name that can be configured and then submitted to the OOCSI server.
        /// </summary>
        /// <returns>New OOCSI device instance</returns>
        public OOCSIDevice HeyOOCSI () {
            //return new OOCSIDevice(this, this._name);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an OOCSI Device instance with the client's name that can be configured and then submitted to the OOCSI server.
        /// </summary>
        /// <param name="deviceName">Name of the created OOCSI device.</param>
        /// <returns>New OOCSI device instance</returns>
        public OOCSIDevice HeyOOCSI ( string deviceName ) {
            //return new OOCSIDevice(this, deviceName);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get all connected clients on the server.
        /// </summary>
        /// <returns>Connected clients.</returns>
        public string ListClients () {
            return this._socketClient.GetClients();
        }

        /// <summary>
        /// Get a list of all channels on the server.
        /// </summary>
        /// <returns></returns>
        public string ListChannels () {
            return this._socketClient.GetChannels();
        }

        /// <summary>
        /// Get a list of sub-channels of the given channel with {channelName}.
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public string ListChannels ( string channelName ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Logging
        /// </summary>
        /// <param name="msg"></param>
        private void Log ( string msg ) {
            this._logHandler?.Invoke(msg);
        }

    }

}
