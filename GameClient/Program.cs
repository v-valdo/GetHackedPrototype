HttpClient client = new();
client.BaseAddress = new Uri("http://localhost:3000");
var server = client.BaseAddress;

Console.WriteLine(server);

// curl -X GET localhost:3000
await client.GetAsync(server);