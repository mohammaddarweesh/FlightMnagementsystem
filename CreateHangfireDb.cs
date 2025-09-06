using Npgsql;

try
{
    // Connection to postgres database to create the hangfire database
    var connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=6482297";
    
    using var connection = new NpgsqlConnection(connectionString);
    connection.Open();
    
    // Check if database exists
    var checkCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'flightbookinghangfire'", connection);
    var exists = checkCmd.ExecuteScalar();
    
    if (exists == null)
    {
        // Create the database
        var createCmd = new NpgsqlCommand("CREATE DATABASE flightbookinghangfire", connection);
        createCmd.ExecuteNonQuery();
        Console.WriteLine("Hangfire database 'flightbookinghangfire' created successfully.");
    }
    else
    {
        Console.WriteLine("Hangfire database 'flightbookinghangfire' already exists.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
