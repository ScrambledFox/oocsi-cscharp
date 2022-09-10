using System;
using System.Collections.Generic;
using System.Text;

namespace OOCSI.Protocol {
    public abstract class DataHandler : Handler {

        public override void Receive ( string sender, Dictionary<string, object> data, long timestamp, string channel, string recipient ) {
            this.Receive(sender, data, timestamp);
        }

        public abstract void Receive ( string sender, Dictionary<string, object> data, long timestamp);

    }
}
