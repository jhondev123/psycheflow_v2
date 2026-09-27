namespace Psycheflow.Api.Common.Storage;

/// <summary>
/// Armazenamento de arquivos (anexos de prontuário, D-06). A implementação local grava em disco (volume Docker);
/// trocar por S3/Azure Blob é só registrar outra implementação.
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken);

    /// <exception cref="FileNotFoundException">Quando a chave não existe.</exception>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public static class StorageSetup
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName);
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Pasta raiz dos arquivos (fora do wwwroot). Relativa ao diretório da aplicação quando não for absoluta.</summary>
    public string Path { get; set; } = "storage";
}

public sealed class LocalFileStorage(Microsoft.Extensions.Options.IOptions<StorageOptions> options) : IFileStorage
{
    private readonly string _root = System.IO.Path.GetFullPath(options.Value.Path);

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        string path = Resolve(key);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        await using FileStream file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        string path = Resolve(key);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Arquivo não encontrado no armazenamento.", key);
        }

        return Task.FromResult<Stream>(File.OpenRead(path));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(Resolve(key));
        return Task.CompletedTask;
    }

    /// <summary>Resolve a chave dentro da raiz, impedindo path traversal ("../").</summary>
    private string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || System.IO.Path.IsPathRooted(key))
        {
            throw new ArgumentException("Chave de arquivo inválida.", nameof(key));
        }

        string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(_root, key));
        if (!full.StartsWith(_root + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Chave de arquivo fora da área de armazenamento.", nameof(key));
        }

        return full;
    }
}
