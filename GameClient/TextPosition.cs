namespace GameClient;

public class TextPosition
{
    public static void Center(string output)
    {
        Console.WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (output.Length / 2)) + "}", output));
    }
}
