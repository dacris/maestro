using Renci.SshNet;
using System.IO.Enumeration;

namespace Dacris.Maestro.Storage;

public class SFTPFileProvider : IFileProvider, IDisposable
{
    private readonly SftpClient _sftpClient;
    private bool _usesExistingClient = false;
    public string LocationPrefix { get; set; }

    public SFTPFileProvider(Interaction interaction, string locationPrefix)
    {
        LocationPrefix = locationPrefix;
        var existingProvider = interaction.BlockSessionResources!.FirstOrDefault(x => ProviderHasPrefix(x, locationPrefix));
        if (existingProvider != null)
        {
            _sftpClient = (existingProvider as SFTPFileProvider)!._sftpClient;
            _usesExistingClient = true;
            return;
        }
        var storageLocations = AppState.Instance.ReadKey("storageLocations")!;
        var storageLocation = storageLocations[locationPrefix]!;
        _sftpClient = new SftpClient(storageLocation["serverHost"]!.ToString(),
            int.Parse(storageLocation["port"]?.ToString() ?? "22"),
            storageLocation["user"]!.ToString(),
            AppState.Instance.ReadKey(storageLocation["secretKey"]!.ToString())!.ToString());
        interaction.BlockSessionResources!.Add(this);
        _sftpClient.Connect();
    }

    private static bool ProviderHasPrefix(IDisposable provider, string locationPrefix)
    {
        return (provider as SFTPFileProvider)?.LocationPrefix == locationPrefix;
    }

    public Task DeleteAsync(FileLocation file)
    {
        _sftpClient.DeleteFile(file.StandardizedPath);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_usesExistingClient)
        {
            _sftpClient.Disconnect();
            _sftpClient.Dispose();
        }
    }

    public Task<bool> ExistsAsync(FileLocation file)
    {
        return Task.FromResult(_sftpClient.Exists(file.StandardizedPath));
    }

    public Task<IEnumerable<string>> ListAsync(FileLocation dir, string pattern)
    {
        return Task.FromResult(_sftpClient.ListDirectory(dir.StandardizedPath)
            .ToArray()
            .Where(f => FileSystemName.MatchesSimpleExpression(pattern, f.Name) && !f.Name.All(c => c == '.'))
            .Select(f => f.Name));
    }

    public async Task<Stream> ReadAsync(FileLocation source)
    {
        var stream = await _sftpClient.OpenAsync(source.StandardizedPath, FileMode.Open, FileAccess.Read, CancellationToken.None);
        return stream;
    }

    public async Task WriteAsync(Stream source, FileLocation destination)
    {
        using var outStream = _sftpClient.Create(destination.StandardizedPath);
        await source.CopyToAsync(outStream);
    }
}
