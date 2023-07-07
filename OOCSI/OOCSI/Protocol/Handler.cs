using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace OOCSI.Protocol {
    abstract public class Handler {

        public void Send ( string sender, string data, string timestamp, string channel ) {
            try {
                Dictionary<string, object> dict = ParseData(data);
                long ts = ParseTimestamp(timestamp);


                this.Receive(sender, dict, ts, channel);
            } catch ( Exception e ) {
                throw e;
            }
        }

        abstract public void Receive ( string sender, Dictionary<string, object> data, long timestamp, string channel );

        public static Dictionary<string, object> ParseData ( string data ) {
            var dat = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            return dat;
        }

        public static long ParseTimestamp ( string timestamp ) {
            long ts = DateTime.Now.Millisecond;

            try {
                ts = long.Parse(timestamp);
            } catch ( Exception e ) {
                throw e;
            }

            return ts;
        }

    }
}
