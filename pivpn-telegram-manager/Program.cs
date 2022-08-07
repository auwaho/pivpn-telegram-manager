using PiVPNTelemanager;

if (!ConfigData.Set())
{
    Console.WriteLine("botsettings.json read error");
    return;
}

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();