using Npgsql;

var connectionString = "Host=localhost;Database=FlightBookingDB;Username=postgres;Password=6482297";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    var sql = @"
        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") 
        VALUES 
            ('20250830220346_AddIdentityEntities', '8.0.0'),
            ('20250831163958_AddAuditSystem', '8.0.0')
        ON CONFLICT (""MigrationId"") DO NOTHING;";
    
    using var command = new NpgsqlCommand(sql, connection);
    var rowsAffected = await command.ExecuteNonQueryAsync();
    
    Console.WriteLine($"Marked {rowsAffected} migrations as applied.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
