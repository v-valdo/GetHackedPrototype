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

    public void StartUpdate(int userId)
    {
        if (firewallUpdateTimer == null)
        {
            firewallUpdateTimer = new Timer(state => Update(userId), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            Console.WriteLine("Periodic firewall health update started.");
        }
    }

    private void Update(object state)
    {
        // Extract the userId from the state object
        int userId = (int)state;

        // You can use userId in your update logic
        const string qUpdateFirewall = "UPDATE users SET firewallhealth = firewallhealth + 1 WHERE id = $1";

        try
        {
            // Use userId in your query and execute the update synchronously
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
