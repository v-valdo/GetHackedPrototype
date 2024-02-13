namespace GameClient;
using System.Text;
public class Software
{
    public static async Task Register(HttpClient client, Uri url, string data)
    {
        using StringContent textContent = new StringContent(data, Encoding.UTF8, "text/plain");
        try
        {
            using HttpResponseMessage response = await client.PostAsync(client.BaseAddress + "/users/register", textContent);
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

    }
}
