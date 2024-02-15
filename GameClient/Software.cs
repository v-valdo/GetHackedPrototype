namespace GameClient;
using System.Net.Http;
using System.Net.Http.Headers;
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
            using HttpResponseMessage response = await client.PostAsync(client.BaseAddress + "newuser", textContent);
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
            using (var requestMessage =
                        new HttpRequestMessage(HttpMethod.Get, "localhost:3000/ipscanner.exe"))
            {
                requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue(textContent.ToString());
                await client.SendAsync(requestMessage);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
