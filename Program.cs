using GetHackedPrototype;
using Npgsql;

const string dbUri = "Host=localhost;Port=5455;Username=postgres;Password=postgres;Database=gethacked;";

bool listen = false;

Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    listen = false;
    Console.WriteLine("Server closed");
};

await using var db = NpgsqlDataSource.Create(dbUri);

Server server = new(db);

server.Start();

listen = true;

while (listen) { }

<<<<<<< HEAD
server.Stop(); 
=======
server.Stop();
>>>>>>> 0e38d0f3649ae8b28151d822e132347be7e6474a
