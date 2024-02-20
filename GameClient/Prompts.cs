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
        Console.Clear();
        Console.Write("Enter target IP: ");
        string ip = Console.ReadLine() ?? "0";
        Console.WriteLine("Enter target Password: ");
        string password = Console.ReadLine() ?? "0";
        return (ip, password);
    }
}
