﻿namespace GameServer;

using GetHackedPrototype;
using Npgsql;
using System.Net;
public class UserAction
{
    private readonly NpgsqlDataSource _db;
    public UserAction(NpgsqlDataSource db)
    {
        _db = db;
    }

    public async Task<string> HideMe(string path, string[] parts, HttpListenerResponse response, RequestHandler handler)
    {
        string message = "";
        string newIP = handler.Generate().ToString();
        try
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction();
            const string qCoinCheck = @"
            SELECT hackercoinz
            FROM users
            WHERE username = $1 AND password = $2";

            var cmdCheckCoins = connection.CreateCommand();
            cmdCheckCoins.Transaction = transaction;
            cmdCheckCoins.CommandText = qCoinCheck;
            cmdCheckCoins.Parameters.AddWithValue(parts[0]);
            cmdCheckCoins.Parameters.AddWithValue(parts[1]);

            var currentCoinsResult = await cmdCheckCoins.ExecuteScalarAsync();
            int currentCoins = Convert.ToInt32(currentCoinsResult);

            int requiredHackercoinz = 30;

            if (currentCoins < requiredHackercoinz)
            {
                await transaction.RollbackAsync();
                message += "Not enough Hackercoinz";
            }
            else
            {
                const string qUserUpdate = @"
                UPDATE users
                SET hackercoinz = GREATEST(hackercoinz - 30, 0),
                    detection = 0
                WHERE username = $1 AND password = $2";

                var cmdUpdateUser = connection.CreateCommand();
                cmdUpdateUser.Transaction = transaction;
                cmdUpdateUser.CommandText = qUserUpdate;
                cmdUpdateUser.Parameters.AddWithValue(parts[0]);
                cmdUpdateUser.Parameters.AddWithValue(parts[1]);
                await cmdUpdateUser.ExecuteNonQueryAsync();

                const string getUserIdQuery = @"
                SELECT id
                FROM users
                WHERE username = $1 AND password = $2";

                var cmdUserId = connection.CreateCommand();
                cmdUserId.Transaction = transaction;
                cmdUserId.CommandText = getUserIdQuery;
                cmdUserId.Parameters.AddWithValue(parts[0]);
                cmdUserId.Parameters.AddWithValue(parts[1]);
                var userId = await cmdUserId.ExecuteScalarAsync();

                const string qUpdateIP = @"
                UPDATE ip
                SET address = $1
                WHERE user_id = $2";

                var cmdUpdateIp = connection.CreateCommand();
                cmdUpdateIp.Transaction = transaction;
                cmdUpdateIp.CommandText = qUpdateIP;
                cmdUpdateIp.Parameters.AddWithValue(newIP);
                cmdUpdateIp.Parameters.AddWithValue(userId ?? 0);
                await cmdUpdateIp.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                message += $"You paid 30 HackerCoinz and changed your IP to {newIP}. Your detection risk in now zero.";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error: {e.Message}");
            message = $"Unexpected error: {e.Message}";
        }
        return message;
    }
    public async Task<string> IPScanner(string path, string[] parts, HttpListenerResponse response)
    {
        string message = "";
        var qIPScanner = "select address from ip";
        var qEditUserStats = @"
            update users 
            set hackercoinz = hackercoinz - 5, 
            detection = detection + 5 
            where username = $1 and password = $2
            ";
        try
        {
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
        return message;
    }
    public async Task<string> Register(string path, string[] parts, HttpListenerResponse response, RequestHandler handler)
    {
        string message = "";

        string qRegister = "INSERT INTO users(username,password) VALUES ($1, $2) RETURNING id";
        string qAddDummy = "INSERT INTO dummy_password(user_id,dummy_pass,keyword) VALUES ($1, $2, $3)";
        string qAddIp = "INSERT INTO ip(address,user_id) VALUES ($1, $2)";
        try
        {
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
            IPAddress generatedIP = handler.Generate();
            string userIp = generatedIP.ToString();
            await using var cmd3 = _db.CreateCommand(qAddIp);
            cmd3.Parameters.AddWithValue(userIp); //user ip
            cmd3.Parameters.AddWithValue(userId); //user id
            await cmd3.ExecuteNonQueryAsync();

            await handler.GeneratePoliceIP();

            message = $"User '{parts[0]}' registered successfully!";
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            message += ex.Message;
        }
        return message;
    }
    public async Task<string> Attack(string path, string[] parts, HttpListenerResponse response)
    {
        string message = "";

        const string qCheckPassword = "SELECT id from users WHERE username = $1 and password=$2";
        const string qUpdateFirewall = "UPDATE users SET firewallhealth = CASE WHEN (firewallhealth - 10) >= 0 THEN (firewallhealth - 10) ELSE 0 END WHERE id = $1";
        const string qReadFirewall = "SELECT firewallhealth from users WHERE id = $1";
        const string qUpdateHackerCoinz = "UPDATE users SET hackercoinz = hackercoinz + 5 WHERE username = $1 and password = $2";
        const string qReadHackerCoinz = "SELECT hackercoinz from users WHERE username = $1 and password = $2";
        const string qUpdateDetection = "UPDATE users SET detection = CASE WHEN (detection + 20) <= 100 THEN (detection + 20) ELSE 100 END WHERE username = $1 and password = $2";
        const string qReadDetection = "SELECT detection from users WHERE username = $1 and password = $2";
        const string qSelectTargetId = "SELECT user_id FROM IP WHERE address = $1";
        const string qReadKeyword = "SELECT keyword FROM dummy_password WHERE user_id = $1";
        const string qCheckBF = "SELECT COUNT (*) FROM brute_force WHERE hacker_id = $1 AND target_id = $2";
        const string qInsertBF = "INSERT INTO brute_force (hacker_id, target_id, cracking) VALUES ($1, $2, $3);";
        const string qReadCurrentCracking = "SELECT cracking FROM brute_force WHERE hacker_id = $1 AND target_id= $2";
        const string qUpdateBF = "UPDATE brute_force SET cracking = $1 WHERE hacker_id = $2 AND target_id= $3;";

        try
        {
            int targetId = 0;
            int userId = 0;
            string targetIp = string.Empty;
            string username = parts[0];
            string password = parts[1];
            string[] items = path.Split("/");

            if (path.Contains("/"))
            {
                targetIp = items.Last();
            }
            else
            {
                message = "Error: Invalid path, please include a target IP.";
            }

            //Get target id
            using (var cmdSelectTargetId = _db.CreateCommand(qSelectTargetId))
            {
                cmdSelectTargetId.Parameters.AddWithValue(targetIp);

                using (var readerSelectTargetId = await cmdSelectTargetId.ExecuteReaderAsync())
                {
                    if (await readerSelectTargetId.ReadAsync())
                        targetId = readerSelectTargetId.GetInt32(0);
                }
            }

            //Get user id
            using (var cmdSelectUserId = _db.CreateCommand(qCheckPassword))
            {
                cmdSelectUserId.Parameters.AddWithValue(username);
                cmdSelectUserId.Parameters.AddWithValue(password);

                using (var readerSelectUserId = await cmdSelectUserId.ExecuteReaderAsync())
                {
                    if (await readerSelectUserId.ReadAsync())
                        userId = readerSelectUserId.GetInt32(0);
                }
            }

            //Check user/pw
            using (var cmdCheckPassword = _db.CreateCommand(qCheckPassword))
            {
                cmdCheckPassword.Parameters.AddWithValue(username);
                cmdCheckPassword.Parameters.AddWithValue(password);
                using (var readerGetId = await cmdCheckPassword.ExecuteReaderAsync())
                {
                    if (await readerGetId.ReadAsync())
                    {
                        userId = readerGetId.GetInt32(0);
                        using (var cmdUpdateFirewall = _db.CreateCommand(qUpdateFirewall))
                        {
                            //Update firewall
                            cmdUpdateFirewall.Parameters.AddWithValue(targetId);
                            await cmdUpdateFirewall.ExecuteNonQueryAsync();

                            //Read firewall
                            var cmdReadFirewall = _db.CreateCommand(qReadFirewall);
                            cmdReadFirewall.Parameters.AddWithValue(targetId);
                            var readerFirewall = await cmdReadFirewall.ExecuteReaderAsync();
                            while (await readerFirewall.ReadAsync())
                            {
                                int firewallHealth = readerFirewall.GetInt32(0);
                                message += $"\n Your attack was succesfull! \n Your opponent's firewall is now at {firewallHealth}. ";
                            }
                        }
                    }
                    else
                    {
                        // User not identified
                        message = $"\nUser identification not successful.";
                    }
                }

                //Update HackerCoinz
                var cmdUpdateHackerCoinz = _db.CreateCommand(qUpdateHackerCoinz);
                cmdUpdateHackerCoinz.Parameters.AddWithValue(username);
                cmdUpdateHackerCoinz.Parameters.AddWithValue(password);
                await cmdUpdateHackerCoinz.ExecuteNonQueryAsync();

                //Read Hackercoinz 
                var cmdHackerCoinz = _db.CreateCommand(qReadHackerCoinz);
                cmdHackerCoinz.Parameters.AddWithValue(username);
                cmdHackerCoinz.Parameters.AddWithValue(password);
                var readerHackerCoinz = await cmdHackerCoinz.ExecuteReaderAsync();
                while (await readerHackerCoinz.ReadAsync())
                {
                    int hackerCoinz = readerHackerCoinz.GetInt32(0);
                    message += $"\nYour have {hackerCoinz} hackercoinz ";
                }

                //Update Detection
                var cmdUpdateDetection = _db.CreateCommand(qUpdateDetection);
                cmdUpdateDetection.Parameters.AddWithValue(username);
                cmdUpdateDetection.Parameters.AddWithValue(password);
                await cmdUpdateDetection.ExecuteNonQueryAsync();

                //Read Detection 
                int detection;
                var cmdReadDetection = _db.CreateCommand(qReadDetection);
                cmdReadDetection.Parameters.AddWithValue(username);
                cmdReadDetection.Parameters.AddWithValue(password);
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

                //Get part of keyword

                //Check if attack already exists in brute_force table, if not insert

                var cmdCheckBF = _db.CreateCommand(qCheckBF);
                cmdCheckBF.Parameters.AddWithValue(userId);
                cmdCheckBF.Parameters.AddWithValue(targetId);
                var rowCount = await cmdCheckBF.ExecuteScalarAsync();

                int rowCountInt = Convert.ToInt32(rowCount);

                if (rowCountInt == 0)
                {
                    var cmdReadKeyword = _db.CreateCommand(qReadKeyword);
                    cmdReadKeyword.Parameters.AddWithValue(targetId);
                    var readerKeyword = await cmdReadKeyword.ExecuteReaderAsync();

                    string firstLetter = "";

                    while (await readerKeyword.ReadAsync())
                    {
                        string fullKeyword = readerKeyword.GetString(0);
                        firstLetter = fullKeyword.Substring(0, 1);
                        message += $"\n The first letter of your target's keyword is '{firstLetter}'";
                    }

                    // INSERT values INTO brute force table

                    var cmdUpdateBF = _db.CreateCommand(qInsertBF);
                    cmdUpdateBF.Parameters.AddWithValue(userId);
                    cmdUpdateBF.Parameters.AddWithValue(targetId);
                    cmdUpdateBF.Parameters.AddWithValue(firstLetter);
                    await cmdUpdateBF.ExecuteNonQueryAsync();
                }

                else

                {
                    // Read the existing cracking value and add new letter
                    var cmdReadCurrentCracking = _db.CreateCommand(qReadCurrentCracking);
                    cmdReadCurrentCracking.Parameters.AddWithValue(username);
                    cmdReadCurrentCracking.Parameters.AddWithValue(targetId);
                    var currentCracking = await cmdReadCurrentCracking.ExecuteScalarAsync() as string;

                    var cmdReadKeyword = _db.CreateCommand(qReadKeyword);
                    cmdReadKeyword.Parameters.AddWithValue(targetId);
                    var tKeyword = await cmdReadKeyword.ExecuteScalarAsync() as string;

                    char[] Keyword = tKeyword.ToCharArray();

                    string newCracking = currentCracking + Keyword[currentCracking.Length];

                    // Update cracking
                    var cmdUpdateBF = _db.CreateCommand(qUpdateBF);
                    cmdUpdateBF.Parameters.AddWithValue(newCracking);
                    cmdUpdateBF.Parameters.AddWithValue(userId);
                    cmdUpdateBF.Parameters.AddWithValue(targetId);
                    await cmdUpdateBF.ExecuteNonQueryAsync();

                    //Read New Cracking
                    var cmdReadNewCracking = _db.CreateCommand(qReadCurrentCracking);
                    cmdReadNewCracking.Parameters.AddWithValue(userId);
                    cmdReadNewCracking.Parameters.AddWithValue(targetId);
                    var ReaderNewCracking = await cmdReadNewCracking.ExecuteReaderAsync();
                    while (await ReaderNewCracking.ReadAsync())
                    {
                        message += $"\n You added a new letter to the target's keyword:'{newCracking}'";
                    }

                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error: {e.Message}");
        }
        return message;
    }

    public async Task<string> ShowStats(string path, string[] parts, HttpListenerResponse response)
    {
        string message = "";
        string userStats = @"
        SELECT u.username, u.hackercoinz, u.detection, u.firewallhealth, i.address
        FROM users u
        JOIN ip i ON u.id = i.user_id
        WHERE u.username = $1 AND u.password = $2;";

        await using var cmd = _db.CreateCommand(userStats);
        cmd.Parameters.AddWithValue(parts[0]);
        cmd.Parameters.AddWithValue(parts[1]);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            do
            {
                message += $"Username: {reader.GetString(0)}, Hackercoinz: {reader.GetInt32(1)}, Detection Rate: {reader.GetInt32(2)}, Firewall Health: {reader.GetInt32(3)}, IP Address: {reader.GetString(4)}\n";
            } while (await reader.ReadAsync());
        }
        else
        {
            message = "No user found with the provided username and password.";
        }

        return message;
    }
}