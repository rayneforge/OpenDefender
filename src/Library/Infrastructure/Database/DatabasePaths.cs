namespace Library.Infrastructure.Database;

internal static class DatabasePaths
{
    public static string GetPath(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("OPENDEFENDER_DATA_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localData))
                throw new InvalidOperationException("Set OPENDEFENDER_DATA_DIR to a writable directory.");
            directory = Path.Combine(localData, "OpenDefender");
        }
        directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, fileName);
    }
}
