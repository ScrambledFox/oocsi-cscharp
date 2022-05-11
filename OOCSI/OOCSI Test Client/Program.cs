using OOCSI;

void Log ( string msg ) {
    Console.WriteLine(msg);
}

OOCSIClient client = new OOCSIClient(Log);

Console.WriteLine(" > Press any key(s) to exit...");
Console.ReadKey();
