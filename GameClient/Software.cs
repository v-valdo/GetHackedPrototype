namespace GameClient;
using System.Text;
public class Software
{
    public string? Name;
    public Software()
    {

    }
    public static async Task Register(HttpClient client, Uri uri, string data)
    {
        using StringContent textContent = new StringContent(data, Encoding.UTF8, "text/plain");
        try
        {
            using HttpResponseMessage response = await client.PostAsync(client.BaseAddress + "users/register", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
    public static async Task IPScanner(HttpClient client, Uri uri, User user)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software IPScanner = new();
        IPScanner.Name = "IPScanner_v1425.exe";
        Animation.Loading(IPScanner);
        try
        {
            using HttpResponseMessage response = await client.PutAsync(uri + "ipscanner.exe", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
