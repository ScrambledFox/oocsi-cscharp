using System;
using System.Collections.Generic;

namespace OOCSI {
    public class OOCSIEvent {

        protected string _channelname;
        protected string _sender;

        protected Dictionary<string, object> _data;
        protected DateTime _timestamp;

        public OOCSIEvent ( string channelname, Dictionary<string, object> data, string sender, long timestamp ) : this(channelname, data, sender, new DateTime(timestamp)) { }

        public OOCSIEvent ( string channelname, Dictionary<string, object> data, string sender ) : this(channelname, data, sender, DateTime.Now) { }

        public OOCSIEvent ( string channelname, Dictionary<string, object> data, string sender, DateTime timestamp ) {
            this._channelname = channelname;
            this._sender = sender;
            this._data = data;
            this._timestamp = timestamp;
        }

        public bool GetBool ( string key, bool defaultValue ) {
            object result = this.GetObject(key);
            if ( result != null ) {
                if ( result is bool ) {
                    return ((bool)result);
                } else {
                    try {
                        return bool.Parse(result.ToString());
                    } catch ( Exception e ) {
                        return defaultValue;
                    }
                }
            } else {
                return defaultValue;
            }
        }

        public int GetInt ( string key, int defaultValue ) {
            object result = this.GetObject(key);
            if ( result != null ) {
                if ( result is int ) {
                    return ((int)result);
                } else {
                    try {
                        return int.Parse(result.ToString());
                    } catch ( Exception ) {
                        return defaultValue;
                    }
                }
            } else {
                return defaultValue;
            }
        }

        public long GetLong ( string key, long defaultValue ) {
            object result = this.GetObject(key);
            if ( result != null ) {
                if ( result is long ) {
                    return ((long)result);
                } else {
                    try {
                        return long.Parse(result.ToString());
                    } catch ( Exception ) {
                        return defaultValue;
                    }
                }
            } else {
                return defaultValue;
            }
        }

        public float GetFloat ( string key, float defaultValue ) {
            object result = this.GetObject(key);
            if ( result != null ) {
                if ( result is float ) {
                    return ((float)result);
                } else {
                    try {
                        return float.Parse(result.ToString());
                    } catch ( Exception ) {
                        return defaultValue;
                    }
                }
            } else {
                return defaultValue;
            }
        }

        public double GetDouble ( string key, double defaultValue ) {
            object result = this.GetObject(key);
            if ( result != null ) {
                if ( result is double ) {
                    return ((double)result);
                } else {
                    try {
                        return double.Parse(result.ToString());
                    } catch ( Exception ) {
                        return defaultValue;
                    }
                }
            } else {
                return defaultValue;
            }
        }

        public string GetString ( string key ) {
            object result = this.GetObject(key);
            return result != null ? result.ToString() : null;
        }

        public string GetString ( string key, string defaultValue ) {
            object result = this.GetObject(key);
            return result != null ? result.ToString() : defaultValue;
        }

        //public bool[] GetBoolArray ( string key, bool[] defaultValue ) {
        //    try {
        //        object obj = this.GetObject(key);
        //    } catch ( Exception ) {
        //        return defaultValue;
        //    }
        //}

        private object GetObject ( string key ) {
            object obj = null;
            this._data.TryGetValue(key, out obj);
            return obj;
        }
    }

    public sealed class OOCSIMessageReceivedEventArgs : EventArgs {
        public OOCSIMessageReceivedEventArgs ( string channel, string sender, Dictionary<string, object> data, DateTime timestamp ) {

        }
    }

}
