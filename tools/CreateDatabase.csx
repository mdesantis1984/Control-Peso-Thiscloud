using Microsoft.Data.Sqlite;

var connectionString = "Data Source=controlpeso.db";
var sqlScript = File.ReadAllText("docs/schema/schema_v1.sql");

using var connection = new SqliteConnection(connectionString);
connection.Open();

// Split by GO or semicolon and execute each statement
var statements = sqlScript.Split(';', StringSplitOptions.RemoveEmptyEntries);

foreach (var statement in statements)
{
    var trimmed = statement.Trim();
    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
        continue;

    using var command = connection.CreateCommand();
    command.CommandText = trimmed;
    try
    {
        command.ExecuteNonQuery();
        Console.WriteLine($"✓ Executed: {trimmed.Substring(0, Math.Min(50, trimmed.Length))}...");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error: {ex.Message}");
        Console.WriteLine($"  Statement: {trimmed}");
    }
}

Console.WriteLine("\n✅ Database created successfully!");
