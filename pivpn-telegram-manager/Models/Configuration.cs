namespace PiVPNTelemanager.Models;
class Configuration
{
    public string? TelegramBotToken { get; set; }
    public string[]? TelegramBotAdmins { get; set; }
    public string? PiVPNConfigsPath { get; set; }
}