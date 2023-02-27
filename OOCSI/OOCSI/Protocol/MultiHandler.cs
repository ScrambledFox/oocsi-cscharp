using System;
using System.Collections.Generic;
using System.Text;

namespace OOCSI.Protocol {
    public class MultiHandler : Handler {

        private List<Handler> _subscribers = new List<Handler>();

        public MultiHandler () {
        }

        public MultiHandler ( Handler handler ) {
            this.Add(handler);
        }

        public void Add ( Handler handler ) {
            _subscribers.Add(handler);
        }

        public void Remove ( Handler handler ) {
            _subscribers.Remove(handler);
        }

        public bool IsEmpty () => _subscribers.Count == 0;

        public override void Receive ( string sender, Dictionary<string, object> data, long timestamp, string channel, string recipient ) {
            foreach ( Handler handler in this._subscribers ) {
                handler.Receive(sender, data, timestamp, channel, recipient);
            }
        }
    }
}
