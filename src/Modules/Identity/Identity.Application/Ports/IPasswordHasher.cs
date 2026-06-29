namespace Identity.Application.Ports;

/// <summary>
/// Port de saída para hashing e verificação de senhas.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera um hash seguro a partir de uma senha em texto claro.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifica se a senha em texto claro corresponde ao hash informado.
    /// </summary>
    bool VerifyPassword(string hashedPassword, string password);
}
