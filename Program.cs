﻿using GetHackedPrototype;
using Npgsql;

const string dbUri = "Host=localhost;Port=5455;Username=postgres;Password=postgres;Database=gethacked;";

bool listen = false;

Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    listen = false;
    Console.WriteLine("Server closed");
};

List<string> IPList = File.ReadAllLines("IPList.txt").ToList();

foreach (var item in IPList)
{
    Console.WriteLine(item);
}

await using var db = NpgsqlDataSource.Create(dbUri);

Server server = new(db);

server.Start();

listen = true;

while (listen) { }

server.Stop();