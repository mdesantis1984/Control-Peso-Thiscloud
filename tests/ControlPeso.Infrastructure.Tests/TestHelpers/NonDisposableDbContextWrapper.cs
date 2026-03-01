using Microsoft.EntityFrameworkCore;

namespace ControlPeso.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Wrapper para DbContext que envuelve el contexto original en un proxy que ignora Dispose.
/// Esto permite reutilizar el mismo contexto In-Memory en múltiples operaciones
/// sin que `await using` lo dispose automáticamente.
/// </summary>
internal sealed class NonDisposableDbContextWrapper<TContext> where TContext : DbContext
{
    private readonly TContext _context;

    public NonDisposableDbContextWrapper(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>
    /// Crea un proxy que ignora Dispose, permitiendo reutilizar el contexto.
    /// </summary>
    public NonDisposableDbContextProxy CreateProxy()
    {
        return new NonDisposableDbContextProxy(_context);
    }

    /// <summary>
    /// Proxy que envuelve el DbContext y previene disposal.
    /// </summary>
    public sealed class NonDisposableDbContextProxy : IDbContextFactory<TContext>
    {
        private readonly TContext _context;

        public NonDisposableDbContextProxy(TContext context)
        {
            _context = context;
        }

        public TContext CreateDbContext() => _context;

        public ValueTask<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_context);
        }
    }
}
