using System;
using System.Collections.Generic;
using System.Text;
using OOCSI.Client;

namespace OOCSI.Protocol {
    public class OOCSIMessage : OOCSIEvent, IOOCSIData {

        protected OOCSIClient _oocsi;
        private bool _isSent = false;

        public OOCSIMessage ( OOCSIClient oocsi, string channelname ) : base(channelname, new Dictionary<string, object>(), "") {
            this._oocsi = oocsi;
        }


        public IOOCSIData Data ( string key, string value ) => throw new NotImplementedException();

        public IOOCSIData Data ( string key, int value ) => throw new NotImplementedException();

        public IOOCSIData Data ( string key, float value ) => throw new NotImplementedException();

        public IOOCSIData Data ( string key, double value ) => throw new NotImplementedException();

        public IOOCSIData Data ( string key, long value ) => throw new NotImplementedException();

        public IOOCSIData Data ( string key, object value ) => throw new NotImplementedException();

        public IOOCSIData Data ( Dictionary<string, object> bulkData ) => throw new NotImplementedException();

        public Dictionary<string, object> Internal () => throw new NotImplementedException();

        public void Send () {
            if ( !this._isSent ) {
                this._isSent = true;
                this._oocsi.Send(this._channelname, this._data);
            }
        }
    }
}
