namespace GameClient;

public class Prompts
{
    public static string AttackPrompt()
    {
        Console.Clear();
        Console.Write("Enter target IP: ");
        string ip = Console.ReadLine() ?? "0";
        return ip;
    }
    public static (string ip, string password) InjectorPrompt()
    {
        Console.Write("Enter target IP: ");
        string ip = Console.ReadLine() ?? "0";
        Console.WriteLine("Enter target Password: ");
        string password = Console.ReadLine() ?? "0";
        return (ip, password);
    }
    public static (string ip, string password) DecryptorPrompt()
    {
        Console.Write("Enter target IP: ");
        string ip = Console.ReadLine() ?? "0";
        Console.WriteLine("Enter target Keyword: ");
        string password = Console.ReadLine() ?? "0";
        return (ip, password);
    }
}
