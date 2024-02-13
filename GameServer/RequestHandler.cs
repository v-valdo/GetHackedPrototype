using Npgsql;
using System.Net;
using System.Text;
namespace GetHackedPrototype;

public class RequestHandler
{
    private readonly NpgsqlDataSource? _db;
    public int port = 3000;
    private HttpListener _listener = new();

    public RequestHandler(NpgsqlDataSource db) => _db = db;

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

        if (path.Contains("ipscanner.exe"))
        {
            try
            {
                var qIPScanner = "select address from ip";

                var qEditUserStats = @"
            update users 
            set hackercoinz = hackercoinz - 5, 
            detection = detection + 5 
            where username = $1 and password = $2
            ";
                string username = parts[0];
                string password = parts[1];
                var IPList = await _db.CreateCommand(qIPScanner).ExecuteReaderAsync();

                while (await IPList.ReadAsync())
                {
                    message += $"IP Address found: {IPList.GetString(0)}\n";
                }
                await using var EditUser = _db.CreateCommand(qEditUserStats);
                EditUser.Parameters.AddWithValue(username);
                EditUser.Parameters.AddWithValue(password);
                await EditUser.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Print(response, message);
            }
        }

        // Finally prints response (message)
        Print(response, message);
    }
    private async Task Post(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = await ReadRequestData(request);

        // register: curl -d "username,password,dummyPassword,keyword" POST http://localhost:3000/users/register
        if (path.Contains("users/register"))
        {
            try
            {
                string qRegister = "INSERT INTO users(username,password) VALUES ($1, $2) RETURNING id";
                string qAddDummy = "INSERT INTO dummy_password(user_id,dummy_pass,keyword) VALUES ($1, $2, $3)";
                string qAddIp = "INSERT INTO ip(address,user_id) VALUES ($1, $2)";

                //INSERT into user table
                await using var cmd = _db.CreateCommand(qRegister);
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

                message = $"User '{parts[0]}' registered successfully!";
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Print(response, $"Unexpected error: {ex.Message}");
            }
            finally
            {
                Print(response, message);
            }
        }
    }
    private async Task Put(HttpListenerResponse response, HttpListenerRequest request)
    {
        string message = "";
        var (path, parts) = await ReadRequestData(request);

        if (path.Contains("attack/")) //Attack! curl -X PUT http://localhost:3000/attack/ -d 'attackerId,targetIp'
        {
            try
            {
                const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth - 10 WHERE id = $1";
                const string qReadFirewall = "select firewallhealth from users where id = $1";

                const string qUpdateHackerCoinz = "UPDATE users SET hackercoinz = hackercoinz + 5 WHERE id = $1";
                const string qReadHackerCoinz = "select hackercoinz from users where id = $1";

                const string qUpdateDetection = "UPDATE users SET detection = CASE WHEN (detection + 20) <= 100 THEN (detection + 20) ELSE 100 END WHERE id = $1";
                const string qReadDetection = "select detection from users where id = $1";

                const string qSelectTargetId = "SELECT user_id FROM IP WHERE address = $1";

                int attackerId = 0;
                int targetId = 0;
                string targetIp = string.Empty;

                if (parts.Length >= 2)
                {
                    attackerId = int.Parse(parts[0].Trim());
                    targetIp = parts[1];
                }
                else
                {
                    message = "Error: Invalid data input.";
                }

                //Get targetId
                var cmdSelectTargetId = _db.CreateCommand(qSelectTargetId);
                cmdSelectTargetId.Parameters.AddWithValue(targetIp);

                using (var readerSelectTargetId = await cmdSelectTargetId.ExecuteReaderAsync())
                {
                    if (await readerSelectTargetId.ReadAsync())
                    {
                        targetId = readerSelectTargetId.GetInt32(0);
                    }
                }

                //Update Firewall
                var cmdUpdateFirewall = _db.CreateCommand(qUpdateFirewall);
                cmdUpdateFirewall.Parameters.AddWithValue(targetId);
                await cmdUpdateFirewall.ExecuteNonQueryAsync();

                //Read Firewall
                var cmdReadFirewall = _db.CreateCommand(qReadFirewall);
                cmdReadFirewall.Parameters.AddWithValue(targetId);
                var readerFirewall = await cmdReadFirewall.ExecuteReaderAsync();
                while (await readerFirewall.ReadAsync())
                {
                    int firewallHealth = readerFirewall.GetInt32(0);
                    message += $"Your attack was succesfull and your opponent's firewall is now at {firewallHealth}%. ";
                }

                //Update HackerCoinz
                var cmdUpdateHackerCoinz = _db.CreateCommand(qUpdateHackerCoinz);
                cmdUpdateHackerCoinz.Parameters.AddWithValue(attackerId);
                await cmdUpdateHackerCoinz.ExecuteNonQueryAsync();

                //Read Points 
                var cmdHackerCoinz = _db.CreateCommand(qReadHackerCoinz);
                cmdHackerCoinz.Parameters.AddWithValue(attackerId);
                var readerHackerCoinz = await cmdHackerCoinz.ExecuteReaderAsync();
                while (await readerHackerCoinz.ReadAsync())
                {
                    int hackerCoinz = readerHackerCoinz.GetInt32(0);
                    message += $"Your points went up to {hackerCoinz} ";
                }

                //Update Detection
                var cmdUpdateDetection = _db.CreateCommand(qUpdateDetection);
                cmdUpdateDetection.Parameters.AddWithValue(attackerId);
                await cmdUpdateDetection.ExecuteNonQueryAsync();

                //Read Detection 
                int detection;
                var cmdReadDetection = _db.CreateCommand(qReadDetection);
                cmdReadDetection.Parameters.AddWithValue(attackerId);
                var readerDetection = await cmdReadDetection.ExecuteReaderAsync();

                while (await readerDetection.ReadAsync())
                {
                    detection = readerDetection.GetInt32(0);
                    if (detection < 100)
                    {
                        message += $"and your detection went up to {detection}%. ";
                    }
                    else
                    {
                        message = $"Police raid - your detection level reached 100%!";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Print(response, $"Unexpected error: {ex.Message}");
            }
            finally
            {
                Print(response, message);
            }
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

