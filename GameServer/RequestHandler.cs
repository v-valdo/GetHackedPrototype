using GameServer;
using Npgsql;
using System.Net;
using System.Text;
namespace GetHackedPrototype;

public class RequestHandler
{
    private readonly NpgsqlDataSource? _db;
    public int port = 3000;
    private HttpListener _listener = new();
    private readonly UserAction _action;

    public RequestHandler(NpgsqlDataSource db)
    {
        _db = db;
        _action = new UserAction(_db);
    }

    public void Start()
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        Console.WriteLine($"Server listening on port {port}");
        _listener.BeginGetContext(new AsyncCallback(Route), _listener);
    }
    public void Stop() => _listener.Stop();
    private async void Route(IAsyncResult result)
    {
        var context = _listener.EndGetContext(result);
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath;

        switch (request.HttpMethod)
        {
            case "GET":
                Console.WriteLine($"get request received - {request.RawUrl}");
                await Get(response, request);
                break;
            case "POST":
                Console.WriteLine($"post request received to {request.RawUrl}");
                await Post(response, request);
                break;
            case "PUT":
                Console.WriteLine($"put request received to {request.RawUrl}");
                await Put(response, request);
                break;
        }
    }
    private async Task Get(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = await ReadRequestData(request);
    }
    private async Task Post(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = await ReadRequestData(request);

        if (path.Contains("ipscanner.exe"))
        {
            message = await _action.IPScanner(path, parts, response);
            Print(response, message);
        }

        // Register User: curl -X POST http://localhost:3000/newuser -d 'username,password,dummy_password,keyword'
        if (path.Contains("newuser"))
        {
            message = await _action.Register(path, parts, response, this);
            Print(response, message);
        }

        if (path.Contains("statuscenter.exe"))
        {
            message = await _action.ShowStats(path, parts, response);
            Print(response, message);
        }
    }
    private async Task Put(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = await ReadRequestData(request);

        if (path.Contains("attack")) //Attack! curl -X PUT http://localhost:3000/attack/targetIP -d 'username,password'
        {
            message = await _action.Attack(path, parts, response);
            Print(response, message);
        }

        if (path.Contains("hide-me.exe"))
        {
            message = await _action.HideMe(path, parts, response, this);
            Print(response, message);
        }
        if (path.Contains("heal"))
        {
            message = await _action.Heal(request, path, parts, response);
            Print(response, message);
        }
    }
    public IPAddress Generate()
    {
        Random random = new Random();
        byte[] ipBytes = new byte[4];
        random.NextBytes(ipBytes);
        return new IPAddress(ipBytes);
    }
    public async Task GeneratePoliceIP()
    {
        const string qCountUsers = @$"SELECT COUNT(*) FROM users u WHERE u.id > 0";
        const string qAddPoliceIp = "INSERT INTO ip(address,user_id) VALUES ($1, $2)";

        var cmdCountUsers = _db.CreateCommand(qCountUsers);
        var rowCount = await cmdCountUsers.ExecuteScalarAsync();
        int rowCountInt = Convert.ToInt32(rowCount);

        if (rowCountInt % 3 == 1)
        {
            IPAddress generatedPoliceIP = Generate();
            string policeIp = generatedPoliceIP.ToString();
            await using var cmd = _db.CreateCommand(qAddPoliceIp);
            cmd.Parameters.AddWithValue(policeIp);
            cmd.Parameters.AddWithValue(0);
            await cmd.ExecuteNonQueryAsync();
        }
    }
    private void Print(HttpListenerResponse response, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.ContentType = "text/plain";
        response.StatusCode = (int)HttpStatusCode.OK;

        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
        _listener.BeginGetContext(new AsyncCallback(Route), _listener);
    }
    private async Task<(string path, string[] parts)> ReadRequestData(HttpListenerRequest request)
    {
        var path = request.Url?.AbsolutePath ?? "404";
        string data;

        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            data = await reader.ReadToEndAsync();
        }

        return (path, data.Split(","));
    }
}

