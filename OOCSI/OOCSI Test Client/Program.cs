using OOCSI;
using OOCSI.Client;
using OOCSI.Protocol;
using System.Collections.Generic;

internal class Program {
    private static void Main ( string[] args ) {
        void Log ( string msg ) {
            Console.WriteLine(DateTime.Now.ToString() + " > " + msg);
        }

        void OnMessage ( OOCSIEvent oocsiEvent ) {
            Log(oocsiEvent.GetInt("color", 0).ToString());
        }

        OOCSIClient client = new OOCSIClient(Log);

        //client.Connect("127.0.0.1", 4444);
        client.Connect("oocsi.id.tue.nl", 4444);

        client.Subscribe("testing", OnMessage);

        //client.Send("testing", "{\"Message\":\"Hello World!\"}");
        //client.Send("testing", "Hello World!");

        //client.SubscribeToSelf(OnMessage);

        //client.Send(client.Name, "{\"Message\":\"Hello Me!\"}");

        //client.HeyOOCSI();
    }

}
