using Npgsql;
using Microsoft.Extensions.Configuration;

public static class TestDbConnection
{
    public static void TestConnection()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Console.WriteLine($"Testing connection with: {connectionString}");

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            Console.WriteLine("✅ Connexion PostgreSQL réussie !");

            // Test simple query
            using var command = new NpgsqlCommand("SELECT version();", connection);
            var result = command.ExecuteScalar();
            Console.WriteLine($"Version PostgreSQL: {result}");

            connection.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur de connexion : {ex.Message}");
        }
    }
}