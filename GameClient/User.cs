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
        string? dummy = "";
        string? keyword = "";

        while (true)
        {
            Console.Clear();
            TextPosition.Center("Enter Username");
            username = Console.ReadLine();
            TextPosition.Center("Enter Password");
            password = Console.ReadLine();
            TextPosition.Center("Enter Dummy Password");
            dummy = Console.ReadLine();
            TextPosition.Center("Enter Keyword (6 characters)");
            keyword = Console.ReadLine();
            Console.WriteLine($"{username} created with password {password}");
            Thread.Sleep(250);
            if (username?.Length > 2 && password?.Length > 2 && dummy?.Length > 2 && keyword?.Length == 6)
            {
                return $"{username},{password},{dummy},{keyword}";
            }
            else
            {
                Console.WriteLine("Invalid user info, keep in mind - keyword has to be 6 characters");
                Thread.Sleep(300);
                continue;
            }
        }
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
        Console.Clear();
        User user = new();
        Console.WriteLine("enter username");
        user.Username = Console.ReadLine();
        Console.Clear();
        Console.WriteLine("enter password");
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
            using HttpResponseMessage response = await client.PostAsync(uri + "newuser", textContent);
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
                case "run statuscenter":
                    await Software.StatusCenter(_client, Uri, user);
                    break;
                case "run wallbreaker":
                    Console.Clear();
                    Console.Write("Enter target IP: ");
                    string ip = Console.ReadLine() ?? "0";
                    await Software.Attack(_client, Uri, user, ip);
                    break;
            }
        }
    }
    public void Help()
    {
        Console.Clear();
        Animation.Space();
        TextPosition.Center("Installed Commands");
        TextPosition.Center("> run ipscanner");
        TextPosition.Center("> run wallbreaker");
        TextPosition.Center("> run hideme");
        TextPosition.Center("> run keyencryptor");
        TextPosition.Center("> run hddwiper");
        TextPosition.Center("> stats");
        TextPosition.Center("> clear");
        TextPosition.Center("> exit");
        Console.WriteLine();
    }
}