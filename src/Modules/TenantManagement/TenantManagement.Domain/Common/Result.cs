using System;

namespace TenantManagement.Domain.Common;

/// <summary>
/// Representa um erro de negócio estruturado.
/// </summary>
public record Error(string Code, string Message)
{
    /// <summary>
    /// Representa a ausência de erro.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Erro para valores nulos.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "O valor fornecido não pode ser nulo.");
}

/// <summary>
/// Representa o resultado de uma operação de negócio.
/// </summary>
public class Result
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indica se a operação falhou.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// O erro associado ao resultado da operação.
    /// </summary>
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Um resultado de sucesso não pode ter um erro.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Um resultado de falha precisa ter um erro associado.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Retorna um resultado de sucesso.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Retorna um resultado de falha contendo um erro.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// Representa o resultado de uma operação de negócio contendo um valor de retorno.
/// </summary>
/// <typeparam name="TValue">O tipo do valor retornado.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected Result(TValue? value, bool isSuccess, Error error) 
        : base(isSuccess, error) => _value = value;

    /// <summary>
    /// O valor retornado pela operação de sucesso. Lança exceção se acessado em um resultado de falha.
    /// </summary>
    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado com falha.");

    /// <summary>
    /// Cria um resultado de sucesso com o valor fornecido.
    /// </summary>
    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);

    /// <summary>
    /// Cria um resultado de falha com o erro fornecido.
    /// </summary>
    public static new Result<TValue> Failure(Error error) => new(default, false, error);
}
