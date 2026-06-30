using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TenantManagement.Application.Ports;
using TenantManagement.Infrastructure.Persistence;

namespace TenantManagement.Infrastructure.Tests.Support;

/// <summary>
/// Factory para criar instâncias isoladas de TenantManagementDbContext em SQLite in-memory.
/// </summary>
public static class TenantDbContextFactory
{
    /// <summary>
    /// Abre uma conexão SQLite in-memory compartilhável entre múltiplos DbContexts do mesmo teste.
    /// </summary>
    public static SqliteConnection CreateSharedConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Cria um DbContext conectado à conexão SQLite fornecida.
    /// </summary>
    public static TenantManagementDbContext CreateContext(SqliteConnection connection, ITenantProvider tenantProvider, bool ensureCreated = true)
    {
        var options = new DbContextOptionsBuilder<TenantManagementDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TenantManagementDbContext(options, tenantProvider);

        if (ensureCreated)
        {
            context.Database.EnsureCreated();
        }

        return context;
    }
}
