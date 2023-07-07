using System;
using System.Collections.Generic;
using System.Reflection;

using OOCSI.Sockets;
using OOCSI.Protocol;
using OOCSI.Services;
using OOCSI.Data;
using System.Runtime.CompilerServices;

namespace OOCSI.Client {
    public delegate void MessageCallback ( OOCSIEvent oocsiEvent );

    public class OOCSIClient {
        private SocketClient _socketClient;
        private string _name;

        public string Name { get => _name; }
        public bool IsConnected => _socketClient.IsConnected;
        public bool IsReconnecting => _socketClient.ShouldReconnect;

        public delegate void LoggingDelegate ( string msg );
        private LoggingDelegate _logHandler;

        public event EventHandler<OOCSIMessageReceivedEventArgs> OnMessageReceived;

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
            _logHandler = logHandler;

            // Set a standard random name if none has been set yet.
            if ( name == null || name.Length == 0 ) {
                name = "OOCSICSharp_" + Guid.NewGuid().ToString().Substring(0, 5);
            }

            if ( name.Contains(" ") ) {
                Log("[ERROR] OOCSI name cannot contain spaces");
                Log(" - OOCSI connection aborted");
                return;
            }

            _name = name;
            _socketClient = new SocketClient(name, logHandler);

            var assembly = Assembly.GetExecutingAssembly();
            var informationVersion = assembly.
                GetCustomAttribute<AssemblyInformationalVersionAttribute>().
                InformationalVersion;

            Log("OOCSI-CSHARP client v" + informationVersion + " started with client handle: " + name);
        }

        /// <summary>
        /// Connects to OOCSI network based on multi-cased messages broadcasting server ip.
        /// </summary>
        /// <returns>True if client connected succesfully.</returns>
        public bool Connect () {
            return _socketClient.StartMulticastLookup();
        }

        /// <summary>
        /// Connects to {hostName} on port 4444.
        /// </summary>
        /// <param name="hostname">Server URL to connect to.</param>
        /// <param name="port">Port where the server is running on.</param>
        /// <returns>True if client connected succesfully.</returns>
        public bool Connect ( string hostname ) {
            return _socketClient.Connect(hostname);
        }

        /// <summary>
        /// Connects to {hostName} on port {port}.
        /// </summary>
        /// <param name="hostname">Server URL to connect to.</param>
        /// <param name="port">Port where the server is running on.</param>
        /// <returns>True if client connected succesfully.</returns>
        public bool Connect ( string hostname, int port ) {
            return _socketClient.Connect(hostname, port);
        }

        /// <summary>
        /// Disconnects from the OOCSI Server.
        /// </summary>
        /// <returns>True if client disconnected succesfully.</returns>
        public void Disconnect () {
            _socketClient.Disconnect();
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
        /// <param name="callback">Callback handler for incoming data.</param>
        public void Subscribe ( string channel, MessageCallback callback ) { 
            _socketClient.Subscribe( channel, callback );
        }

        /// <summary>
        /// Subscribe to self. Direct-To-Direct Communication.
        /// </summary>
        /// <param name="messageCallback"></param>
        public void SubscribeToSelf ( MessageCallback messageCallback ) {
            _socketClient.SubscribeToSelf(messageCallback);
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
                _socketClient.Send(channelName, message);
            }
        }

        /// <summary>
        /// Send a data object to a channel.
        /// </summary>
        /// <param name="channelName">Channel to sent the data to.</param>
        /// <param name="data">Data to send as a dictionary with a string key and a general object value.</param>
        public void Send ( string channelName, Dictionary<string, object> data ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an OOCSI Device instance with the client's name that can be configured and then submitted to the OOCSI server.
        /// </summary>
        /// <returns>New OOCSI device instance</returns>
        public OOCSIDevice HeyOOCSI () {
            return new OOCSIDevice(this, _name);
        }

        /// <summary>
        /// Create an OOCSI Device instance with the client's name that can be configured and then submitted to the OOCSI server.
        /// </summary>
        /// <param name="deviceName">Name of the created OOCSI device.</param>
        /// <returns>New OOCSI device instance</returns>
        public OOCSIDevice HeyOOCSI ( string deviceName ) {
            return new OOCSIDevice(this, deviceName);
        }

        /// <summary>
        /// Get all connected clients on the server.
        /// </summary>
        /// <returns>Connected clients.</returns>
        public string ListClients () {
            return _socketClient.GetClients();
        }

        /// <summary>
        /// Get a list of all channels on the server.
        /// </summary>
        /// <returns></returns>
        public string ListChannels () {
            return _socketClient.GetChannels();
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
        /// Logs a message to the logging delegate.
        /// </summary>
        /// <param name="msg"></param>
        public void Log ( string msg ) {
            _logHandler?.Invoke(msg);
        }

    }

}
