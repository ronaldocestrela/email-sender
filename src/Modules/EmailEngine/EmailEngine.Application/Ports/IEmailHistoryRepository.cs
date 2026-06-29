using System.Threading;
using System.Threading.Tasks;
using EmailEngine.Domain.Aggregates;

namespace EmailEngine.Application.Ports;

/// <summary>
/// Port de saída para persistência e auditoria de históricos de disparos.
/// </summary>
public interface IEmailHistoryRepository
{
    /// <summary>
    /// Adiciona um novo registro histórico de e-mail.
    /// </summary>
    Task AddAsync(EmailHistory history, CancellationToken cancellationToken = default);
}
