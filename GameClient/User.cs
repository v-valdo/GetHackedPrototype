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
        return $"{username},{password}";
    }
    public User Login(string username, string password)
    {
        User user = new();
        user.Username = username;
        user.Password = password;
        return user;
    }
    public static async Task WelcomeMenu()
    {
        while (true)
        {
            Console.Clear();
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
                        user.Username = parts[0];
                        user.Password = parts[1];
                        await user.PlayerMenu(user);
                        break;
                    case 2:
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
    public async Task PlayerMenu(User user)
    {
        Console.Clear();
        Console.WriteLine("1.Scan for IPs\n2.View Stats\n3.Attack IP");
        if (int.TryParse(Console.ReadLine(), out int r))
        {
            switch (r)
            {
                case 1:
                    await Software.IPScanner(_client, Uri, user);
                    break;
                default:
                    break;
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
            throw;
        }
    }
}
