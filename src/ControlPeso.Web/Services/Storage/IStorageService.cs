namespace ControlPeso.Web.Services.Storage;

/// <summary>
/// Interfaz unificada para almacenamiento persistente en navegador.
/// Abstrae diferentes mecanismos de storage (localStorage, sessionStorage, cookies).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Obtiene un valor del storage.
    /// </summary>
    /// <param name="key">Clave del valor a obtener</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Valor almacenado o null si no existe</returns>
    Task<string?> GetItemAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Guarda un valor en el storage.
    /// </summary>
    /// <param name="key">Clave del valor</param>
    /// <param name="value">Valor a almacenar</param>
    /// <param name="ct">Token de cancelación</param>
    Task SetItemAsync(string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Elimina un valor del storage.
    /// </summary>
    /// <param name="key">Clave del valor a eliminar</param>
    /// <param name="ct">Token de cancelación</param>
    Task RemoveItemAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Limpia todo el storage.
    /// </summary>
    /// <param name="ct">Token de cancelación</param>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Verifica si una clave existe en el storage.
    /// </summary>
    /// <param name="key">Clave a verificar</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>True si la clave existe, False en caso contrario</returns>
    Task<bool> ContainsKeyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Obtiene la cantidad de elementos en el storage.
    /// </summary>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Número de elementos almacenados</returns>
    Task<int> LengthAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene la clave en el índice especificado.
    /// </summary>
    /// <param name="index">Índice de la clave</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Clave en el índice especificado o null si no existe</returns>
    Task<string?> KeyAsync(int index, CancellationToken ct = default);
}
