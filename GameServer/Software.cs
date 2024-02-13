namespace GameServer;

using GetHackedPrototype;
using Npgsql;
using System.Net;
public class Software
{
    private readonly NpgsqlDataSource _db;

    public Software(NpgsqlDataSource db)
    {
        _db = db;
    }

    public async Task<string> HideMe(string path, string[] parts, HttpListenerResponse response)
    {
        RequestHandler handler = new RequestHandler(_db);
        string message = "";
        string newIP = handler.Generate().ToString();
        try
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction();
            const string checkHackercoinzQuery = @"
            SELECT hackercoinz
            FROM users
            WHERE username = $1 AND password = $2";

            var cmdCheckHackercoinz = connection.CreateCommand();
            cmdCheckHackercoinz.Transaction = transaction;
            cmdCheckHackercoinz.CommandText = checkHackercoinzQuery;
            cmdCheckHackercoinz.Parameters.AddWithValue(parts[0]);
            cmdCheckHackercoinz.Parameters.AddWithValue(parts[1]);
            var currentHackercoinzResult = await cmdCheckHackercoinz.ExecuteScalarAsync();
            int currentHackercoinz = Convert.ToInt32(currentHackercoinzResult);

            int requiredHackercoinz = 30; // Amount required for the operation
            if (currentHackercoinz < requiredHackercoinz)
            {
                // Rollback transaction and provide feedback to the user
                await transaction.RollbackAsync();
                message += "Insufficient hackercoinz to perform the operation.";
            }
            else
            {
                // Update users table
                const string updateUserQuery = @"
                UPDATE users
                SET hackercoinz = GREATEST(hackercoinz - 30, 0),
                    detection = 0
                WHERE username = $1 AND password = $2";

                var cmdUpdateUser = connection.CreateCommand();
                cmdUpdateUser.Transaction = transaction;
                cmdUpdateUser.CommandText = updateUserQuery;
                cmdUpdateUser.Parameters.AddWithValue(parts[0]);
                cmdUpdateUser.Parameters.AddWithValue(parts[1]);
                await cmdUpdateUser.ExecuteNonQueryAsync();

                // Get user_id from users table
                const string getUserIdQuery = @"
                SELECT id
                FROM users
                WHERE username = $1 AND password = $2";

                var cmdGetUserId = connection.CreateCommand();
                cmdGetUserId.Transaction = transaction;
                cmdGetUserId.CommandText = getUserIdQuery;
                cmdGetUserId.Parameters.AddWithValue(parts[0]);
                cmdGetUserId.Parameters.AddWithValue(parts[1]);
                var userId = await cmdGetUserId.ExecuteScalarAsync();

                // Update IP table
                const string updateIpQuery = @"
                UPDATE ip
                SET address = $1
                WHERE user_id = $2";

                var cmdUpdateIp = connection.CreateCommand();
                cmdUpdateIp.Transaction = transaction;
                cmdUpdateIp.CommandText = updateIpQuery;
                cmdUpdateIp.Parameters.AddWithValue(newIP);
                cmdUpdateIp.Parameters.AddWithValue(userId);
                await cmdUpdateIp.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                message += $"You changed your IP to {newIP}";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected error: {e.Message}");
            message = $"Unexpected error: {e.Message}";
        }
        return message;
    }
}