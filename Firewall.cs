using Npgsql;
using System;
using System.Threading;

public class Firewall
{
    private readonly NpgsqlDataSource _db;
    private Timer firewallUpdateTimer;

    public Firewall(NpgsqlDataSource db)
    {
        _db = db;
    }

    public void StartUpdateTimer(int userId)
    {
        if (firewallUpdateTimer == null)
        {
            firewallUpdateTimer = new Timer(state => Update(userId), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            Console.WriteLine("Periodic firewall health update started.");
        }
    }

    private void Update(object state)
    {
        int userId = (int)state;

        const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth + 1 WHERE id = $1";

        try
        {
            using var cmd = _db.CreateCommand(qUpdateFirewall);
            cmd.Parameters.AddWithValue(userId);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating firewall health: {ex.Message}");
        }

        Console.WriteLine($"Firewall health is being updated for user {userId}. Current time: {DateTime.Now}");
    }
}
