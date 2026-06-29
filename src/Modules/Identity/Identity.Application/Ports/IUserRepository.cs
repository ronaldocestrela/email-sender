using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Domain.Aggregates;

namespace Identity.Application.Ports;

/// <summary>
/// Port de saída para persistência e consulta do Agregado User.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Recupera um usuário a partir do seu endereço de e-mail.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera um usuário a partir do seu identificador único.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo usuário no banco de dados.
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o estado de um usuário existente.
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
