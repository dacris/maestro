

namespace Dacris.Maestro.Storage
{
    public class LocalFileProvider : IFileProvider
    {
        public async Task DeleteAsync(FileLocation file)
        {
            if (File.Exists(file.StandardizedPath))
            {
                File.Delete(file.StandardizedPath);
            }
            await Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(FileLocation file)
        {
            return Task.FromResult(File.Exists(file.StandardizedPath));
        }

        public Task<IEnumerable<string>> ListAsync(FileLocation dir, string pattern)
        {
            return Task.FromResult(new DirectoryInfo(dir.StandardizedPath)
                .EnumerateFileSystemInfos()
                .Select(i => Path.GetFileName(i.FullName)));
        }

        public Task<Stream> ReadAsync(FileLocation source)
        {
            return Task.FromResult((Stream)File.OpenRead(source.StandardizedPath));
        }

        public async Task WriteAsync(Stream source, FileLocation destination)
        {
            await DeleteAsync(destination);
            using var destStream = File.OpenWrite(destination.StandardizedPath);
            await source.CopyToAsync(destStream);
        }
    }
}
