using System.Threading;
using System.Threading.Tasks;
using EmailEngine.Domain.Common;
using EmailEngine.Domain.Entities;

namespace EmailEngine.Application.Ports;

/// <summary>
/// Port de saída para envio físico de e-mail utilizando adapters de SMTP ou APIs de produção.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envia o e-mail fisicamente utilizando as configurações especificadas ou as padrões do sistema.
    /// </summary>
    Task<Result> SendAsync(
        string to,
        string subject,
        string body,
        string senderAddress,
        string senderName,
        EmailProviderSettings? customSettings = null,
        CancellationToken cancellationToken = default);
}
