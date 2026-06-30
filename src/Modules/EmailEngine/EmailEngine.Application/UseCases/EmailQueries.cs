using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailEngine.Domain.Common;
using EmailEngine.Application.Ports;

namespace EmailEngine.Application.UseCases;

/// <summary>
/// DTO contendo informações resumidas sobre o log de envio de e-mail.
/// </summary>
public record EmailHistoryResponse(
    Guid Id,
    string To,
    string Subject,
    string SenderDomain,
    DateTime SentAt,
    bool IsSuccess,
    string? ErrorMessage
);

/// <summary>
/// Interface para obter o histórico de e-mails do Tenant ativo.
/// </summary>
public interface IGetEmailHistoryUseCase
{
    Task<Result<List<EmailHistoryResponse>>> ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação do caso de uso de consulta do histórico de e-mails.
/// </summary>
public class GetEmailHistoryUseCase : IGetEmailHistoryUseCase
{
    private readonly IEmailHistoryRepository _emailHistoryRepository;

    public GetEmailHistoryUseCase(IEmailHistoryRepository emailHistoryRepository)
    {
        _emailHistoryRepository = emailHistoryRepository;
    }

    public async Task<Result<List<EmailHistoryResponse>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var history = await _emailHistoryRepository.GetAllAsync(cancellationToken);

        var response = history.Select(h => new EmailHistoryResponse(
            h.Id,
            h.To,
            h.Subject,
            h.SenderDomain,
            h.SentAt,
            h.IsSuccess,
            h.ErrorMessage
        )).ToList();

        return Result<List<EmailHistoryResponse>>.Success(response);
    }
}
