namespace GameServer;

using GetHackedPrototype;
using Npgsql;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
public class UserAction
{
    private readonly NpgsqlDataSource _db;
    public UserAction(NpgsqlDataSource db)
    {
        _db = db;
    }

    public string HideMe(string[] parts, RequestHandler handler)
    {
        if (!UserExists(parts))
        {
            return "User doesn't exist";
        }
        string message = "";
        string newIP = handler.Generate().ToString();
        try
        {
            using var connection = _db.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
            const string qCoinCheck = @"
            SELECT hackercoinz
            FROM users
            WHERE username = $1 AND password = $2";

            var cmdCheckCoins = connection.CreateCommand();
            cmdCheckCoins.Transaction = transaction;
            cmdCheckCoins.CommandText = qCoinCheck;
            cmdCheckCoins.Parameters.AddWithValue(parts[0]);
            cmdCheckCoins.Parameters.AddWithValue(parts[1]);

            var currentCoinsResult = cmdCheckCoins.ExecuteScalar();
            int currentCoins = Convert.ToInt32(currentCoinsResult);

            int requiredHackercoinz = 30;

            if (currentCoins < requiredHackercoinz)
            {
                transaction.Rollback();
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
                cmdUpdateUser.ExecuteNonQuery();

                const string getUserIdQuery = @"
                SELECT id
                FROM users
                WHERE username = $1 AND password = $2";

                var cmdUserId = connection.CreateCommand();
                cmdUserId.Transaction = transaction;
                cmdUserId.CommandText = getUserIdQuery;
                cmdUserId.Parameters.AddWithValue(parts[0]);
                cmdUserId.Parameters.AddWithValue(parts[1]);
                var userId = cmdUserId.ExecuteScalar();

                const string qUpdateIP = @"
                UPDATE ip
                SET address = $1
                WHERE user_id = $2";

                var cmdUpdateIp = connection.CreateCommand();
                cmdUpdateIp.Transaction = transaction;
                cmdUpdateIp.CommandText = qUpdateIP;
                cmdUpdateIp.Parameters.AddWithValue(newIP);
                cmdUpdateIp.Parameters.AddWithValue(userId ?? 0);
                cmdUpdateIp.ExecuteNonQuery();

                transaction.Commit();

                message += $"You paid 30 HackerCoinz and changed your IP to {newIP}. Your detection risk in now zero.";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"HIDE ME ERROR: {e.Message}");
            message = $"HIDE ME ERROR: {e.Message}";
        }
        return message;
    }
    public string IPScanner(string[] parts)
    {
        if (!UserExists(parts))
        {
            return "User doesn't exist";
        }

        string message = "";
        var qIPScanner = "SELECT address FROM ip ORDER BY RANDOM() LIMIT 3;";
        var qEditUserStats = @"
            update users 
            set hackercoinz = hackercoinz - 5, 
            detection = detection + 5 
            where username = $1 and password = $2
            ";

        var qCheckCoins = @"
        select hackercoinz 
        from users 
        where username = $1 
        and password = $2";

        string username = parts[0];
        string password = parts[1];

        try
        {
            var checkCoins = _db.CreateCommand(qCheckCoins);
            checkCoins.Parameters.AddWithValue(username);
            checkCoins.Parameters.AddWithValue(password);
            var reader = checkCoins.ExecuteReader();

            while (reader.Read())
            {
                if (reader.GetInt32(0) <= 5)
                {
                    message = "Not enough HackerCoinz";
                    return message;
                }
            }

            var IPList = _db.CreateCommand(qIPScanner).ExecuteReader();
            while (IPList.Read())
            {
                message += $"IP Address found: {IPList.GetString(0)}\n";
            }
            using var EditUser = _db.CreateCommand(qEditUserStats);
            EditUser.Parameters.AddWithValue(username);
            EditUser.Parameters.AddWithValue(password);
            EditUser.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Console.WriteLine("IPScanner ERROR: " + e.Message);
        }
        return message;
    }
    public string Register(string[] parts, RequestHandler handler)
    {
        string message = "";

        string qRegister = "INSERT INTO users(username,password) VALUES ($1, $2) RETURNING id";
        string qAddDummy = "INSERT INTO dummy_password(user_id,dummy_pass,keyword,encrypted_dummyP) VALUES ($1, $2, $3, $4)";
        string qAddIp = "INSERT INTO ip(address,user_id) VALUES ($1, $2)";

        try
        {

            using var cmd = _db.CreateCommand(qRegister);
            cmd.Parameters.AddWithValue(parts[0]); //username
            cmd.Parameters.AddWithValue(parts[1]); //password

            int userId = (int)cmd.ExecuteScalar();

            //INSERT into dummy_password table

            string keyword = handler.GenerateKeyword();
            string dummyPass = handler.GenerateDummyPass();
            string key = handler.GenerateKey(dummyPass, keyword);
            string encrypted_dummy = handler.EncryptDummy(keyword, key);

            using var cmd2 = _db.CreateCommand(qAddDummy);
            cmd2.Parameters.AddWithValue(userId); //user id
            cmd2.Parameters.AddWithValue(dummyPass); //dummy password
            cmd2.Parameters.AddWithValue(keyword); //keyword
            cmd2.Parameters.AddWithValue(encrypted_dummy); // encrypted Dummy

            cmd2.ExecuteNonQuery();

            //INSERT into ip table
            IPAddress generatedIP = handler.Generate();
            string userIp = generatedIP.ToString();
            using var cmd3 = _db.CreateCommand(qAddIp);
            cmd3.Parameters.AddWithValue(userIp); //user ip
            cmd3.Parameters.AddWithValue(userId); //user id
            cmd3.ExecuteNonQuery();

            handler.GeneratePoliceIP();

            message = $"User '{parts[0]}' registered successfully!";

        }

        catch (Exception ex)
        {
            Console.WriteLine($"Registration ERROR: {ex.Message}");
            message += ex.Message;
        }
        return message;
    }
    public string Heal(string[] parts)
    {
        if (!UserExists(parts))
        {
            return "User doesn't exist";
        }

        string message = "";
        string username = parts[0];
        string password = parts[1];
        try
        {
            //Get user id
            int id = GetUserId(parts);

            const string qUpdateFirewall = "UPDATE users SET firewallhealth = 100 WHERE id = $1";
            const string qUpdateCoins = "UPDATE users SET hackercoinz = hackercoinz - 10 WHERE id = $1";

            var cmdUpdateFirewall = _db.CreateCommand(qUpdateFirewall);
            var cmdUpdateCoins = _db.CreateCommand(qUpdateCoins);

            cmdUpdateFirewall.Parameters.AddWithValue(id);
            cmdUpdateCoins.Parameters.AddWithValue(id);

            cmdUpdateFirewall.ExecuteNonQuery();
            cmdUpdateCoins.ExecuteNonQuery();
            message = "User healed successfully.";

        }
        catch (Exception ex)
        {
            message = "HEAL ERROR: " + ex.Message;
        }
        return message;
    }
    public string FinalHack(string path, string[] parts, RequestHandler handler)
    {
        if (!UserExists(parts))
        {
            return "User does not exist";
        }

        string message = "";

        try
        {
            int targetId;
            string username = parts[0];
            string password = parts[1];
            string[] items = path.Split("/");
            string targetIp = items.Last();
            string dummyGuess = items[items.Length - 2];
            string dummyPassword;

            const string qSelectTargetId = "SELECT user_id FROM IP WHERE address = $1";
            const string qUpdateTarget = "UPDATE users SET firewallhealth = 100, hackercoinz = CASE WHEN (hackercoinz-50) >=0 THEN (hackercoinz-50) ELSE 0 END WHERE id = $1";
            const string qReadFirewall = "SELECT firewallhealth from users WHERE id = $1";
            const string qReadDummy = "SELECT dummy_pass from dummy_password WHERE id = $1";
            const string qUpdateHackerCoinz = "UPDATE users SET hackercoinz = hackercoinz + 50 WHERE id = $1";
            const string qReadHackerCoinz = "SELECT hackercoinz from users WHERE id = $1";
            const string qResetIp = "UPDATE ip SET address = $1 WHERE user_id = $2";
            const string qResetDummy = "UPDATE dummy_password SET dummy_pass = $1, keyword = $2, encrypted_dummyp = $3 WHERE user_id = $4";

            //Get user id
            int userId = GetUserId(parts);

            //Checks if target IP exists & gets target ID

            using (var cmdSelectTargetId = _db.CreateCommand(qSelectTargetId))
            {
                cmdSelectTargetId.Parameters.AddWithValue(targetIp);

                using (var readerSelectTargetId = cmdSelectTargetId.ExecuteReader())
                {
                    if (readerSelectTargetId.Read())
                    {
                        targetId = readerSelectTargetId.GetInt32(0);
                    }
                    else
                    {
                        message = "Target IP does not exist.";
                        return message;
                    }
                }
            }

            //Checks firewall = 0
            var cmdReadFirewall = _db.CreateCommand(qReadFirewall);
            cmdReadFirewall.Parameters.AddWithValue(targetId);
            var readerFirewall = cmdReadFirewall.ExecuteReader();
            while (readerFirewall.Read())
            {
                int firewallHealth = readerFirewall.GetInt32(0);
                if (firewallHealth != 0)
                {
                    message = "\nTarget's firewall is not 0. You need to bring it down to 0 for your final hack!";
                    return message;
                }
            }

            //Checks if dummy-password is correct
            var cmdReadDummy = _db.CreateCommand(qReadDummy);
            cmdReadDummy.Parameters.AddWithValue(targetId);
            var readerDummy = cmdReadDummy.ExecuteReader();
            while (readerDummy.Read())
            {
                dummyPassword = readerDummy.GetString(0);
                if (dummyPassword != dummyGuess)
                {
                    message = "\nIncorrect password!";
                    return message;
                }
            }

            DeleteNotepadEntry(userId, targetIp);

            //Update user: get 50 HC
            var cmdUpdateHackerCoinz = _db.CreateCommand(qUpdateHackerCoinz);
            cmdUpdateHackerCoinz.Parameters.AddWithValue(userId);
            cmdUpdateHackerCoinz.ExecuteNonQuery();

            //Update target in users: reset firewall & loses 50 HC
            var cmdUpdateTarget = _db.CreateCommand(qUpdateTarget);
            cmdUpdateTarget.Parameters.AddWithValue(targetId);
            cmdUpdateTarget.ExecuteNonQuery();

            //Reset target's IP
            IPAddress generatedIP = handler.Generate();
            string userIp = generatedIP.ToString();
            using var cmdresetIP = _db.CreateCommand(qResetIp);
            cmdresetIP.Parameters.AddWithValue(userIp); //new IP
            cmdresetIP.Parameters.AddWithValue(targetId); //user id
            cmdresetIP.ExecuteNonQuery();

            //Reset targets dummypassword and keyword qResetDummy
            string newKeyword = handler.GenerateKeyword();
            string newDummyPassword = handler.GenerateDummyPass();
            string key = handler.GenerateKey(newDummyPassword, newKeyword);
            string newEncryptedDummy = handler.EncryptDummy(newKeyword, key);

            using var cmdResetDummy = _db.CreateCommand(qResetDummy);
            cmdResetDummy.Parameters.AddWithValue(newDummyPassword);
            cmdResetDummy.Parameters.AddWithValue(newKeyword);
            cmdResetDummy.Parameters.AddWithValue(newEncryptedDummy);
            cmdResetDummy.Parameters.AddWithValue(targetId);
            cmdResetDummy.ExecuteNonQuery();

            //Read Hackercoinz 
            var cmdHackerCoinz = _db.CreateCommand(qReadHackerCoinz);
            cmdHackerCoinz.Parameters.AddWithValue(userId);
            var readerHackerCoinz = cmdHackerCoinz.ExecuteReader();
            while (readerHackerCoinz.Read())
            {
                int hackerCoinz = readerHackerCoinz.GetInt32(0);
                message += $"\nFinal hacking successful!\nYou have acquired 50 coinz and now have {hackerCoinz} HackerCoinz in your account.\nYour target's IP, firewall and login information have been reset.";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"ATTACK ERROR: {e.Message}");
        }
        return message;
    }


    public string Attack(string path, string[] parts)
    {
        if (!UserExists(parts))
        {
            return "User doesn't exist";
        }

        string message = "";

        const string qSelectTargetId = "SELECT user_id FROM IP WHERE address = $1";
        const string qUpdateFirewall = "UPDATE users SET firewallhealth = CASE WHEN (firewallhealth - 10) >= 0 THEN (firewallhealth - 10) ELSE 0 END WHERE id = $1";
        const string qReadFirewall = "SELECT firewallhealth from users WHERE id = $1";
        const string qUpdateHackerCoinz = "UPDATE users SET hackercoinz = hackercoinz + 5 WHERE id = $1";
        const string qReadHackerCoinz = "SELECT hackercoinz from users WHERE id = $1";
        const string qUpdateDetection = "UPDATE users SET detection = CASE WHEN (detection + 20) <= 100 THEN (detection + 20) ELSE 100 END WHERE id = $1";
        const string qReadDetection = "SELECT detection from users WHERE id = $1";
        const string qReadKeyword = "SELECT keyword FROM dummy_password WHERE user_id = $1";
        const string qCheckBF = "SELECT COUNT (*) FROM brute_force WHERE hacker_id = $1 AND target_ip = $2";
        const string qInsertBF = "INSERT INTO brute_force (hacker_id, target_ip, cracking) VALUES ($1, $2, $3);";
        const string qReadCurrentCracking = "SELECT cracking FROM brute_force WHERE hacker_id = $1 AND target_ip= $2";
        const string qUpdateBF = "UPDATE brute_force SET cracking = $1 WHERE hacker_id = $2 AND target_ip= $3;";
        const string qCheckNotepad = "SELECT COUNT (*) FROM notepad WHERE user_id = $1 AND ip_address = $2";
        const string qUpdateKeywordNP = "UPDATE notepad SET keyword = $1 WHERE user_id = $2 AND ip_address = $3";
        const string qReadNoAttacksNP = "SELECT number_of_attacks FROM notepad WHERE user_id = $1 AND ip_address = $2";
        const string qUpdateNoAttacksNP = "UPDATE notepad SET number_of_attacks = $1 WHERE user_id = $2 AND ip_address = $3";

        try
        {
            int targetId;
            string username = parts[0];
            string password = parts[1];
            string[] items = path.Split("/");
            string targetIp = items.Last();

            //Get user id
            int userId = GetUserId(parts);

            //Checks if target IP exists & gets target ID

            using (var cmdSelectTargetId = _db.CreateCommand(qSelectTargetId))
            {
                cmdSelectTargetId.Parameters.AddWithValue(targetIp);

                using (var readerSelectTargetId = cmdSelectTargetId.ExecuteReader())
                {
                    if (readerSelectTargetId.Read())
                    {
                        targetId = readerSelectTargetId.GetInt32(0);
                    }
                    else
                    {
                        message = "Target IP does not exist.";
                        return message;
                    }
                }
            }

            //Checks if target is police
            if (targetId == 0)
            {
                var police = new Police(_db);
                return police.AttackedPolice(GetUserId(parts));
            }

            var cmdCheckNotepad = _db.CreateCommand(qCheckNotepad);
            cmdCheckNotepad.Parameters.AddWithValue(userId);
            cmdCheckNotepad.Parameters.AddWithValue(targetIp);
            var count = (long?)cmdCheckNotepad.ExecuteScalar();

            if (count == 0)
            {
                NewNotepadEntry(userId, targetIp);
            }

            int numberOfAttacks = 0;

            var cmdReadNoAttacks = _db.CreateCommand(qReadNoAttacksNP);
            cmdReadNoAttacks.Parameters.AddWithValue(userId);
            cmdReadNoAttacks.Parameters.AddWithValue(targetIp);
            var attackReader = cmdReadNoAttacks.ExecuteReader();

            while (attackReader.Read())
            {
                numberOfAttacks = attackReader.GetInt32(0);
                numberOfAttacks++;
            }

            var cmdUpdateNoAttacks = _db.CreateCommand(qUpdateNoAttacksNP);
            cmdUpdateNoAttacks.Parameters.AddWithValue(numberOfAttacks);
            cmdUpdateNoAttacks.Parameters.AddWithValue(userId);
            cmdUpdateNoAttacks.Parameters.AddWithValue(targetIp);
            cmdUpdateNoAttacks.ExecuteNonQuery();

            //Check Detection level
            var cmdReadDetection = _db.CreateCommand(qReadDetection);
            cmdReadDetection.Parameters.AddWithValue(userId);
            var detectionReader = cmdReadDetection.ExecuteReader();

            //Update & read firewall
            using (var cmdUpdateFirewall = _db.CreateCommand(qUpdateFirewall))
            {
                cmdUpdateFirewall.Parameters.AddWithValue(targetId);
                cmdUpdateFirewall.ExecuteNonQuery();

                var cmdReadFirewall = _db.CreateCommand(qReadFirewall);
                cmdReadFirewall.Parameters.AddWithValue(targetId);
                var readerFirewall = cmdReadFirewall.ExecuteReader();
                while (readerFirewall.Read())
                {
                    int firewallHealth = readerFirewall.GetInt32(0);
                    if (firewallHealth == 0)
                    {
                        message += "\nTarget's firewall is at 0. You breached the firewall!";
                    }
                    else
                    {
                        message += $"\nYour attack was succesfull! \nYour opponent's firewall is now at {firewallHealth}%. ";
                    }
                }
            }

            //Update HackerCoinz
            var cmdUpdateHackerCoinz = _db.CreateCommand(qUpdateHackerCoinz);
            cmdUpdateHackerCoinz.Parameters.AddWithValue(userId);
            cmdUpdateHackerCoinz.ExecuteNonQuery();

            //Read Hackercoinz 
            var cmdHackerCoinz = _db.CreateCommand(qReadHackerCoinz);
            cmdHackerCoinz.Parameters.AddWithValue(userId);
            var readerHackerCoinz = cmdHackerCoinz.ExecuteReader();
            while (readerHackerCoinz.Read())
            {
                int hackerCoinz = readerHackerCoinz.GetInt32(0);
                message += $"\nYou have acquired 5 coinz and now have {hackerCoinz} hackercoinz in your account.";
            }

            //Update Detection
            var cmdUpdateDetection = _db.CreateCommand(qUpdateDetection);
            cmdUpdateDetection.Parameters.AddWithValue(userId);
            cmdUpdateDetection.ExecuteNonQuery();

            //Read Detection 
            int detection;
            cmdReadDetection = _db.CreateCommand(qReadDetection);
            cmdReadDetection.Parameters.AddWithValue(userId);
            var readerDetection = cmdReadDetection.ExecuteReader();

            while (readerDetection.Read())
            {
                detection = readerDetection.GetInt32(0);
                if (detection < 100)
                {
                    message += $"\nWatch out, your detection went up and is now at {detection}%. ";
                }
                else
                {
                    message += $"\n BE AWARE -- your risk of detection reached 100%!";
                }
            }

            //Get part of keyword
            //Check if attack already exists in brute_force table, if not insert

            var cmdCheckBF = _db.CreateCommand(qCheckBF);
            cmdCheckBF.Parameters.AddWithValue(userId);
            cmdCheckBF.Parameters.AddWithValue(targetIp);
            var rowCount = (long?)cmdCheckBF.ExecuteScalar();

            if (rowCount == 0)
            {
                var cmdReadKeyword = _db.CreateCommand(qReadKeyword);
                cmdReadKeyword.Parameters.AddWithValue(targetId);
                var readerKeyword = cmdReadKeyword.ExecuteReader();

                string firstLetter = "";

                while (readerKeyword.Read())
                {
                    string fullKeyword = readerKeyword.GetString(0);
                    firstLetter = fullKeyword.Substring(0, 1);
                    message += $"\nThe first letter of your target's keyword is '{firstLetter}'.";
                }

                // INSERT values INTO brute force table
                var cmdUpdateBF = _db.CreateCommand(qInsertBF);
                cmdUpdateBF.Parameters.AddWithValue(userId);
                cmdUpdateBF.Parameters.AddWithValue(targetIp);
                cmdUpdateBF.Parameters.AddWithValue(firstLetter);
                cmdUpdateBF.ExecuteNonQuery();

                var cmdUpdateNP = _db.CreateCommand(qUpdateKeywordNP);
                cmdUpdateNP.Parameters.AddWithValue(firstLetter);
                cmdUpdateNP.Parameters.AddWithValue(userId);
                cmdUpdateNP.Parameters.AddWithValue(targetIp);
                cmdUpdateNP.ExecuteNonQuery();
            }

            else
            {
                // Read the existing cracking value and add new letter
                var cmdReadCurrentCracking = _db.CreateCommand(qReadCurrentCracking);
                cmdReadCurrentCracking.Parameters.AddWithValue(userId);
                cmdReadCurrentCracking.Parameters.AddWithValue(targetIp);
                var currentCracking = cmdReadCurrentCracking.ExecuteScalar() as string;

                var cmdReadKeyword = _db.CreateCommand(qReadKeyword);
                cmdReadKeyword.Parameters.AddWithValue(targetId);
                var tKeyword = cmdReadKeyword.ExecuteScalar() as string;

                if (currentCracking.Length < tKeyword.Length)
                {
                    char[] Keyword = tKeyword.ToCharArray();

                    string newCracking = currentCracking + Keyword[currentCracking.Length];

                    // Update cracking
                    var cmdUpdateBF = _db.CreateCommand(qUpdateBF);
                    cmdUpdateBF.Parameters.AddWithValue(newCracking);
                    cmdUpdateBF.Parameters.AddWithValue(userId);
                    cmdUpdateBF.Parameters.AddWithValue(targetIp);
                    cmdUpdateBF.ExecuteNonQuery();

                    var cmdUpdateNP = _db.CreateCommand(qUpdateKeywordNP);
                    cmdUpdateNP.Parameters.AddWithValue(newCracking);
                    cmdUpdateNP.Parameters.AddWithValue(userId);
                    cmdUpdateNP.Parameters.AddWithValue(targetIp);
                    cmdUpdateNP.ExecuteNonQuery();

                    //Read New Cracking

                    {
                        message += $"\nYou added a new letter to the target's keyword:'{newCracking[newCracking.Length - 1]}'";
                    }
                }
                else
                {
                    message += $"\nYou got all the letters. You cracked the keyword!";
                }
            }
        }

        catch (Exception e)
        {
            Console.WriteLine($"ATTACK ERROR: {e.Message}");
        }
        return message;
    }
    public string ShowStats(string[] parts)
    {

        if (!UserExists(parts))
        {
            return "User does not exist";
        }

        string message = "";
        string userStats = @"
        SELECT u.username, u.hackercoinz, u.detection, u.firewallhealth, i.address
        FROM users u
        JOIN ip i ON u.id = i.user_id
        WHERE u.username = $1 AND u.password = $2;";

        try
        {
            using var cmd = _db.CreateCommand(userStats);
            cmd.Parameters.AddWithValue(parts[0]);
            cmd.Parameters.AddWithValue(parts[1]);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                do
                {
                    message += $"Username: {reader.GetString(0)}, Hackercoinz: {reader.GetInt32(1)}, Detection Rate: {reader.GetInt32(2)}, Firewall Health: {reader.GetInt32(3)}, IP Address: {reader.GetString(4)}\n";
                } while (reader.Read());
            }
            else
            {
                message = "No user found with the provided username and password.";
            }
            return message;
        }
        catch (Exception e)
        {
            Console.WriteLine("Show Stats ERROR: " + e.Message);
        }
        return message;
    }

    public string ShowNotepad(string[] parts)
    {
        if (!UserExists(parts))
        {
            return "User does not exist";
        }

        const string qReadNotepad = "SELECT n.ip_address, n.number_of_attacks, n.keyword, n.dummy_pass FROM notepad n WHERE n.user_id = $1";

        string message = "";
        int userId = GetUserId(parts);

        try
        {
            using var cmd2 = _db.CreateCommand(qReadNotepad);
            cmd2.Parameters.AddWithValue(userId);
            using var readNotepad = cmd2.ExecuteReader();

            while (readNotepad.Read())
            {
                message += $"\nIP address: {readNotepad.GetString(0)}\nNumber of attacks: {readNotepad.GetInt32(1)}\nKeyword: {readNotepad.GetString(2)}\nPassword: {readNotepad.GetString(3)}\n";
            }

            if (message == "")
            {
                message += "Nothing to show here yet, go hack someone!";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("SHOW NOTEPAD ERROR: " + e.Message);
        }
        return message;
    }

    public string AutoDecrypt(string path, string[] parts)
    {
        if (!UserExists(parts))
        {
            return "User does not exist";
        }

        int userId = GetUserId(parts);
        string message = "";

        try
        {
            string[] items = path.Split("/");
            string targetIp = items[2];
            string keyword = items.Last();
            int targetId;

            string qCheckIPKeyword = "SELECT COUNT (*) FROM brute_force WHERE target_ip = $1 AND cracking = $2";
            string qGetDummyPass = "SELECT dummy_pass FROM dummy_password WHERE user_id = $1";
            string qSelectTargetId = "SELECT user_id FROM IP WHERE address = $1";
            string qUpdateDummyPassNP = "UPDATE notepad SET dummy_pass = $1 WHERE user_id = $2 AND ip_address = $3";

            var cmdCheckIPKeyword = _db.CreateCommand(qCheckIPKeyword);
            cmdCheckIPKeyword.Parameters.AddWithValue(targetIp);
            cmdCheckIPKeyword.Parameters.AddWithValue(keyword);
            var rowCount = (long?)cmdCheckIPKeyword.ExecuteScalar();

            if (rowCount == 0)
            {
                message = "This IP address does not match the given keyword. No hacking for you";
            }

            if (rowCount == 1)
            {
                //Checks if target IP exists & gets target ID
                using (var cmdSelectTargetId = _db.CreateCommand(qSelectTargetId))
                {
                    cmdSelectTargetId.Parameters.AddWithValue(targetIp);

                    using (var readerSelectTargetId = cmdSelectTargetId.ExecuteReader())
                    {
                        if (readerSelectTargetId.Read())
                        {
                            targetId = readerSelectTargetId.GetInt32(0);
                        }
                        else
                        {
                            message = "Target IP does not exist.";
                            return message;
                        }
                    }
                }
                var cmdGetDummyPass = _db.CreateCommand(qGetDummyPass);
                cmdGetDummyPass.Parameters.AddWithValue(targetId);
                var dummypass = cmdGetDummyPass.ExecuteScalar() as string;

                var cmdUpdateDummyPassNP = _db.CreateCommand(qUpdateDummyPassNP);
                cmdUpdateDummyPassNP.Parameters.AddWithValue(dummypass);
                cmdUpdateDummyPassNP.Parameters.AddWithValue(userId);
                cmdUpdateDummyPassNP.Parameters.AddWithValue(targetIp);
                cmdUpdateDummyPassNP.ExecuteNonQuery();

                message = $"\nYour target's password is {dummypass}";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Auto Decrypt ERROR: " + e.Message);
        }
        return message;
    }
    public bool UserExists(string[] parts)
    {
        var qCheckUser = "SELECT COUNT(*) FROM users WHERE username = $1 AND password = $2;";
        try
        {

            var checkUserCmd = _db.CreateCommand(qCheckUser);
            checkUserCmd.Parameters.AddWithValue(parts[0]);
            checkUserCmd.Parameters.AddWithValue(parts[1]);
            var userCount = (long?)checkUserCmd.ExecuteScalar();

            if (userCount == 0)
            {
                return false;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("UserExists ERROR: " + e.Message);
        }
        return true;
    }
    public int GetUserId(string[] parts)
    {
        const string qCheckUserPass = "SELECT id FROM users WHERE username = $1 AND password = $2";
        int userId = 0;
        try
        {

            using (var cmdCheckPassword = _db.CreateCommand(qCheckUserPass))
            {
                cmdCheckPassword.Parameters.AddWithValue(parts[0]);
                cmdCheckPassword.Parameters.AddWithValue(parts[1]);

                using (var readerGetId = cmdCheckPassword.ExecuteReader())
                {
                    if (readerGetId.Read())
                    {
                        userId = readerGetId.GetInt32(0);
                    }
                }
            }
            return userId;
        }
        catch (Exception e)
        {

            Console.WriteLine("GetUserdID ERROR " + e.Message);
        }
        return userId;
    }

    public bool CheckKeyword(string parts)
    {
        foreach (var item in parts)
        {
            if (item is >= 'a' and <= 'z')
                continue;
            else
                return false;
        }
        return true;
    }

    public void NewNotepadEntry(int userId, string targetIp)
    {
        const string qAddNotepad = "INSERT INTO notepad(user_id, ip_address, number_of_attacks, keyword, dummy_pass) VALUES ($1, $2, $3, $4, $5)";

        using var cmd = _db.CreateCommand(qAddNotepad);
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(targetIp);
        cmd.Parameters.AddWithValue(0);
        cmd.Parameters.AddWithValue("");
        cmd.Parameters.AddWithValue("");
        cmd.ExecuteNonQuery();
    }

    public void DeleteNotepadEntry(int userId, string targetIp)
    {
        const string qDeleteNotepad = "DELETE FROM notepad WHERE user_id = $1 AND ip_address = $2";

        var cmd = _db.CreateCommand(qDeleteNotepad);
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(targetIp);
        cmd.ExecuteNonQuery();
    }
}