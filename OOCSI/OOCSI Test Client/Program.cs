using OOCSI;

void Log ( string msg ) {
    Console.WriteLine(msg);
}

OOCSIClient client = new OOCSIClient(Log);
client.Connect();

Console.WriteLine(" > Press any key(s) to exit...");
Console.ReadKey();
