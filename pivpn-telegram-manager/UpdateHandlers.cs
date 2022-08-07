using QRCoder;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace PiVPNTelemanager;
public static class UpdateHandlers
{
    public static Task PollingErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
            _ => UnknownUpdateHandlerAsync(botClient, update)
        };

        try
        {
            await handler;
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            await PollingErrorHandler(botClient, exception, cancellationToken);
        }
    }

    private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
    {
        Console.WriteLine($"Receive message type: {message.Type}");

        if (message.Text is not { } messageText)
            return;

        if (ConfigData.TelegramBotAdmins != null && ConfigData.TelegramBotAdmins.Length > 0)
            if (!ConfigData.TelegramBotAdmins.Contains(message.Chat.Username))
                return;

        var action = messageText.Split(' ')[0] switch
        {
            "/add" => ConfAdd(botClient, message),
            "/del" => ConfDel(botClient, message),
            "/cfg" => ConfGetFile(botClient, message),
            "/qr" => ConfGetQR(botClient, message),
            "/list" => ConfGetList(botClient, message),
            _ => Usage(botClient, message)
        };

        if (action != null)
        {
            Message sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");
        }


        static async Task<Message> ConfAdd(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == null)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Invalid input. Type /start for usage information.");

            string[] command = message.Text.Split(' ');

            if (ConfigData.PiVPNConfigsType == ".conf" && command.Length != 2)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Invalid input. Type /start for usage information.");

            if (ConfigData.PiVPNConfigsType == ".ovpn" && (command.Length < 3 || command.Length > 4))
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Invalid input. Type /start for usage information.");

            if (GetConfFiles().Contains(command[1] + ConfigData.PiVPNConfigsType))
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"The client already exists.");

            if (ConfigData.PiVPNConfigsType == ".conf")
                await SendCommand($"pivpn -a -n {command[1]}");
            else
                await SendCommand($"pivpn -a -n {command[1]} -d {command[2]} {(command.Length > 3 ? $"-p {command[3]}" : "nopass")}");

            string text = "Client added.";
            if (!GetConfFiles().Contains(command[1] + ConfigData.PiVPNConfigsType))
                text = "Error. Unable to add client.";

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: text);
        }


        static async Task<Message> ConfDel(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == null || message.Text.Split(' ').Length != 2)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Invalid input. Type /start for usage information.");

            string client = message.Text.Split(' ')[1];
            if (!GetConfFiles().Contains(client + ConfigData.PiVPNConfigsType))
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Client does not exist.");

            await SendCommand($"pivpn -r {client} -y");

            string text = "Client removed.";
            if (GetConfFiles().Contains(client + ConfigData.PiVPNConfigsType))
                text = "Error. Unable to remove client.";

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: text);
        }


        static async Task<Message> ConfGetList(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == null)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Invalid input. Type /start for usage information.");

            string[] configs = GetConfFiles();

            if (configs.Length == 0)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "No client profiles.");

            string text = "Clients:\n";

            foreach (string conf in configs)
            {
                string fileExt = new FileInfo(ConfigData.PiVPNConfigsPath + conf).Extension;
                text += $"•  {conf.Replace(fileExt, "")}\n";
            }

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: text);
        }


        static async Task<Message> ConfGetFile(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == null || message.Text.Split(' ').Length != 2)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Invalid input. Type /start for usage information.");

            string clientName = message.Text.Split(' ')[1];
            string confFile = clientName + ConfigData.PiVPNConfigsType;
            string confFilePath = $"{ConfigData.PiVPNConfigsPath}{Path.DirectorySeparatorChar}{confFile}";

            if (!GetConfFiles().Contains(confFile))
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Client does not exist.");

            if (new FileInfo(confFilePath).Length == 0)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "The client configuration file is empty. Try removing it first.");

            using FileStream fileStream = new(confFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.UploadDocument);
            return await botClient.SendDocumentAsync(chatId: message.Chat.Id, document: new InputOnlineFile(fileStream, confFile));
        }


        static async Task<Message> ConfGetQR(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == null || message.Text.Split(' ').Length != 2)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Invalid input. Type /start for usage information.");

            string clientName = message.Text.Split(' ')[1];
            string confFile = clientName + ConfigData.PiVPNConfigsType;
            string confFilePath = $"{ConfigData.PiVPNConfigsPath}{Path.DirectorySeparatorChar}{confFile}";

            if (!GetConfFiles().Contains(confFile))
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Client does not exist.");

            FileInfo fileInfo = new(confFilePath);

            if (fileInfo.Length == 0)
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "The client configuration file is empty. Try removing it first.");

            using QRCodeGenerator qrGenerator = new();
            string confFileText = await fileInfo.OpenText().ReadToEndAsync();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(confFileText, QRCodeGenerator.ECCLevel.L);
            using PngByteQRCode qrCode = new(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            using MemoryStream ms = new(qrCodeBytes);

            await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);
            return await botClient.SendPhotoAsync(chatId: message.Chat.Id, photo: new InputOnlineFile(ms), caption: clientName);
        }


        static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
        {
            const string usageOpenVPN =
                "Manage your VPN clients with the following commands:\n" +
                "/add <name> <days> [[<pass>]]  ‒  create a client\n" +
                "/del <name>  ‒  remove a client\n" +
                "/cfg <name>  ‒  get client config file\n" +
                "/list  ‒  list all clients\n\n";

            const string usageWireGuard =
                "Manage your VPN clients with the following commands:\n" +
                "/add <name>  ‒  create a client\n" +
                "/del <name>  ‒  remove a client\n" +
                "/cfg <name>  ‒  get client config file\n" +
                "/qr <name>  ‒  generate client qrcode\n" +
                "/list  ‒  list all clients\n";

            string usage = ConfigData.PiVPNConfigsPath!.Contains("ovpns") ? usageOpenVPN : usageWireGuard;
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: usage, parseMode: ParseMode.Markdown);
        }

    }

    private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }

    private static async Task SendCommand(string command)
    {
        using Process p = new();
        p.StartInfo.FileName = "/bin/bash";
        p.StartInfo.Arguments = command;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
        p.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync();
    }

    private static string[] GetConfFiles()
    {
        string[] files = Directory.GetFiles(ConfigData.PiVPNConfigsPath!, $"*{ConfigData.PiVPNConfigsType}");

        for (int i = 0; i < files.Length; i++)
            files[i] = files[i].Split(Path.DirectorySeparatorChar).Last();

        return files;
    }

}