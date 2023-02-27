using OOCSI;
using OOCSI.Client;

internal class Program {
    private static void Main ( string[] args ) {
        void Log ( string msg ) {
            Console.WriteLine(DateTime.Now.ToString() + " > " + msg);
        }

        void OnOOCSIMessage ( object sender, OOCSIMessageReceivedEventArgs args ) {
            Log($"Received a message from callback: {args}");
        }

        OOCSIClient client = new OOCSIClient(Log);

        client.OnMessageReceived += OnOOCSIMessage;

        //client.Connect("127.0.0.1", 4444);
        client.Connect("oocsi.id.tue.nl", 4444);

        client.Subscribe("testing", null);

        client.Send("testing", "{\"Message\":\"Hello World!\"}");
        client.Send("testing", "Hello World!");

        client.HeyOOCSI();
    }
}

