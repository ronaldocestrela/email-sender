using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Gateway.Blazor.Services;

/// <summary>
/// Serviço auxiliar para leitura e gravação persistente no LocalStorage do navegador.
/// </summary>
public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Salva um valor serializado sob a chave especificada.
    /// </summary>
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    /// <summary>
    /// Recupera e desserializa um valor associado à chave especificada.
    /// </summary>
    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Remove um item associado à chave especificada.
    /// </summary>
    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }
}
