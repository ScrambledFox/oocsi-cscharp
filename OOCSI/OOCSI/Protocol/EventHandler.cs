using System;
using System.Collections.Generic;
using System.Text;

namespace OOCSI.Protocol {
    public abstract class EventHandler : Handler {

        public override void Receive ( string sender, Dictionary<string, object> data, long timestamp, string channel, string recipient ) {
            this.Receive(new OOCSIEvent(channel, data, sender, timestamp));
        }

        public abstract void Receive ( OOCSIEvent oocsiEvent );

    }
}
