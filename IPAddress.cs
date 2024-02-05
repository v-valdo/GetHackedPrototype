namespace GetHackedPrototype;

public class IPAddress
{
    public string Generate()
    {
        Random rnd = new();
        List<string> IPList = File.ReadAllLines("IPList.txt").ToList();
        int IPGrabber = rnd.Next(IPList.Count);
        string randomIp = IPList[rnd.Next(IPList.Count)];
        IPList.RemoveAt(IPGrabber);
        return randomIp;
    }
}