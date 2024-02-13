using GameClient;

public class Animation
{
    public static void Loading(Software software)
    {
        for (int i = 0; i < 100; i++)
        {
            Console.WriteLine($"Configuring {software.Name}");
            Console.WriteLine();
            Console.Write(i + "%");
            Thread.Sleep(20);
            Console.Clear();
        }
    }
    public static void Raided()
    {
        string[] hacked = File.ReadAllLines("../../../ASCII/Raided.txt");
        foreach (var line in hacked)
        {
            Console.WriteLine(line);
            Thread.Sleep(26);
        }
        Console.WriteLine();
        Console.WriteLine("The local police department has raided you and seized all your hardware.");
        Console.ReadLine();
    }
    public static void Title()
    {
        string[] title = File.ReadAllLines("../../../ASCII/Title.txt");
        foreach (var line in title)
        {
            Console.WriteLine(line);
            Thread.Sleep(50);
        }
    }
    public static void Space()
    {
        Console.WriteLine();
        Console.WriteLine();
    }
    public static void Hacked()
    {
        string[] c = File.ReadAllLines(@"../../../ASCII/Hacked.txt");

        while (true)
        {
            for (int i = 37; i < 50; i++)
            {
                foreach (var item in c)
                {
                    Console.WriteLine(item);
                }
                Console.Beep();
                Console.BackgroundColor = ConsoleColor.White;
                Thread.Sleep(50);
                Console.Clear();
                Console.Beep(i, 25);
                Console.BackgroundColor = ConsoleColor.Yellow;
                Thread.Sleep(50);
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Green;
                Thread.Sleep(50);
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Red;
                Thread.Sleep(50);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Thread.Sleep(50);
                Console.Clear();
            }
        }
    }
}