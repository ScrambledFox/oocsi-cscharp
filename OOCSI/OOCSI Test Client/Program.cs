using OOCSI;

void Log ( string msg ) {
    Console.WriteLine(System.DateTime.Now.ToString() + " > " + msg);
}

void MessageCallback ( string message ) {
    Console.WriteLine(message);
}

OOCSIClient client = new OOCSIClient(Log);
//client.Connect("127.0.0.1", 4444);
client.Connect("oocsi.id.tue.nl", 4444);

client.Subscribe("testing", MessageCallback);
client.Send("testing", "{\"Message\":\"Hello World!\"}");
client.Send("testing", "Hello World!");

//Console.WriteLine(" > Press any key(s) to exit...");
//Console.ReadKey();
