using Newtonsoft.Json.Linq;
using OOCSI.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace OOCSI.Data {
    /// <summary>
    /// OOCSIDevice allows to configure one or more devices for an OOCSI client that can be recognized by HomeAssistant (and
    /// the OOCSI server) and will then be displayed or treated otherwise in a semantically correct way.
    /// </summary>
    public class OOCSIDevice {

        OOCSIClient _client;
        string _name;

        Dictionary<string, string> _properties = new Dictionary<string, string>();
        Dictionary<string, float[]> _location = new Dictionary<string, float[]>();
        Dictionary<string, object> _components = new Dictionary<string, object>();

        /// <summary>
        /// Create a new OOCSI device.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <param name="Log"></param>
        public OOCSIDevice ( OOCSIClient client, string name ) {
            this._client = client;
            this._name = name;

            _properties.Add("device_id", client.Name);

            Log($"Created device with name {client.Name}");
            // Add device id to properties;
        }

        /// <summary>
        /// Add a device property.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddProperty ( string name, string value ) {
            _properties.Add(name, value);
            Log($"Added property with name {name} and value {value}.");
        }

        /// <summary>
        /// Add a location to this device.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        public void AddLocation ( string name, float lat, float lng ) {
            _location.Add(name, new float[] { lat, lng });
            Log($"Added location with name {name} and lat {lat} and long {lng}.");
        }

        /// <summary>
        /// Add a number component to this device.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="channel"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="numberDefault"></param>
        /// <param name="icon"></param>
        public void AddNumber ( string name, string channel, float min, float max, float numberDefault, string icon ) {
            this.AddNumber(name, channel, min, max, "", numberDefault, icon);
        }

        /// <summary>
        /// Add a number component to this device.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="channel"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="numberUnit"></param>
        /// <param name="numberDefault"></param>
        /// <param name="icon"></param>
        public void AddNumber ( string name, string channel, float min, float max, string numberUnit, float numberDefault, string icon ) {
            Dictionary<string, object> componentDictionary = new Dictionary<string, object>();
            componentDictionary.Add("channel_name", channel);
            componentDictionary.Add("type", "number");
            componentDictionary.Add("unit", numberUnit);
            componentDictionary.Add("min_max", new float[] { min, max });
            componentDictionary.Add("value", numberDefault);
            componentDictionary.Add("icon", icon);

            _components.Add(name, componentDictionary);
        }

        /// <summary>
        /// Submit the heyOOCSI! message to the server.
        /// </summary>
        public void Submit () {
            Dictionary<string, object> messageData = new Dictionary<string, object>();
            messageData.Add("properties", _properties);
            messageData.Add("components", _components);
            messageData.Add("location", _location);

            _client.Send("heyOOCSI!", messageData);
            Log("Sent heyOOCSI! message.");
        }


        /// <summary>
        /// Alternative call for Submit.
        /// </summary>
        public void SayHi () {
            this.Submit();
        }


        private void Log ( string message ) {
            _client.Log($" - heyOOCSI ({_name}): {message}");
        }

    }
}
