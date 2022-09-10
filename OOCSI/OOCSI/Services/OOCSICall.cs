using System;
using System.Collections.Generic;
using System.Text;

using OOCSI.Protocol;

namespace OOCSI.Services {
    public class OOCSICall : OOCSIMessage {

        public static readonly string MESSAGE_HANDLE = "_MESSAGE_HANDLE";
        public static readonly string MESSAGE_ID = "_MESSAGE_ID";

        enum CALL_MODE {
            call_return, call_multi_return
        }

        private string uuid = "";
        private long expiration = 0;
        private int maxResponses = 1;
        private OOCSIEvent response = null;

        public OOCSICall ( OOCSIClient oocsi, string callname, int timeoutMS, int maxResponses ) : this(oocsi, callname, callname, timeoutMS, maxResponses) { }

        public OOCSICall ( OOCSIClient oocsi, string channelname, string callname, int timeoutMS, int maxResponses ) : base(oocsi, channelname) {
            this.Data(OOCSICall.MESSAGE_HANDLE, callname);

            this.expiration = System.DateTime.Now.Millisecond + timeoutMS;
            this.maxResponses = maxResponses;
        }

        public string GetID () {
            return uuid;
        }

        public void Respond ( Dictionary<string, object> data ) {
            response = new OOCSIEvent(this._sender, data, this._channelname);
        }

        //public bool CanSend () {
        //}

    }
}
