// lógica de inicialização, configuração e injeção de dependência.
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonitorB3.App.Interfaces;
using MonitorB3.App.Models;
using MonitorB3.App.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1. carregar o .env antes de tudo (DotNetEnv)
        Env.Load();

        // 2. tentar validar e parsear os argumentos de linha de comando
        var inputConfig = ParseArguments(args);
        if (!inputConfig.IsValid())
        {
            ExibirUso();
            return;
        }

        // 3. configurar o Host e a Injeção de Dependência (DI)
        using IHost host = CreateHostBuilder(inputConfig).Build();

        // 4. rodar o Host (que irá executar o IHostedService - MonitorEngine)
        await host.RunAsync();
    }

    // cria e configura o Host e o contêiner de DI
    private static IHostBuilder CreateHostBuilder(InputConfig inputConfig) =>
        Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                // carrega appsettings.json e variáveis de ambiente (onde o .env está)
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                configuration.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                // carregar SmtpConfig
                var smtpConfig = hostContext.Configuration.GetSection("SmtpConfig").Get<SmtpConfig>();

                // validação de configuração
                if (smtpConfig == null || string.IsNullOrEmpty(smtpConfig.FromEmail) || string.IsNullOrEmpty(smtpConfig.ApiKey))
                {
                    Console.WriteLine("ERRO FATAL: Configurações SmtpConfig incompletas. Verifique appsettings.json e .env.");
                    Environment.Exit(1);
                }

                // registrar Instâncias

                // configuração
                services.AddSingleton(smtpConfig);
                services.AddSingleton(inputConfig);

                // serviços
                services.AddSingleton<ICotacaoService, CotacaoService>();
                services.AddSingleton<IEmailService, EmailService>();

                // registra o Hosted Service para ser executado pelo Host
                // monitorEngine implementa IMonitorEngine e IHostedService
                services.AddHostedService<MonitorEngine>();
            });

    // função para parsear os argumentos de linha de comando
    private static InputConfig ParseArguments(string[] args)
    {
        if (args.Length != 4)
        {
            return new InputConfig("", 0, 0, "");
        }

        // uso de InvariantCulture para garantir que o parse decimal funcione
        if (!decimal.TryParse(args[1], System.Globalization.CultureInfo.InvariantCulture, out decimal precoVenda) || precoVenda <= 0)
        {
            Console.WriteLine($"Erro: Preço de Venda inválido: {args[1]}");
            return new InputConfig("", 0, 0, "");
        }

        if (!decimal.TryParse(args[2], System.Globalization.CultureInfo.InvariantCulture, out decimal precoCompra) || precoCompra <= 0)
        {
            Console.WriteLine($"Erro: Preço de Compra inválido: {args[2]}");
            return new InputConfig("", 0, 0, "");
        }

        string ativo = args[0].ToUpper();
        string toEmail = args[3];

        // usa o record InputConfig para encapsular a validação
        var config = new InputConfig(ativo, precoVenda, precoCompra, toEmail);

        return config;
    }

    // função de exibição de uso
    private static void ExibirUso()
    {
        Console.WriteLine("Uso: dotnet run -- <Ativo> <PrecoVenda> <PrecoCompra> <EmailDestino>");
        Console.WriteLine("Exemplo: dotnet run -- PETR4 30.00 25.00 seu.amigo@email.com");
        Console.WriteLine("\n[ERRO] É necessário fornecer 4 parâmetros válidos.");
    }
}
