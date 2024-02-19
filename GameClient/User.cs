using System.Text;
namespace GameClient;
public class User
{
    private static HttpClient _client = new HttpClient();
    private static Uri _baseUri = new Uri("http://localhost:3000");
    public string? Username;
    public string? Password;

    static User()
    {
        _client.BaseAddress = _baseUri;
    }
    public string Register()
    {
        while (true)
        {
            Console.Clear();
            TextPosition.Center("Enter Username");
            string? username = Console.ReadLine();
            TextPosition.Center("Enter Password");
            string? password = Console.ReadLine();
            TextPosition.Center("Enter Dummy Password");
            string? dummy = Console.ReadLine();
            TextPosition.Center("Enter Keyword (6 characters)");
            string? keyword = Console.ReadLine();
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
    public async Task WelcomeMenu()
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
                        await RegisterRequest(_client, _baseUri, userDetails);

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
                case "run injector":
                    Console.Clear();
                    var (injectIP, password) = Prompts.InjectorPrompt();
                    await software.Inject(_client, user, injectIP, password);
                    break;
                case "clear":
                    Console.Clear();
                    break;
                case "run ipscanner":
                    Console.Clear();
                    await software.IPScanner(_client, _baseUri, user);
                    break;
                case "run hideme":
                    Console.Clear();
                    await software.HideMe(_client, user);
                    break;
                case "run statuscenter":
                    await software.StatusCenter(_client, _baseUri, user);
                    break;
                case "run wallbreaker":
                    string breakIP = Prompts.AttackPrompt();
                    await software.Attack(_client, user, breakIP);
                    break;
                case "firewallpatcher":
                    await software.Heal(_client, user);
                    break;
                case "logout":
                    Console.Clear();
                    await WelcomeMenu();
                    break;
                default:
                    Console.WriteLine("Command not found");
                    break;
            }
        }
    }
    public void Help()
    {
        Console.Clear();
        Animation.Space();
        TextPosition.Center("Installed Commands");
        TextPosition.Center("> run ipscanner"); // ipscanner
        TextPosition.Center("> run wallbreaker"); // Attack IP
        TextPosition.Center("> run hideme"); // reset IP
        TextPosition.Center("> run decryptor"); // decrypts  
        TextPosition.Center("> run statuscenter"); // show stats
        TextPosition.Center("> clear"); // clears console
        TextPosition.Center("> exit");
        Console.WriteLine();
    }
}