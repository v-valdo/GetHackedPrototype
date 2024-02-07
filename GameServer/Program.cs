using GetHackedPrototype;
using Npgsql;

bool listen = true;
const string dbUri = "Host=localhost;Port=5455;Username=postgres;Password=postgres;Database=gethacked;";

/// Handle ctrl + c interrupt event, and gracefully shut down server
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
{
    Console.WriteLine("Interrupting cancel event");
    e.Cancel = true;
    listen = false;
};

var db = NpgsqlDataSource.Create(dbUri);

RequestHandler requestHandler = new(db);

try
{
    requestHandler.Start();
    while (listen) { };
    requestHandler.Stop();
}
finally
{

}

