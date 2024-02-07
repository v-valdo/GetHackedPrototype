using System.Net.Http;
using System.Text;
using System.Text.Json;

HttpClient client = new();
client.BaseAddress = new Uri("http://localhost:3000");
var server = client.BaseAddress;

// curl -X GET localhost:3000
//await client.GetAsync(server);

// curl -X GET localhost:3000/users
//await client.GetAsync($"{server}/users");

// curl -d "username=hej&password=hej" POST localhost:3000
string plainTextData = "username=hej&password=hej";
using StringContent textContent = new StringContent(plainTextData, Encoding.UTF8, "text/plain");

using HttpResponseMessage response = await client.PostAsync(server, textContent);

var jsonResponse = await response.Content.ReadAsStringAsync();
Console.WriteLine($"{jsonResponse}\n");