using GameClient;

HttpClient client = new();
client.BaseAddress = new Uri("http://localhost:3000");
var server = client.BaseAddress;

TextPosition.Center("Welcome to GetHacked");
// GET REQUEST GENERAL (curl -X GET localhost:3000)
//await client.GetAsync(server);

// GET REQUEST SPECIFIC URL (curl -X GET localhost:3000/users)
//await client.GetAsync($"{server}/users");

// POST REQUEST (curl -d "username=hej&password=hej" POST localhost:3000)
User user = new();

string plainTextData = user.Register();

// Register
await user.WelcomeMenu();
