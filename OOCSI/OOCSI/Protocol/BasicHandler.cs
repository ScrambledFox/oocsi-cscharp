using OOCSI.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace OOCSI.Protocol {
    public class BasicHandler : Handler {

        private MessageCallback _callback;

        public BasicHandler( MessageCallback cb ) { 
            this._callback = cb;
        }

        public override void Receive ( string sender, Dictionary<string, object> data, long timestamp, string channel ) {
            this._callback(new OOCSIEvent(channel, data, sender));
        }

    }
}
