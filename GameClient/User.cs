namespace GameClient;

public class User
{
    public string Register()
    {
        string? username = "";
        string? password = "";

        TextPosition.Center("Enter Username");
        username = Console.ReadLine();
        TextPosition.Center("Enter Password");
        password = Console.ReadLine();

        Console.WriteLine($"{username} created with password {password}");
        return $"username={username}&password={password}";
    }
}
