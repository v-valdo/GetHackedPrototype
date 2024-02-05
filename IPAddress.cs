namespace GetHackedPrototype;

public class IPAddress
{
    public string Generate()
    {
        Random rnd = new();
        int[] numbers = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        string ip = string.Empty;
        return $"{rnd.Next(1, 255)}.{rnd.Next(0, 255)}.{rnd.Next(0, 255)}.{rnd.Next(0, 255)}";
    }
}