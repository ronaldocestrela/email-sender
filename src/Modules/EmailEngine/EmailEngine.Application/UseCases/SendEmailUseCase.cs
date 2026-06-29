using System;
using System.Threading;
using System.Threading.Tasks;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Contracts;

namespace EmailEngine.Application.UseCases;

/// <summary>
/// Interface do caso de uso de envio de e-mail assíncrono.
/// </summary>
public interface ISendEmailUseCase
{
    /// <summary>
    /// Executa as validações do comando de e-mail e inicia a engine física de envio.
    /// </summary>
    Task<Result> ExecuteAsync(SendEmailCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação inicial do caso de uso de envio de e-mail (Esqueleto).
/// </summary>
public class SendEmailUseCase : ISendEmailUseCase
{
    /// <summary>
    /// Executa o caso de uso de e-mail. Inicialmente simula o sucesso e loga informações.
    /// </summary>
    public Task<Result> ExecuteAsync(SendEmailCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            return Task.FromResult(Result.Failure(new Error("EmailEngine.NullCommand", "O comando de envio não pode ser nulo.")));
        }

        if (string.IsNullOrWhiteSpace(command.To))
        {
            return Task.FromResult(Result.Failure(new Error("EmailEngine.InvalidRecipient", "O destinatário do e-mail é inválido.")));
        }

        // Simula processamento com log informativo
        Console.WriteLine($"[EmailEngine] Enviando e-mail para: {command.To}, Assunto: {command.Subject}, TenantId: {command.TenantId}");

        return Task.FromResult(Result.Success());
    }
}
