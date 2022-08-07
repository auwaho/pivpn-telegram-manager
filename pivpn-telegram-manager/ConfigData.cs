using System.Text.Json;
using PiVPNTelemanager.Models;

namespace PiVPNTelemanager;
public static class ConfigData
{
    public static string? TelegramBotToken { get; set; }
    public static string[]? TelegramBotAdmins { get; set; }
    public static string? PiVPNConfigsPath { get; set; }
    public static string? PiVPNConfigsType { get; set; }

    public static bool Set()
    {
        var data = JsonParse();
        if (data != null)
        {
            TelegramBotToken = data.TelegramBotToken;
            TelegramBotAdmins = data.TelegramBotAdmins;
            PiVPNConfigsPath = data.PiVPNConfigsPath;
            PiVPNConfigsType = data.PiVPNConfigsPath!.Contains("ovpns") ? ".ovpn" : ".conf";
            return true;
        }
        return false;
    }

    private static Configuration? JsonParse()
    {
        string settFilePath = AppDomain.CurrentDomain.BaseDirectory + "config.json";
        FileInfo settFile = new(settFilePath);

        if (!settFile.Exists)
        {
            ConsoleError("File doesn't exist");
            return null;
        }

        string settJson = settFile.OpenText().ReadToEnd();
        Configuration config;

        try
        {
            config = JsonSerializer.Deserialize<Configuration>(settJson)!;
        }
        catch (Exception ex)
        {
            ConsoleError(ex.Message);
            return null;
        }

        if (string.IsNullOrWhiteSpace(config.TelegramBotToken))
        {
            ConsoleError("Invalid TelegramBotToken value");
            return null;
        }

        if (string.IsNullOrWhiteSpace(config.PiVPNConfigsPath) ||
            (!config.PiVPNConfigsPath.Contains("configs") && !config.PiVPNConfigsPath.Contains("ovpns")))
        {
            ConsoleError("Invalid PiVPNConfigsPath value");
            return null;
        }

        return config;
    }

    private static void ConsoleError(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
