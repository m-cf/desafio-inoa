using Microsoft.Extensions.Hosting;
using MonitorB3.App.Interfaces;
using MonitorB3.App.Models;

namespace MonitorB3.App.Services
{
    // a classe principal que orquestra o monitoramento (o motor)
    public interface IMonitorEngine : IHostedService { }

    public class MonitorEngine : IMonitorEngine
    {
        private readonly SmtpConfig _smtpConfig;
        private readonly InputConfig _inputConfig;
        private readonly ICotacaoService _cotacaoService;
        private readonly IEmailService _emailService;
        private Timer? _timer = null;

        // injeção de Dependência no construtor
        public MonitorEngine(SmtpConfig smtpConfig, InputConfig inputConfig, ICotacaoService cotacaoService, IEmailService emailService)
        {
            _smtpConfig = smtpConfig;
            _inputConfig = inputConfig;
            _cotacaoService = cotacaoService;
            _emailService = emailService;
        }
        
        // variáveis de estado para evitar e-mails repetidos
        private bool alertaVendaDisparado = false;
        private bool alertaCompraDisparado = false;
        
        // método chamado quando o Host inicia
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"----------------------------------------------------------------------");
            Console.WriteLine($"Monitoramento Iniciado:");
            Console.WriteLine($"Ativo: {_inputConfig.Ativo}");
            Console.WriteLine($"Preço Venda: R$ {_inputConfig.PrecoVenda:N2}");
            Console.WriteLine($"Preço Compra: R$ {_inputConfig.PrecoCompra:N2}");
            Console.WriteLine($"E-mail Alerta: {_inputConfig.ToEmail}");
            Console.WriteLine($"----------------------------------------------------------------------");

            // inicia o loop de monitoramento a cada 30 segundos
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        // loop de trabalho do monitor
        private async void DoWork(object? state)
        {
            decimal preco = await _cotacaoService.ObterCotacaoAsync(_inputConfig.Ativo, _smtpConfig.ApiKey);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Cotação atual de {_inputConfig.Ativo}: R$ {preco:N2}");

            if (preco > 0)
            {
                VerificarLimites(_inputConfig.Ativo, preco, _inputConfig.PrecoVenda, _inputConfig.PrecoCompra, _smtpConfig, _inputConfig.ToEmail);
            }
        }

        // lógica de Verificação e Gerenciamento de Alertas
        private void VerificarLimites(string ativo, decimal precoAtual, decimal precoVenda, decimal precoCompra, SmtpConfig config, string toEmail)
        {
            // alerta de VENDA (Preço subiu)
            if (precoAtual >= precoVenda)
            {
                if (!alertaVendaDisparado)
                {
                    string assunto = $"ALERTA DE VENDA: {ativo} acima de R$ {precoVenda:N2}";
                    string corpo = $"O ativo {ativo} atingiu R$ {precoAtual:N2}, igual ou superior ao preço de venda R$ {precoVenda:N2}. Aconselha-se a Venda.";
                    _emailService.EnviarAlerta(assunto, corpo, config, toEmail);
                    alertaVendaDisparado = true;
                    alertaCompraDisparado = false; // permite novo alerta de compra se o preço cair
                }
            }
            // alerta de COMPRA (Preço caiu)
            else if (precoAtual <= precoCompra)
            {
                if (!alertaCompraDisparado)
                {
                    string assunto = $"ALERTA DE COMPRA: {ativo} abaixo de R$ {precoCompra:N2}";
                    string corpo = $"O ativo {ativo} atingiu R$ {precoAtual:N2}, igual ou inferior ao preço de compra R$ {precoCompra:N2}. Aconselha-se a Compra.";
                    _emailService.EnviarAlerta(assunto, corpo, config, toEmail);
                    alertaCompraDisparado = true;
                    alertaVendaDisparado = false; // permite novo alerta de venda se o preço subir
                }
            }
            // preço entre os limites
            else
            {
                // resetar alertas para permitir novo disparo
                alertaVendaDisparado = false;
                alertaCompraDisparado = false;
            }
        }

        // método chamado quando o Host é parado
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            Console.WriteLine("Monitoramento finalizado.");
            return Task.CompletedTask;
        }
    }
}
