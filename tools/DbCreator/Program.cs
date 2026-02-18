using Microsoft.Data.Sqlite;

var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..");
var dbPath = Path.Combine(rootPath, "controlpeso.db");
var schemaPath = Path.Combine(rootPath, "docs", "schema", "schema_v1.sql");

Console.WriteLine($"📁 Root: {rootPath}");
Console.WriteLine($"💾 DB: {dbPath}");
Console.WriteLine($"📄 Schema: {schemaPath}");

if (!File.Exists(schemaPath))
{
    Console.WriteLine($"❌ Schema file not found: {schemaPath}");
    return 1;
}

var connectionString = $"Data Source={dbPath}";
var sqlScript = File.ReadAllText(schemaPath);

using var connection = new SqliteConnection(connectionString);
connection.Open();

Console.WriteLine("\n🔨 Executing SQL script...\n");

// SQLite ejecuta múltiples statements en un solo comando
using var command = connection.CreateCommand();
command.CommandText = sqlScript;

try
{
    command.ExecuteNonQuery();
    Console.WriteLine("✅ Database created successfully!");
    Console.WriteLine($"📍 Location: {dbPath}");

    // Verificar tablas creadas
    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
    using var reader = command.ExecuteReader();

    Console.WriteLine("\n📋 Tables created:");
    while (reader.Read())
    {
        Console.WriteLine($"   - {reader.GetString(0)}");
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    return 1;
}
