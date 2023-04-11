using System.Text.Json;

namespace Discord.Net.BearerServer.Configuration;

internal static class Config
{
    public static Messages Messages { get; private set; } = new Messages();
    public static Uris Uris { get; private set; } = new Uris();
    public static Secrets Secrets { get; private set; } = new Secrets();

    private static readonly string _appDirectory;
    private static readonly string _secretsFilePath;

    static Config()
    {
        _appDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            nameof(BearerServer));
        _secretsFilePath = Path.Combine(_appDirectory, "secrets.json");
    }
    
    public static void SetUp(IConfigurationRoot configurationRoot)
    {
        configurationRoot.GetSection(nameof(Messages))
            .Bind(Messages);
        configurationRoot.GetSection(nameof(Uris))
            .Bind(Uris);
        
        SetUpSecrets();
    }

    private static void SetUpSecrets()
    {
        if (!File.Exists(_secretsFilePath))
        {
            Directory.CreateDirectory(_appDirectory);
            
            File.WriteAllText(_secretsFilePath, JsonSerializer.Serialize(Secrets, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            throw new ArgumentException($"\"{_secretsFilePath}\" does not contain any data");
        }

        Secrets = JsonSerializer.Deserialize<Secrets>(File.ReadAllText(_secretsFilePath)) ?? new Secrets();
    }
}