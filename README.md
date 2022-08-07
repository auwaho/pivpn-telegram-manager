## PiVPN Telegram Manager
With this telegram bot you can manage clients for WireGuard/OpenVPN installed with PiVPN.

## Features
* Create new clients.
* Removing existing clients.
* Getting the client configuration file (for WireGuard and OpenVPN).
* Obtaining a client QR code (for the WireGuard mobile application).
* Getting a list of clients.

## Installation

### 1. Publish application 

Clone project, open it and run command: 

```bash
$ dotnet publish -c release -r linux-x64 --self-contained
```

### 2. Preparation and setup

Upload the *publish* folder to the server using WinSCP or whatever method you like, and then fill the *config.json* file inside the *publish* folder following this example:

| Key | Default | Example | Description |
| - | - | - | - |
| `TelegramBotToken` | "" | `"1234567890:NNWQxDDCqFB5Hf9AdaRTiA12-c5qWbqCCLN"` | Telegram bot authentication token. |
| `TelegramBotAdmins` | [ ] | `[ "user1", "user2" ]` | Names of users who can use the bot. **Attention**: It is strongly recommended to specify users, if left empty, then any user can manage the bot! |
| `PiVPNConfigsPath` | "" | `"/home/ubuntu/configs/"` | The path where the client configuration files are located. Must contain "configs" for WireGuard and "ovpns" for OpenVPN. |

### 3. Run pivpn-telegram-manager

Get superuser rights and go to the folder with the bot, then run the commands:

```bash
$ chmod 777 ./pivpn-telegram-manager
$ ./pivpn-telegram-manager
```

Or to run as a service, first install *systemd* on your server, then create a *.service* file:

```bash
$ nano /etc/systemd/system/pivpn-telegram-manager.service
```

The contents of the *pivpn-telegram-manager.service* file:

```
[Unit]
Description=PiVPN Telegram Manager

[Service]
WorkingDirectory=/path/to/app/
ExecStart=/usr/bin/dotnet /path/to/app/pivpn-telegram-manager.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=log.pivpntgmanager
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
```

Where `/path/to/app/` is the path to the *publish* folder.

Then reload Unit files and start service:

```bash
$ systemctl daemon-reload
$ systemctl start pivpn-telegram-manager
```
