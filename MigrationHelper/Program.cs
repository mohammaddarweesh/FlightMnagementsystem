using Npgsql;

var connectionString = "Host=localhost;Database=FlightBookingDB;Username=postgres;Password=6482297";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    Console.WriteLine("Connected to database successfully.");
    
    // Check if migrations table exists
    var checkTableSql = @"
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = '__EFMigrationsHistory'
        );";
    
    using var checkCommand = new NpgsqlCommand(checkTableSql, connection);
    var tableExists = (bool)await checkCommand.ExecuteScalarAsync()!;
    
    if (!tableExists)
    {
        Console.WriteLine("Migrations table does not exist. Creating it...");
        var createTableSql = @"
            CREATE TABLE ""__EFMigrationsHistory"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );";
        
        using var createCommand = new NpgsqlCommand(createTableSql, connection);
        await createCommand.ExecuteNonQueryAsync();
        Console.WriteLine("Migrations table created.");
    }
    
    // Mark existing migrations as applied
    var sql = @"
        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") 
        VALUES 
            ('20250830220346_AddIdentityEntities', '8.0.0'),
            ('20250831163958_AddAuditSystem', '8.0.0')
        ON CONFLICT (""MigrationId"") DO NOTHING;";
    
    using var command = new NpgsqlCommand(sql, connection);
    var rowsAffected = await command.ExecuteNonQueryAsync();
    
    Console.WriteLine($"Marked {rowsAffected} migrations as applied.");
    
    // List current migrations
    var listSql = @"SELECT ""MigrationId"" FROM ""__EFMigrationsHistory"" ORDER BY ""MigrationId"";";
    using var listCommand = new NpgsqlCommand(listSql, connection);
    using var reader = await listCommand.ExecuteReaderAsync();
    
    Console.WriteLine("\nCurrent applied migrations:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader.GetString(0)}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
