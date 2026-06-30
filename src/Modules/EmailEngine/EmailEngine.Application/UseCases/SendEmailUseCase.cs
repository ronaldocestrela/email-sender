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
    private readonly IEmailTenantDomainVerifier _domainVerifier;

    public SendEmailUseCase(
        IEmailSender emailSender,
        IEmailHistoryRepository emailHistoryRepository,
        IEmailProviderSettingsRepository providerSettingsRepository,
        IConfiguration configuration,
        IEmailTenantDomainVerifier domainVerifier)
    {
        _emailSender = emailSender;
        _emailHistoryRepository = emailHistoryRepository;
        _providerSettingsRepository = providerSettingsRepository;
        _configuration = configuration;
        _domainVerifier = domainVerifier;
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

        var subject = command.Subject ?? string.Empty;
        var body = command.Body ?? string.Empty;

        // 1. Valida se o domínio de envio está cadastrado e verificado para o Tenant
        var isDomainVerified = await _domainVerifier.IsDomainVerifiedAsync(command.TenantId, command.SenderDomain, cancellationToken);
        if (!isDomainVerified)
        {
            var verificationError = new Error("EmailEngine.DomainNotVerified", $"O domínio '{command.SenderDomain}' não está verificado para este inquilino.");

            // Grava histórico de erro no banco
            var failedHistoryResult = EmailHistory.Create(
                command.TenantId,
                command.To,
                subject,
                body,
                command.SenderDomain,
                isSuccess: false,
                errorMessage: verificationError.Message);

            if (failedHistoryResult.IsSuccess)
            {
                await _emailHistoryRepository.AddAsync(failedHistoryResult.Value, cancellationToken);
            }

            return Result.Failure(verificationError);
        }

        // 2. Busca configurações customizadas do Tenant atual (se houver)
        var customSettings = await _providerSettingsRepository.GetByTenantIdAsync(command.TenantId, cancellationToken);

        // 3. Resolve Remetente Padrão (Customizado do Tenant ou gerado dinamicamente a partir do domínio verificado)
        var senderAddress = customSettings?.SenderAddress 
            ?? $"noreply@{command.SenderDomain.Trim().ToLowerInvariant()}";

        var senderName = customSettings?.SenderName 
            ?? _configuration["EmailSettings:SenderName"] 
            ?? "System Sender";

        // 3. Processa Substituição de Variáveis de Template (se houver)
        body = command.Body ?? string.Empty;
        subject = command.Subject ?? string.Empty;

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
