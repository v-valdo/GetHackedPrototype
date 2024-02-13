namespace GameClient;

public class User
{
    private HttpClient? _client { get; set; }
    private Uri Uri { get; set; }
    private string Username;
    private string Password;
    public User()
    {
        _client = new();
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
    public async Task WelcomeMenu()
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
                        await Software.Register(_client, Uri, Register());
                        User user =
                        await PlayerMenu();
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
                    await Software.IPScanner(_client, Uri,);
                    break;
                default:
                    break;
            }
        }
    }
}
