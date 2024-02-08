using Npgsql;
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

        // register
        if (path.Contains("users/register"))
        {
            string qRegister = "insert into users(username,password) values ($1, $2) RETURNING id";
            await using var cmd = _db.CreateCommand(qRegister);

            string[] parts = data.Split(",");
            for (int i = 0; i < 2; i++)
            {
                cmd.Parameters.AddWithValue(parts[i]);
            }     
            /*string[] parts = data.Split("&");
            foreach (var part in parts)
            {
                string[] dateDescription = part.Split("=");

                string column = dateDescription[0];
                string value = dateDescription[1];

                //username
                //value1
                //password
                //value2
                //dummy_pass
                //value3
                //keyword
                //value4

                if (column == "username")
                {
                    cmd.Parameters.AddWithValue(value);
                }
                else if (column == "password")
                {
                    cmd.Parameters.AddWithValue(value);
                }
                else if (column == "dummy_pass")
                {
                    cmd2.Parameters.AddWithValue(value);
                }
                else if (column == "keyword")
                {
                    cmd2.Parameters.AddWithValue(value);
                }
            }*/

            await cmd.ExecuteNonQueryAsync();
            int userId = (int)await cmd.ExecuteScalarAsync();

            //IPAddress ip = new();
            //string userIp = ip.Generate();
            //var insertIpCmd = _db.CreateCommand("INSERT INTO ip (userid, address) VALUES ($1, $2)");


            var insertDummyCmd = _db.CreateCommand("INSERT INTO dummy_password(user_id,dummy_pass, keyword) VALUES ($1, $2, $3)");
            insertDummyCmd.Parameters.AddWithValue(userId);
            insertDummyCmd.Parameters.AddWithValue(parts[2]);
            insertDummyCmd.Parameters.AddWithValue(parts[3]);

            await insertDummyCmd.ExecuteNonQueryAsync(); 

            //await insertIpCmd.ExecuteNonQueryAsync();

            // Finally prints response (message
            Print(response, message);
        }
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
