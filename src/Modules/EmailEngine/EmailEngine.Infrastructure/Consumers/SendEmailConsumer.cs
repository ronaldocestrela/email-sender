using System;
using System.Threading.Tasks;
using MassTransit;
using EmailEngine.Domain.Contracts;
using EmailEngine.Application.Ports;
using EmailEngine.Application.UseCases;

namespace EmailEngine.Infrastructure.Consumers;

/// <summary>
/// Consumidor assíncrono para o comando de envio de e-mails.
/// Implementa a interface IConsumer do MassTransit.
/// </summary>
public class SendEmailConsumer : IConsumer<SendEmailCommand>
{
    private readonly ISendEmailUseCase _sendEmailUseCase;
    private readonly ITenantSetter _tenantSetter;

    public SendEmailConsumer(ISendEmailUseCase sendEmailUseCase, ITenantSetter tenantSetter)
    {
        _sendEmailUseCase = sendEmailUseCase;
        _tenantSetter = tenantSetter;
    }

    /// <summary>
    /// Consome e processa o comando de envio de e-mail assíncrono, aplicando o isolamento do tenant.
    /// </summary>
    public async Task Consume(ConsumeContext<SendEmailCommand> context)
    {
        // 1. Injeta o ID do Tenant da mensagem no contexto assíncrono local (AsyncLocal)
        _tenantSetter.SetTenantId(context.Message.TenantId);

        // 2. Executa o caso de uso de envio de e-mail
        var result = await _sendEmailUseCase.ExecuteAsync(context.Message, context.CancellationToken);

        if (result.IsFailure)
        {
            // Lançar exceção para forçar a retentativa automática do MassTransit/RabbitMQ
            throw new InvalidOperationException($"Falha no processamento do e-mail: {result.Error.Message}");
        }
    }
}
