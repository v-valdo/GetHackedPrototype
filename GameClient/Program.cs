using GameClient;
Console.Clear();
Animation.Title();
Console.WriteLine();
Console.WriteLine();
// GET REQUEST GENERAL (curl -X GET localhost:3000)
//await client.GetAsync(server);

// GET REQUEST SPECIFIC URL (curl -X GET localhost:3000/users)
//await client.GetAsync($"{server}/users");

// POST REQUEST (curl -d "username=hej&password=hej" POST localhost:3000)
// MAIN MENU
User loggedOutUser = new();
await loggedOutUser.WelcomeMenu();
