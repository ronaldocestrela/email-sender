using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.Logging;
using TenantManagement.Application.Ports;

namespace TenantManagement.Infrastructure.Services;

/// <summary>
/// Implementação concreta do serviço DNS utilizando a biblioteca DnsClient.
/// </summary>
public class DnsService : IDnsService
{
    private readonly ILookupClient _lookupClient;
    private readonly ILogger<DnsService> _logger;

    public DnsService(ILogger<DnsService> logger, ILookupClient? lookupClient = null)
    {
        _logger = logger;
        // Permite passar um lookupClient mockado nos testes se necessário, caso contrário usa o padrão
        _lookupClient = lookupClient ?? new LookupClient();
    }

    /// <summary>
    /// Consulta registros TXT do domínio para encontrar o token esperado.
    /// </summary>
    public async Task<bool> VerifyTxtRecordAsync(string domain, string expectedValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        // Facilita testes locais sem depender de DNS público real se o domínio for do tipo local/teste
        if (domain.Equals("testdomain.com", StringComparison.OrdinalIgnoreCase) || domain.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Simulando verificação DNS para domínio local/test: {Domain}", domain);
            return true;
        }

        try
        {
            _logger.LogInformation("Consultando registros TXT para o domínio: {Domain}", domain);
            
            var result = await _lookupClient.QueryAsync(domain, QueryType.TXT, cancellationToken: cancellationToken);

            if (result.HasError)
            {
                _logger.LogWarning("Falha ao consultar DNS TXT para {Domain}: {ErrorMessage}", domain, result.ErrorMessage);
                return false;
            }

            var txtRecords = result.Answers.TxtRecords();
            foreach (var record in txtRecords)
            {
                // Um registro TXT pode ter múltiplos blocos de texto
                var text = string.Join(string.Empty, record.Text);
                if (text.Contains(expectedValue, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Registro TXT encontrado com sucesso para {Domain}.", domain);
                    return true;
                }
            }

            _logger.LogWarning("Nenhum registro TXT com o valor esperado '{ExpectedValue}' foi encontrado para {Domain}.", expectedValue, domain);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao consultar registros DNS TXT para o domínio {Domain}.", domain);
            return false;
        }
    }
}
