using System.Threading;
using System.Threading.Tasks;
using EmailEngine.Application.Ports;
using EmailEngine.Domain.Aggregates;

namespace EmailEngine.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório concreto para persistência de logs históricos de envio.
/// </summary>
public class EmailHistoryRepository : IEmailHistoryRepository
{
    private readonly EmailEngineDbContext _context;

    public EmailHistoryRepository(EmailEngineDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adiciona o registro histórico de e-mail ao banco de dados.
    /// </summary>
    public async Task AddAsync(EmailHistory history, CancellationToken cancellationToken = default)
    {
        await _context.EmailHistories.AddAsync(history, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
