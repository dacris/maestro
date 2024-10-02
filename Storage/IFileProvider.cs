using System.Reflection;

namespace Dacris.Maestro.Storage
{
    public interface IFileProvider
    {
        Task<Stream> ReadAsync(FileLocation source);
        Task WriteAsync(Stream source, FileLocation destination);
        Task DeleteAsync(FileLocation file);
        Task<bool> ExistsAsync(FileLocation file);
        Task<IEnumerable<string>> ListAsync(FileLocation dir, string pattern);
    }

    public class FileLocation
    {
        public Interaction Interaction { get; set; }
        public string Path { get; set; }
        public string StandardizedPath { get; set; }
        public IFileProvider FileProvider => GetFileProvider();

        private Assembly? _assembly;

        public FileLocation (Interaction interaction, string path)
        {
            Interaction = interaction;
            Path = path;
            StandardizedPath = Path.Replace('\\', '/').WithoutPrefix();
        }

        private IFileProvider GetFileProvider()
        {
            var pathPrefix = Path.IndexOf(':') <= 1 ? Path : Path.Substring(0, Path.IndexOf(':'));
            var storageLocations = AppState.Instance.ReadKey("storageLocations");
            var systemType = storageLocations?[pathPrefix]?["systemType"]?.ToString();
            var customProviders = AppState.Instance.ReadKey("customStorageProviders");
            if (customProviders?[systemType ?? "none"] is not null)
            {
                var namespaceName = customProviders?[systemType ?? "none"]!.ToString();
                _assembly = Assembly.LoadFile(System.IO.Path.Combine(
                                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                                    namespaceName + ".dll"));
                return (IFileProvider)_assembly.CreateInstance(namespaceName + "." + systemType + "FileProvider",
                    true, BindingFlags.Default, null,
                    [Interaction, pathPrefix], null, null)!;
            }
            switch (systemType)
            {
                case "SFTP":
                    return new SFTPFileProvider(Interaction, pathPrefix);
                default:
                    return new LocalFileProvider();
            }
        }
    }
}
