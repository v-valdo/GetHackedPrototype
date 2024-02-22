using Npgsql;

namespace GameServer;

public class Police
{
    private readonly NpgsqlDataSource _db;
    private UserAction _user;
    public Police(NpgsqlDataSource db)
    {
        _db = db;
        _user = new UserAction(_db);
    }
    public int DetectionRate(string[] parts)
    {
        int detection = 0;
        const string qGetDetection = "select detection from users where id = $1";
        using var cmd = _db.CreateCommand(qGetDetection);
        cmd.Parameters.AddWithValue(_user.GetUserId(parts));
        var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            detection += reader.GetInt32(0);
        }
        return detection;
    }

    public string SendToJail(string[] parts)
    {
        if (!_user.UserExists(parts))
        {
            return "User doesn't exist";
        }

        const string qInsertUserJail = @"
         insert into users_jail (user_id, jailtime) 
         values ($1, $2);";

        const string qResetStats = @"
        update users
        set detection = 0, 
        hackercoinz = hackercoinz * 0.5
        where id = $1";

        try
        {
            int userId = _user.GetUserId(parts);
            DateTime jailTime = DateTime.UtcNow.AddMinutes(1); // change jail time here!
            using var insertCmd = _db.CreateCommand(qInsertUserJail);
            insertCmd.Parameters.AddWithValue(userId);
            insertCmd.Parameters.AddWithValue(jailTime);
            insertCmd.ExecuteNonQuery();

            using var resetCmd = _db.CreateCommand(qResetStats);
            resetCmd.Parameters.AddWithValue(userId);
            resetCmd.ExecuteNonQuery();
            Console.WriteLine($"User {userId}:s detection reset to 0");

            string jailMsg = "You have been detected by the authorities! \n You have been imprisoned for 1 minute and can no longer use your computer. \n You lost half of your HackerCoinz";
            return jailMsg;
        }
        catch (Exception e)
        {
            Console.WriteLine("SEND TO JAIL ERROR: " + e.Message);
        }
        return "Error";
    }
    public bool IsInJail(string[] parts)
    {
        const string qCheckJailtime = @"
        select jailtime 
        from users_jail 
        where user_id = $1;";
        try
        {
            int userId = Convert.ToInt32(_user.GetUserId(parts));
            using var checkCmd = _db.CreateCommand(qCheckJailtime);
            checkCmd.Parameters.AddWithValue(userId);
            using var reader = checkCmd.ExecuteReader();

            if (reader.Read())
            {
                DateTime jailtime = reader.GetDateTime(0);

                DateTime now = DateTime.UtcNow;
                if (now < jailtime)
                {
                    return true;
                }
                else
                {
                    ReleaseFromJail(userId);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("IS IN JAIL ERROR: " + e.Message);
        }
        return false;
    }
    private void ReleaseFromJail(int userId)
    {
        const string qDeleteUserJail = @"
        delete from users_jail 
        where user_id = $1;";

        using var deleteCmd = _db.CreateCommand(qDeleteUserJail);
        deleteCmd.Parameters.AddWithValue(userId);
        deleteCmd.ExecuteNonQuery();
        Console.WriteLine($"User {userId} released from jail");
    }
    public string AttackedPolice(int userId)
    {
        const string qDetected = "update users set detection = detection + 50 where id = $1";
        using var cmd = _db.CreateCommand(qDetected);
        cmd.Parameters.AddWithValue(userId);
        cmd.ExecuteNonQuery();
        return "You tried to hack the police! Your detection risk increased by 50%";
    }
}
