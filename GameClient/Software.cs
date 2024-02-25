namespace GameClient;
using System.Net.Http;
using System.Net.Mime;
using System.Text;

public class Software
{
    public string? Name;
    public Software()
    {

    }
    public async Task Register(HttpClient client, Uri uri, string data)
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
    public async Task IPScanner(HttpClient client, Uri uri, User user)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software IPScanner = new();
        IPScanner.Name = "IPScanner_v1425.exe";

        Animation.Loading(IPScanner);
        try
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"{uri}ipscanner.exe"),
                Content = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, MediaTypeNames.Application.Json /* or "application / json" in older versions */),
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine(responseBody);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task Notepad(HttpClient client, Uri uri, User user)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        try
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"{uri}notepad.exe"),
                Content = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, MediaTypeNames.Application.Json /* or "application / json" in older versions */),
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine("MY NOTEPAD:");
            Console.WriteLine("----------");
            Console.WriteLine(responseBody);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task AutoDecryptor(HttpClient client, Uri uri, User user, string ip, string password)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software software = new();
        software.Name = "AutoDecryptor.exe";

        Animation.Loading(software);

        try
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"{uri}autodecrypt.exe/{ip}/{password}"),
                Content = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, MediaTypeNames.Application.Json /* or "application / json" in older versions */),
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine(responseBody);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task StatusCenter(HttpClient client, Uri uri, User user)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software StatusCenter = new();
        StatusCenter.Name = "StatusCenter_V2.exe";

        Animation.Loading(StatusCenter);

        try
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"{uri}statuscenter.exe"),
                Content = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, MediaTypeNames.Application.Json /* or "application / json" in older versions */),
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine(responseBody);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task Attack(HttpClient client, User user, string ip)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software software = new();
        software.Name = "WallBreaker_v0.2.exe";
        Animation.Loading(software);

        try
        {
            using HttpResponseMessage response = await client.PutAsync(client.BaseAddress + $"attack/{ip}", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");

            if (jsonResponse.Contains("You have been detected by the authorities!"))
            {
                Animation.Raided();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task HideMe(HttpClient client, User user)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software software = new();
        software.Name = "HideMe_0.9.exe";
        Animation.Loading(software);

        try
        {
            using HttpResponseMessage response = await client.PutAsync(client.BaseAddress + $"hideme.exe", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task Heal(HttpClient client, User user)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software software = new();
        software.Name = "FireWallFirmwareUpdator.exe";
        Animation.Loading(software);
        try
        {
            using HttpResponseMessage response = await client.PutAsync(client.BaseAddress + $"updatefirewall.exe", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task Inject(HttpClient client, User user, string ip, string dummyPassword)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software software = new();
        software.Name = "ssh-injectscript.exe";
        Animation.Loading(software);
        try
        {
            using HttpResponseMessage response = await client.PutAsync(client.BaseAddress + $"injector.exe/{dummyPassword}/{ip}", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    public async Task RestoreDetection(HttpClient client, User user)
    {
        using StringContent textContent = new StringContent($"{user.Username},{user.Password}", Encoding.UTF8, "text/plain");
        Software software = new();
        software.Name = "DetectionUpdator_v1.0.exe";
        Animation.Loading(software);
        try
        {
            using HttpResponseMessage response = await client.PutAsync(client.BaseAddress + $"restoredetection.exe", textContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{jsonResponse}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}