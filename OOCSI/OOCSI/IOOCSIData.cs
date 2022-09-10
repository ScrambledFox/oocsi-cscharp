using System;

using System.Collections.Generic;
using System.Text;

namespace OOCSI {
    public interface IOOCSIData {

        IOOCSIData Data ( string key, string value );
        
        IOOCSIData Data ( string key, int value );
        
        IOOCSIData Data ( string key, float value );
        
        IOOCSIData Data ( string key, double value );
        
        IOOCSIData Data ( string key, long value );
        
        IOOCSIData Data ( string key, object value );

        IOOCSIData Data ( Dictionary<string, object> bulkData );

        Dictionary<string, object> Internal ();

    }
}
