using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Ports;
using Identity.Domain.Aggregates;

namespace Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório concreto do aggregate User utilizando o Entity Framework Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Busca o usuário pelo e-mail ignorando filtros de Tenant (necessário na autenticação).
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        return await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    /// <summary>
    /// Busca o usuário pelo ID ignorando os filtros de Tenant (necessário no MFA Setup/Confirm).
    /// </summary>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Adiciona um novo usuário ao banco de dados.
    /// </summary>
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Atualiza as informações de um usuário existente.
    /// </summary>
    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
