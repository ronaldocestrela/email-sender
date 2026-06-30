using System.Threading;
using System.Threading.Tasks;

namespace TenantManagement.Application.Ports;

/// <summary>
/// Port de saída para consultas de registros DNS.
/// </summary>
public interface IDnsService
{
    /// <summary>
    /// Consulta os registros TXT de um domínio e verifica se algum deles possui o valor esperado.
    /// </summary>
    /// <param name="domain">O domínio a ser verificado (ex: "acme.com").</param>
    /// <param name="expectedValue">O token/valor esperado no registro TXT.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se o registro com o valor esperado for encontrado, caso contrário False.</returns>
    Task<bool> VerifyTxtRecordAsync(string domain, string expectedValue, CancellationToken cancellationToken = default);
}
