using Npgsql;
using System.Net;
using System.Text;

namespace GetHackedPrototype;

public class Server
{
    private readonly NpgsqlDataSource? _db;
    public int port = 3000;
    private HttpListener _listener = new();

    public Server(NpgsqlDataSource db)
    {
        _db = db;
    }

    public void Start()
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();

        Console.WriteLine("Server listening on port: " + port);

        _listener.BeginGetContext(new AsyncCallback(Route), _listener);
    }

    public void Stop()
    {
        _listener.Stop();
    }

    private async void Route(IAsyncResult result)
    {
        if (result.AsyncState is HttpListener listener)
        {

            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            HttpListenerResponse response = context.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/plain";

            Console.WriteLine($"{request.HttpMethod} request received");

            string path = request.Url?.AbsolutePath ?? "/";
            string responseString = "";

            if (request.HasEntityBody && request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/user/register")
            {

                using (var body = request.InputStream)
                {
                    var encoder = request.ContentEncoding;

                    using (var reader = new StreamReader(body, encoder))
                    {
                        var cmd = _db.CreateCommand("insert into users (username, password) values ($1, $2)");

                        string postBody = reader.ReadToEnd();
                        Console.WriteLine(postBody);

                        string[] parts = postBody.Split("&");

                        foreach (var part in parts)
                        {
                            string[] userParts = part.Split("=");

                            string column = userParts[0];
                            string value = userParts[1];

                            //date
                            //value1
                            //description
                            //value2

                            if (column == "username")
                            {
                                cmd.Parameters.AddWithValue(value);
                            }
                            else if (column == "password")
                            {
                                cmd.Parameters.AddWithValue(value);
                            }
                        }
                        await cmd.ExecuteNonQueryAsync();
                    }
                    // insert logic for post (curl -d "username=test&description=test" -X POST http://localhost:3000/data)
                }
            }

            // view all users
            else if (request.HttpMethod == "GET" && path.Contains("users/all"))
            {
                const string qUsers = "select * from users";

                var cmd = _db.CreateCommand(qUsers);
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    responseString += reader.GetInt32(0) + ", "; // user id
                    responseString += reader.GetString(1) + ", "; // username
                    responseString += reader.GetString(2) + ", "; // password
                    responseString += reader.GetInt32(3) + ", "; // firewallhealth
                    responseString += reader.GetInt32(4) + ", "; // points
                }
            }

            // Query specific users
            else if (request.HttpMethod == "GET" && path.Contains("users/id"))
            {
                const string qTodo = "select username, password from users where id = $1";

                var cmd = _db?.CreateCommand(qTodo);

                int userId = int.Parse(path.Split("/").Last());

                cmd?.Parameters.AddWithValue(userId);

                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    responseString += reader.GetString(0) + ", ";
                    responseString += reader.GetString(1) + ", ";
                }
            }
            else if (request.HttpMethod == "GET" && path.Contains("attack/user"))
            {
                const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth - 10 WHERE id = $1";
                const string qReadFirewall = "select firewallhealth from users where id = $1";

                const string qUpdateDetection = "UPDATE users SET detection = detection + 10 WHERE id = $1";

                int userId = int.Parse(path.Split("/").Last());

                await using var cmd = _db.CreateCommand(qUpdateFirewall);
                cmd.Parameters.AddWithValue(userId);
                await cmd.ExecuteNonQueryAsync();


                var cmd2 = _db.CreateCommand(qReadFirewall);
                cmd2.Parameters.AddWithValue(userId);
                await cmd2.ExecuteNonQueryAsync();

                var cmd3 = _db.CreateCommand(qUpdateDetection);
                cmd.Parameters.AddWithValue(userId);
                await cmd3.ExecuteNonQueryAsync();

                var reader = await cmd2.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    responseString += reader.GetInt32(0);
                    responseString = $"You reduced your opponent's firewall by 10%";
                    responseString = $"Your detection went up to ...";

                }

            }

            else
            {
                responseString = "Nothing here...";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        // Loopback
        _listener.BeginGetContext(new AsyncCallback(Route), _listener);
    }
}
