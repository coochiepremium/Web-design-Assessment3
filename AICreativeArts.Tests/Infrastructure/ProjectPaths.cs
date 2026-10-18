namespace AICreativeArts.Tests.Infrastructure;

/// <summary>Locates the web project folder from the test output directory.</summary>
public static class ProjectPaths
{
    public static string WebProject { get; } = Find();

    private static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "AICreativeArts", "AICreativeArts.csproj");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the AICreativeArts project folder.");
    }
}
