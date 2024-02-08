using GameClient;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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
using StringContent textContent = new StringContent(plainTextData, Encoding.UTF8, "text/plain");

using HttpResponseMessage response = await client.PostAsync(server + "/users/register", textContent);

var jsonResponse = await response.Content.ReadAsStringAsync();
Console.WriteLine($"{jsonResponse}\n");