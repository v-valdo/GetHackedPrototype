using Npgsql;
using Npgsql.Replication.PgOutput.Messages;
using System.Net;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
namespace GetHackedPrototype;

public class RequestHandler
{
    private readonly NpgsqlDataSource? _db;
    public int port = 3000;
    private HttpListener _listener = new();

    public RequestHandler(NpgsqlDataSource db)
    {
        _db = db;
    }

    public void Start()
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        Console.WriteLine($"Server listening on port {port}");
        _listener.BeginGetContext(new AsyncCallback(Route), _listener);
    }

    public void Stop()
    {
        _listener.Stop();
    }

    private async void Route(IAsyncResult result)
    {
        var context = _listener.EndGetContext(result);

        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath;

        switch (request.HttpMethod)
        {
            // routing request types to methods? mb better?
            case "GET":
                Console.WriteLine($"get request received - {request.RawUrl}");
                await Get(response, request);
                break;
            case "POST":
                Console.WriteLine($"post request received");
                await Post(response, request);
                break;
        }
    }

    private async Task Get(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var path = request.Url?.AbsolutePath ?? "404";

        // Remove this, just example
        if (path.Contains("users/all"))
        {
            string qUsers = "select username,password from users;";
            var reader = await _db.CreateCommand(qUsers).ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                message += $"Username: {reader.GetString(0)}, Password: {reader.GetString(1)}";
            }
        }

        // Finally prints response (message)
        Print(response, message);
    }

    private async Task Post(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var path = request.Url?.AbsolutePath ?? "404";

        StreamReader reader = new(request.InputStream, request.ContentEncoding);
        string data = reader.ReadToEnd();

        // register: curl -d "username,password,dummyPassword,keyword" POST http://localhost:3000/users/register
        if (path.Contains("users/register"))
        {
            string qRegister = "INSERT INTO users(username,password) VALUES ($1, $2) RETURNING id";
            string qAddDummy = "INSERT INTO dummy_password(user_id,dummy_pass,keyword) VALUES ($1, $2, $3)";
            string qAddIp = "INSERT INTO ip(address,user_id) VALUES ($1, $2)";

            //INSERT into user table
            await using var cmd = _db.CreateCommand(qRegister);
            string[] parts = data.Split(",");
            cmd.Parameters.AddWithValue(parts[0]); //username
            cmd.Parameters.AddWithValue(parts[1]); //password

            int userId = (int)await cmd.ExecuteScalarAsync();
            
            //INSERT into dummy_password table
            await using var cmd2 = _db.CreateCommand(qAddDummy);
            cmd2.Parameters.AddWithValue(userId); //user id
            cmd2.Parameters.AddWithValue(parts[2]); //dummy password
            cmd2.Parameters.AddWithValue(parts[3]); //keyword
            await cmd2.ExecuteNonQueryAsync();

            //INSERT into ip table
            IPAddress generatedIP = Generate();
            string userIp = generatedIP.ToString();
            await using var cmd3 = _db.CreateCommand(qAddIp);
            cmd3.Parameters.AddWithValue(userIp); //user ip
            cmd3.Parameters.AddWithValue(userId); //user id
            await cmd3.ExecuteNonQueryAsync();

            // Finally prints response, message
            Print(response, message);
        }
    }
    static IPAddress Generate()
    {
        Random random = new Random();
        byte[] ipBytes = new byte[4];
        random.NextBytes(ipBytes);
        return new IPAddress(ipBytes);
    }
    private void Print(HttpListenerResponse response, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.ContentType = "text/plain";
        response.StatusCode = (int)HttpStatusCode.OK;

        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
}
