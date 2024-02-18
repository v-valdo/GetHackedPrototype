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
    private readonly UserAction _user;
    private Police _police;
    public RequestHandler(NpgsqlDataSource db)
    {
        _db = db;
        _user = new UserAction(_db);
        _police = new(_db);
    }

    public void Start()
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        Console.WriteLine($"Server listening on port {port}");
        _listener.BeginGetContext(new AsyncCallback(Route), _listener);
    }
    public void Stop() => _listener.Stop();
    private void Route(IAsyncResult result)
    {
        var context = _listener.EndGetContext(result);
        var request = context.Request;
        var response = context.Response;

        switch (request.HttpMethod)
        {
            case "GET":
                Console.WriteLine($"get request received - {request.RawUrl}");
                Get(response, request);
                break;
            case "POST":
                Console.WriteLine($"post request received to {request.RawUrl}");
                Post(response, request);
                break;
            case "PUT":
                Console.WriteLine($"put request received to {request.RawUrl}");
                Put(response, request);
                break;
        }
    }
    private void Get(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = ReadRequestData(request);

        // jail loop
        if (_police.IsInJail(parts))
        {
            message = "You're in jail! Your stats are: \n" + _user.ShowStats(parts);
            Print(response, message);
            return;
        }

        if (_police.DetectionRate(parts) > 99)
        {
            Print(response, _police.SendToJail(parts));
            return;
        }

        if (path.Contains("ipscanner.exe"))
        {
            message = _user.IPScanner(parts);
            Print(response, message);
        }

        if (path.Contains("statuscenter.exe"))
        {
            message = _user.ShowStats(parts);
            Print(response, message);
        }

        Print(response, "Invalid endpoint");
    }
    private void Post(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = ReadRequestData(request);

        if (_police.IsInJail(parts))
        {
            message = "You're in jail! Your stats are: \n" + _user.ShowStats(parts);
            Print(response, message);
            return;
        }

        if (_police.DetectionRate(parts) > 99)
        {
            Print(response, _police.SendToJail(parts));
            return;
        }


        // Register User: curl -X POST http://localhost:3000/newuser -d 'username,password,dummy_password,keyword'
        if (path.Contains("newuser"))
        {
            message = _user.Register(parts, this);
            Print(response, message);
        }

        Print(response, "Invalid endpoint");
    }
    private void Put(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = ReadRequestData(request);

        if (_police.IsInJail(parts))
        {
            message = "You're in jail! Your stats are: \n" + _user.ShowStats(parts);
            Print(response, message);
            return;
        }

        if (_police.DetectionRate(parts) > 99)
        {
            Print(response, _police.SendToJail(parts));
            return;
        }

        if (path.Contains("attack")) //Attack! curl -X PUT http://localhost:3000/attack/targetIP -d 'username,password'
        {
            message = _user.Attack(path, parts);
            Print(response, message);
        }

        if (path.Contains("hide-me.exe"))
        {
            message = _user.HideMe(parts, this);
            Print(response, message);
        }
        if (path.Contains("updatefirewall.exe"))
        {
            message = _user.Heal(parts);
            Print(response, message);
        }
        Print(response, "Invalid endpoint");
    }
    public IPAddress Generate()
    {
        Random random = new Random();
        byte[] ipBytes = new byte[4];
        random.NextBytes(ipBytes);
        return new IPAddress(ipBytes);
    }
    public void GeneratePoliceIP()
    {
        const string qCountUsers = @$"SELECT COUNT(*) FROM users u WHERE u.id > 0";
        const string qAddPoliceIp = "INSERT INTO ip(address,user_id) VALUES ($1, $2)";

        var cmdCountUsers = _db.CreateCommand(qCountUsers);
        var rowCount = cmdCountUsers.ExecuteScalar();
        int rowCountInt = Convert.ToInt32(rowCount);

        if (rowCountInt % 3 == 1)
        {
            IPAddress generatedPoliceIP = Generate();
            string policeIp = generatedPoliceIP.ToString();
            using var cmd = _db.CreateCommand(qAddPoliceIp);
            cmd.Parameters.AddWithValue(policeIp);
            cmd.Parameters.AddWithValue(0);
            cmd.ExecuteNonQuery();
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
    private (string path, string[] parts) ReadRequestData(HttpListenerRequest request)
    {
        var path = request.Url?.AbsolutePath ?? "404";
        string data;

        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            data = reader.ReadToEnd();
        }

        return (path, data.Split(","));
    }
}