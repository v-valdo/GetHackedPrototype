using System.Text;
namespace GameClient;

public class User
{
    private static HttpClient? _client { get; set; }
    private static Uri? Uri { get; set; }
    public string? Username;
    public string? Password;
    public User()
    {
        _client = new();
        _client.BaseAddress = new Uri("http://localhost:3000");
        Uri = _client.BaseAddress;
    }
    public string Register()
    {
        string? username = "";
        string? password = "";

        TextPosition.Center("Enter Username");
        username = Console.ReadLine();
        TextPosition.Center("Enter Password");
        password = Console.ReadLine();
        Console.WriteLine($"{username} created with password {password}");
        Thread.Sleep(250);
        return $"{username},{password}";
    }
    public async Task LoginAction(string username, string password)
    {
        User user = new();
        user.Username = username;
        user.Password = password;
        await user.PlayMenu(user);
    }
    public static User LoginMenu()
    {
        User user = new();
        user.Username = Console.ReadLine();
        user.Password = Console.ReadLine();
        return user;
    }
    public static async Task WelcomeMenu()
    {
        while (true)
        {
            Console.WriteLine("1.Register New User\n2.Login\n3.Exit Game");
            if (int.TryParse(Console.ReadLine(), out int r))
            {
                switch (r)
                {
                    case 1:
                        User user = new();

                        string? userDetails = user.Register();
                        await RegisterRequest(_client, Uri, userDetails);

                        string[] parts = userDetails.Split(",");
                        await user.LoginAction(parts[0], parts[1]);
                        break;
                    case 2:
                        User loginUser = LoginMenu();
                        await loginUser.LoginAction(loginUser.Username, loginUser.Password);
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                    default:
                        break;
                }
            }
        }
    }
    public static async Task RegisterRequest(HttpClient client, Uri uri, string data)
    {
        using StringContent textContent = new StringContent(data, Encoding.UTF8, "text/plain");
        try
        {
            using HttpResponseMessage response = await client.PostAsync(uri + "users/register", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task PlayMenu(User user)
    {
        while (true)
        {
            Animation.Space();
            TextPosition.Center("type 'h' for list of commands");
            Console.Write($"~/{user.Username}> ");
            string? command = Console.ReadLine();
            Software software = new();

            switch (command)
            {
                case "h":
                    Help();
                    break;
                case "clear":
                    Console.Clear();
                    break;
                case "run ipscanner":
                    await Software.IPScanner(_client, Uri, user);
                    break;
                case "run hideme":
                    //software.HideMe();
                    break;
                case "stats":
                    //Menu.Stats(hacker);
                    break;
                case "crash":
                    // Animation.Raided();
                    break;
                case "hacked":
                    //Animation.Hacked();
                    break;
            }
        }
    }
    public void Help()
    {
        Console.Clear();
        Animation.Space();
        TextPosition.Center("Installed Commands");
        TextPosition.Center("> run ipscraper");
        TextPosition.Center("> run defender");
        TextPosition.Center("> run hideme");
        TextPosition.Center("> run keyencryptor");
        TextPosition.Center("> run hddwiper");
        TextPosition.Center("> stats");
        TextPosition.Center("> clear");
        TextPosition.Center("> exit");
        Console.WriteLine();
    }
}