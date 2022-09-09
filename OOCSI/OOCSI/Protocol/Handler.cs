using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace OOCSI.Protocol
{
    abstract public class Handler {

        public static Dictionary<string, object> ParseData ( string data ) {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
        }

    }
}
