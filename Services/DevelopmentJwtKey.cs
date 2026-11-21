using System.Security.Cryptography;
using System.Text;

namespace Khotwa.Services;

/// <summary>
/// Creates a persistent, per-user JWT key for local development only.
/// The key is never stored in the source repository.
/// </summary>
internal static class DevelopmentJwtKey
{
    public static string GetOrCreate()
    {
        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
            throw new InvalidOperationException("Local application data directory is unavailable.");

        string directory = Path.Combine(localData, "Khotwa");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "jwt-development.key");

        if (File.Exists(path))
            return File.ReadAllText(path).Trim();

        string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string temporaryPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");

        // Publish a fully written file atomically. If another local process
        // creates the key first, both processes use the winning key.
        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None
            };

            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

            using (var stream = new FileStream(temporaryPath, options))
                stream.Write(Encoding.UTF8.GetBytes(key));

            try
            {
                File.Move(temporaryPath, path);
                return key;
            }
            catch (IOException) when (File.Exists(path))
            {
                return File.ReadAllText(path).Trim();
            }
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
