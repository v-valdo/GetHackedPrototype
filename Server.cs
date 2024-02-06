using Npgsql;
using System.Net;
using System.Text;

namespace GetHackedPrototype;

public class Server
{
    private readonly NpgsqlDataSource? _db;
    public int port = 3000;
    private HttpListener _listener = new();
    private Timer firewallUpdateTimer;

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

    private void StartPeriodicUpdate()
    {
        // Start the periodic update if not already started
        if (firewallUpdateTimer == null)
        {
            firewallUpdateTimer = new Timer(UpdateFirewallPeriodically, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            Console.WriteLine("Periodic firewall health update started.");
        }
    }

    public async void UpdateFirewallPeriodically(object state)
    {
        const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth + 1 WHERE firewallhealth < 100";

        try
        {

            await using var cmd = _db.CreateCommand(qUpdateFirewall);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            Console.WriteLine($"Error updating firewall health: {ex.Message}");
        }

        Console.WriteLine($"Firewall health has been updated periodically. Current time: {DateTime.Now}");
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
                        var cmd = _db.CreateCommand("insert into users (username, password) values ($1, $2) RETURNING id");

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

                        int userId = (int)await cmd.ExecuteScalarAsync();

                        IPAddress ip = new();
                        string userIp = ip.Generate();

                        var insertIpCmd = _db.CreateCommand("INSERT INTO IP (userid, address) VALUES ($1, $2)");
                        insertIpCmd.Parameters.AddWithValue(userId);
                        insertIpCmd.Parameters.AddWithValue(userIp);
                        await insertIpCmd.ExecuteNonQueryAsync();
                    }
                    // insert logic for post (curl -d "username=test&description=test" -X POST http://localhost:3000/data)
                }
            }

            else if (request.HttpMethod == "PUT" && path.Contains("attacker/attackee/"))
            {

                const string qUpdateDetection = "UPDATE users SET detection = detection + 10 WHERE id = $1";
                const string qReadDetection = "select detection from users where id = $1";

                const string qUpdatePoints = "UPDATE users SET points = points + 5 WHERE id = $1";
                const string qReadPoints = "select points from users where id = $1";

                string[] pathParts = path.Split("/");
                int attackerId = int.Parse(pathParts[pathParts.Length - 2]);

                const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth - 10 WHERE id = $1";
                const string qReadFirewall = "select firewallhealth from users where id = $1";

                int attackeeId = int.Parse(path.Split("/").Last());

                //Update Firewall
                var cmdUpdateFirewall = _db.CreateCommand(qUpdateFirewall);
                cmdUpdateFirewall.Parameters.AddWithValue(attackeeId);
                await cmdUpdateFirewall.ExecuteNonQueryAsync();

                //Read Firewall
                var cmdReadFirewall = _db.CreateCommand(qReadFirewall);
                cmdReadFirewall.Parameters.AddWithValue(attackeeId);
                var readerFirewall = await cmdReadFirewall.ExecuteReaderAsync();
                while (await readerFirewall.ReadAsync())
                {
                    int firewallHealth = readerFirewall.GetInt32(0);
                    responseString += $"Your attack was succesfull and your opponent's firewall is now at {firewallHealth}%. ";
                }

                //Update Detection
                var cmdUpdateDetection = _db.CreateCommand(qUpdateDetection);
                cmdUpdateDetection.Parameters.AddWithValue(attackerId);
                await cmdUpdateDetection.ExecuteNonQueryAsync();

                //Read Detection 

                var cmdReadDetection = _db.CreateCommand(qReadDetection);
                cmdReadDetection.Parameters.AddWithValue(attackerId);
                var readerDetection = await cmdReadDetection.ExecuteReaderAsync();
                while (await readerDetection.ReadAsync())
                {
                    int detectionValue = readerDetection.GetInt32(0);
                    responseString += $"Your detection went up to {detectionValue}% ";
                }
                //Update Points
                var cmdUpdatePoints = _db.CreateCommand(qUpdatePoints);
                cmdUpdatePoints.Parameters.AddWithValue(attackerId);
                await cmdUpdatePoints.ExecuteNonQueryAsync();

                //Read Poinst 

                var cmdReadPoints = _db.CreateCommand(qReadPoints);
                cmdReadPoints.Parameters.AddWithValue(attackerId);
                var readerPoints = await cmdReadPoints.ExecuteReaderAsync();
                while (await readerPoints.ReadAsync())
                {
                    int pointsValue = readerPoints.GetInt32(0);
                    responseString += $"and your points went up to {pointsValue}. ";
                }
                //request: $ curl -X PUT http://localhost:3000/attacker/attackee/x/y -d '[{"Id": x}, {"Id": y}]'
            }

            else if (request.HttpMethod == "PATCH" && path.Contains("heal/user"))
            {
                const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth + 1 WHERE id = $1";
                StartPeriodicUpdate();
                UpdateFirewallPeriodically(null);
                //const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth + 1 WHERE firewallhealth < 100";

                int userId = int.Parse(path.Split("/").Last());

                await using var cmd = _db.CreateCommand(qUpdateFirewall);
                cmd.Parameters.AddWithValue(userId);
                await cmd.ExecuteNonQueryAsync();

                //responseString = $"You updated your Anti-Virus in time. Firewall is now back to 100%";
            }

            //IPScanner
            else if (request.HttpMethod == "GET" && path.Contains("/user/") && path.Contains("/software/ipscanner.exe"))
            {
                const string qFindIP = "select address from ip";
                const string qDetection = "update users set detection = detection + 10 where id = $1 RETURNING detection";

                var cmdFindIP = _db.CreateCommand(qFindIP);
                var cmdRaiseDetection = _db.CreateCommand(qDetection);

                int userId = int.Parse(path.Split("/")[2]);

                cmdRaiseDetection.Parameters.AddWithValue(userId);

                var readIPs = await cmdFindIP.ExecuteReaderAsync();

                while (await readIPs.ReadAsync())
                {
                    responseString += "IP address found: " + readIPs.GetString(0) + "\n";
                }

                var updatedDetection = await cmdRaiseDetection.ExecuteScalarAsync();

                responseString += "\nYour risk of detection is now: " + updatedDetection;

                // curl -X GET localhost:3000/user/<id>/software/ip-scanner.exe

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
