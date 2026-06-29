using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Aggregates;
using EmailEngine.Domain.Contracts;
using EmailEngine.Application.Ports;

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
/// Caso de uso que executa o envio de e-mails, resolvendo as configurações do remetente
/// por Tenant e auditando os disparos no banco de dados.
/// </summary>
public class SendEmailUseCase : ISendEmailUseCase
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailHistoryRepository _emailHistoryRepository;
    private readonly IEmailProviderSettingsRepository _providerSettingsRepository;
    private readonly IConfiguration _configuration;

    public SendEmailUseCase(
        IEmailSender emailSender,
        IEmailHistoryRepository emailHistoryRepository,
        IEmailProviderSettingsRepository providerSettingsRepository,
        IConfiguration configuration)
    {
        _emailSender = emailSender;
        _emailHistoryRepository = emailHistoryRepository;
        _providerSettingsRepository = providerSettingsRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Processa o comando de envio resolvendo o provedor e salvando o log de auditoria correspondente.
    /// </summary>
    public async Task<Result> ExecuteAsync(SendEmailCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            return Result.Failure(new Error("EmailEngine.NullCommand", "O comando de envio não pode ser nulo."));
        }

        // 1. Busca configurações customizadas do Tenant atual
        var customSettings = await _providerSettingsRepository.GetByTenantIdAsync(command.TenantId, cancellationToken);

        // 2. Resolve Remetente Padrão (Customizado do Tenant ou Default do Sistema)
        var senderAddress = customSettings?.SenderAddress 
            ?? _configuration["EmailSettings:SenderAddress"] 
            ?? "noreply@system.com";

        var senderName = customSettings?.SenderName 
            ?? _configuration["EmailSettings:SenderName"] 
            ?? "System Sender";

        // 3. Processa Substituição de Variáveis de Template (se houver)
        var body = command.Body ?? string.Empty;
        var subject = command.Subject ?? string.Empty;

        if (command.TemplateVariables != null)
        {
            foreach (var variable in command.TemplateVariables)
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                body = body.Replace(placeholder, variable.Value);
                subject = subject.Replace(placeholder, variable.Value);
            }
        }

        // 4. Executa Envio Físico via Adapter
        var sendResult = await _emailSender.SendAsync(
            command.To,
            subject,
            body,
            senderAddress,
            senderName,
            customSettings,
            cancellationToken);

        // 5. Registra o Histórico no Banco de Dados para Auditoria
        var historyResult = EmailHistory.Create(
            command.TenantId,
            command.To,
            subject,
            body,
            command.SenderDomain,
            sendResult.IsSuccess,
            sendResult.IsFailure ? sendResult.Error.Message : null);

        if (historyResult.IsSuccess)
        {
            await _emailHistoryRepository.AddAsync(historyResult.Value, cancellationToken);
        }

        return sendResult;
    }
}
