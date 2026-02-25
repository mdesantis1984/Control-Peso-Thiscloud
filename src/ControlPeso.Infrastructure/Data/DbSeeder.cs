using ControlPeso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Infrastructure.Data;

/// <summary>
/// Implementación de seeding de datos para desarrollo.
/// Crea usuarios demo y registros de peso con datos realistas.
/// </summary>
public sealed class DbSeeder : IDbSeeder
{
    private readonly ControlPesoDbContext _context;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(ControlPesoDbContext context, ILogger<DbSeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el seeding de datos.
    /// Si ya existen usuarios, no hace nada (idempotente).
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting database seed...");

        try
        {
            // Asegurar que la base de datos existe
            await _context.Database.EnsureCreatedAsync(ct);

            // Verificar si ya hay datos
            var existingUsersCount = await _context.Users.CountAsync(ct);
            if (existingUsersCount > 0)
            {
                _logger.LogInformation("Database already seeded with {Count} users. Skipping seed.", existingUsersCount);
                return;
            }

            _logger.LogInformation("Database is empty. Creating seed data...");

            // 1. Crear usuarios demo
            var users = CreateDemoUsers();
            await _context.Users.AddRangeAsync(users, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Created {Count} demo users", users.Count);

            // 2. Crear preferencias de usuario
            var preferences = CreateUserPreferences(users);
            await _context.UserPreferences.AddRangeAsync(preferences, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Created {Count} user preferences", preferences.Count);

            // 3. Crear registros de peso (últimos 30 días con tendencias realistas)
            var weightLogs = CreateWeightLogs(users);
            await _context.WeightLogs.AddRangeAsync(weightLogs, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Created {Count} weight logs", weightLogs.Count);

            _logger.LogInformation("Database seed completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seed");
            throw;
        }
    }

    /// <summary>
    /// Crea usuarios demo con diferentes roles y configuraciones.
    /// </summary>
    private static List<Users> CreateDemoUsers()
    {
        var now = DateTime.UtcNow.ToString("O"); // ISO 8601 format

        return
        [
            // Usuario 1: Administrador (Marco - el dueño del proyecto)
            new Users
            {
                Id = "550e8400-e29b-41d4-a716-446655440001",
                GoogleId = "google_demo_admin_001",
                Name = "Marco De Santis",
                Email = "marco.desantis@thiscloud.com",
                Role = 1, // Administrator
                AvatarUrl = "https://lh3.googleusercontent.com/a/default-user=s96-c",
                MemberSince = DateTime.UtcNow.AddMonths(-6).ToString("O"),
                Height = 178.0, // 178 cm
                UnitSystem = 0, // Metric
                DateOfBirth = new DateTime(1984, 5, 15).ToString("yyyy-MM-dd"),
                Language = "es",
                Status = 0, // Active
                GoalWeight = 75.0, // 75 kg
                StartingWeight = 82.5, // 82.5 kg (se establecerá con el primer log)
                CreatedAt = DateTime.UtcNow.AddMonths(-6).ToString("O"),
                UpdatedAt = now
            },

            // Usuario 2: Usuario regular masculino (progreso descendente)
            new Users
            {
                Id = "550e8400-e29b-41d4-a716-446655440002",
                GoogleId = "google_demo_user_002",
                Name = "Juan Pérez",
                Email = "juan.perez@example.com",
                Role = 0, // User
                AvatarUrl = null,
                MemberSince = DateTime.UtcNow.AddMonths(-3).ToString("O"),
                Height = 175.0, // 175 cm
                UnitSystem = 0, // Metric
                DateOfBirth = new DateTime(1990, 8, 20).ToString("yyyy-MM-dd"),
                Language = "es",
                Status = 0, // Active
                GoalWeight = 70.0, // 70 kg
                StartingWeight = 78.0, // 78 kg
                CreatedAt = DateTime.UtcNow.AddMonths(-3).ToString("O"),
                UpdatedAt = now
            },

            // Usuario 3: Usuario regular femenino (progreso ascendente - ganando peso saludable)
            new Users
            {
                Id = "550e8400-e29b-41d4-a716-446655440003",
                GoogleId = "google_demo_user_003",
                Name = "María García",
                Email = "maria.garcia@example.com",
                Role = 0, // User
                AvatarUrl = "https://lh3.googleusercontent.com/a/default-user=s96-c",
                MemberSince = DateTime.UtcNow.AddMonths(-2).ToString("O"),
                Height = 165.0, // 165 cm
                UnitSystem = 1, // Imperial (usa lb)
                DateOfBirth = new DateTime(1992, 3, 10).ToString("yyyy-MM-dd"),
                Language = "en",
                Status = 0, // Active
                GoalWeight = 58.0, // 58 kg (~128 lb)
                StartingWeight = 52.0, // 52 kg (~115 lb)
                CreatedAt = DateTime.UtcNow.AddMonths(-2).ToString("O"),
                UpdatedAt = now
            }
        ];
    }

    /// <summary>
    /// Crea preferencias de usuario para cada usuario demo.
    /// </summary>
    private static List<UserPreferences> CreateUserPreferences(List<Users> users)
    {
        var now = DateTime.UtcNow.ToString("O");

        return users.Select(user => new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            DarkMode = 1, // Modo oscuro habilitado por defecto
            NotificationsEnabled = 1, // Notificaciones habilitadas
            TimeZone = "America/Argentina/Buenos_Aires", // Zona horaria por defecto
            UpdatedAt = now
        }).ToList();
    }

    /// <summary>
    /// Crea registros de peso realistas para los últimos 30 días.
    /// Simula progreso realista con variaciones diarias normales.
    /// </summary>
    private static List<WeightLogs> CreateWeightLogs(List<Users> users)
    {
        var logs = new List<WeightLogs>();
        var random = new Random(42); // Seed fijo para reproducibilidad

        // Usuario 1 (Marco): Pérdida de peso gradual (82.5 → 78 kg en 30 días)
        logs.AddRange(GenerateWeightLogsForUser(
            user: users[0],
            startWeight: 82.5,
            endWeight: 78.0,
            days: 30,
            variance: 0.3, // ±300g de variación diaria
            random: random));

        // Usuario 2 (Juan): Pérdida de peso más rápida (78 → 72 kg en 30 días)
        logs.AddRange(GenerateWeightLogsForUser(
            user: users[1],
            startWeight: 78.0,
            endWeight: 72.0,
            days: 30,
            variance: 0.4, // ±400g de variación
            random: random));

        // Usuario 3 (María): Ganancia de peso saludable (52 → 55 kg en 30 días)
        logs.AddRange(GenerateWeightLogsForUser(
            user: users[2],
            startWeight: 52.0,
            endWeight: 55.0,
            days: 30,
            variance: 0.2, // ±200g de variación (más estable)
            random: random));

        return logs;
    }

    /// <summary>
    /// Genera registros de peso para un usuario con progreso realista.
    /// Simula variaciones diarias normales y días sin registro (más realista).
    /// </summary>
    private static List<WeightLogs> GenerateWeightLogsForUser(
        Users user,
        double startWeight,
        double endWeight,
        int days,
        double variance,
        Random random)
    {
        var logs = new List<WeightLogs>();
        var weightChange = endWeight - startWeight;
        var dailyChange = weightChange / days;

        var currentDate = DateTime.UtcNow.AddDays(-days);

        for (int i = 0; i <= days; i++)
        {
            // Simular días sin registro (20% de probabilidad de saltear)
            if (i > 0 && i < days && random.NextDouble() < 0.2)
            {
                currentDate = currentDate.AddDays(1);
                continue;
            }

            // Calcular peso con progreso lineal + variación aleatoria
            var baseWeight = startWeight + (dailyChange * i);
            var dailyVariance = (random.NextDouble() - 0.5) * 2 * variance; // ±variance
            var weight = Math.Round(baseWeight + dailyVariance, 1);

            // Asegurar que el peso esté dentro de rangos razonables
            weight = Math.Max(20.0, Math.Min(500.0, weight));

            // Calcular tendencia (comparar con registro anterior si existe)
            var trend = 2; // Neutral por defecto
            if (logs.Count > 0)
            {
                var previousWeight = logs[^1].Weight;
                if (weight > previousWeight + 0.1)
                    trend = 0; // Up
                else if (weight < previousWeight - 0.1)
                    trend = 1; // Down
                // else: Neutral (diferencia menor a 100g)
            }

            // Hora de pesaje realista (entre 6:00 AM y 9:00 AM)
            var hour = 6 + random.Next(4);
            var minute = random.Next(60);
            var timeOfDay = new TimeOnly(hour, minute);

            logs.Add(new WeightLogs
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Date = currentDate.ToString("yyyy-MM-dd"),
                Time = timeOfDay.ToString("HH:mm"),
                Weight = weight,
                DisplayUnit = user.UnitSystem, // Usar la preferencia del usuario
                Note = GenerateRandomNote(i, trend, random),
                Trend = trend,
                CreatedAt = currentDate.ToString("O")
            });

            currentDate = currentDate.AddDays(1);
        }

        return logs;
    }

    /// <summary>
    /// Genera notas aleatorias realistas para registros de peso.
    /// Solo agrega nota en ~30% de los registros (más realista).
    /// </summary>
    private static string? GenerateRandomNote(int dayIndex, int trend, Random random)
    {
        // Solo 30% de registros tienen nota
        if (random.NextDouble() > 0.3)
            return null;

        var notes = trend switch
        {
            0 => new[] // Up
            {
                "Comí mucho ayer 😅",
                "Fin de semana largo",
                "Retención de líquidos",
                "Día de descanso del gym"
            },
            1 => new[] // Down
            {
                "¡Buen progreso! 💪",
                "Mantuve la dieta",
                "Entrenamiento intenso ayer",
                "Me siento genial"
            },
            _ => new[] // Neutral
            {
                "Todo normal",
                "Día regular",
                "Pesaje matutino",
                null
            }
        };

        var selectedNote = notes[random.Next(notes.Length)];
        return selectedNote;
    }
}
