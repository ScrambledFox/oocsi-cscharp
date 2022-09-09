using OOCSI;

void Log ( string msg ) {
    Console.WriteLine(msg);
}

OOCSIClient client = new OOCSIClient(Log);
client.Connect("127.0.0.1", 4444);

client.Send("testing", "{\"Message\":\"Hello World!\"}");

Console.WriteLine(" > Press any key(s) to exit...");
Console.ReadKey();
