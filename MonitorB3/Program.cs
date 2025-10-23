using System.Text.Json;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

// --- 1. Classes de Configuração e Dados ---
// Estrutura para desserializar o JSON de resposta da API (ex: brapi)
public record CotacaoResult(decimal regularMarketPrice, string symbol);
public record CotacaoResponse(CotacaoResult[] results);

// Estrutura para ler as configurações de SMTP do appsettings.json
public class SmtpConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

// O ponto de entrada da aplicação precisa ser assíncrono para usar 'await'
public class Program
{
    public static async Task Main(string[] args)
    {
        // --- 2. Validação e Leitura de Parâmetros de Linha de Comando ---
        if (args.Length != 3)
        {
            Console.WriteLine("Uso: MonitorB3 <Ativo> <PrecoVenda> <PrecoCompra>");
            Console.WriteLine("Exemplo: dotnet run -- PETR4 30.00 25.00");
            return;
        }

        string ativo = args[0].ToUpper();
        
        // Uso de InvariantCulture para garantir que o parse decimal funcione em diferentes configurações regionais (ponto ou vírgula)
        if (!decimal.TryParse(args[1], System.Globalization.CultureInfo.InvariantCulture, out decimal precoVenda) || precoVenda <= 0)
        {
            Console.WriteLine($"Erro: Preço de Venda inválido: {args[1]}");
            return;
        }

        if (!decimal.TryParse(args[2], System.Globalization.CultureInfo.InvariantCulture, out decimal precoCompra) || precoCompra <= 0)
        {
            Console.WriteLine($"Erro: Preço de Compra inválido: {args[2]}");
            return;
        }

        // --- 3. Carregamento do Arquivo de Configuração (appsettings.json) ---
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var smtpConfig = configuration.GetSection("SmtpConfig").Get<SmtpConfig>();

        if (smtpConfig == null || string.IsNullOrEmpty(smtpConfig.ToEmail) || string.IsNullOrEmpty(smtpConfig.ApiKey))
        {
            Console.WriteLine("Erro de Configuração: Por favor, verifique se o 'appsettings.json' está completo e correto.");
            return;
        }

        // --- 4. Loop de Monitoramento (Core da Aplicação) ---
        TimeSpan intervalo = TimeSpan.FromSeconds(30);

        Console.WriteLine($"----------------------------------------------------------------------");
        Console.WriteLine($"Monitorando Ativo: {ativo}");
        Console.WriteLine($"Preço de Venda (ALERTA SUPERIOR): R$ {precoVenda:N2}");
        Console.WriteLine($"Preço de Compra (ALERTA INFERIOR): R$ {precoCompra:N2}");
        Console.WriteLine($"Intervalo de Verificação: {intervalo.TotalSeconds} segundos");
        Console.WriteLine($"E-mail de Alerta: {smtpConfig.ToEmail}");
        Console.WriteLine($"----------------------------------------------------------------------");
        
        // Mantém o histórico de alertas disparados para não enviar o mesmo e-mail repetidamente
        bool alertaVendaDisparado = false;
        bool alertaCompraDisparado = false;

        while (true)
        {
            decimal preco = await ObterCotacao(ativo, smtpConfig.ApiKey);
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Cotação atual de {ativo}: R$ {preco:N2}");

            if (preco > 0)
            {
                VerificarLimites(ativo, preco, precoVenda, precoCompra, smtpConfig, ref alertaVendaDisparado, ref alertaCompraDisparado);
            }

            // Pausa de forma não bloqueante
            await Task.Delay(intervalo);
        }
    }

    // --- 5. Lógica de Obtenção da Cotação ---
    // Usando a API brapi como exemplo
    private static async Task<decimal> ObterCotacao(string ativo, string apiKey)
    {
        string apiUrl = $"https://brapi.dev/api/quote/{ativo}?token={apiKey}";
        
        // Recomendado: usar HttpClientFactory em aplicações reais. Aqui, usamos o 'using var' para simplicidade.
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Configura um timeout para evitar que a aplicação trave em chamadas lentas
                client.Timeout = TimeSpan.FromSeconds(10); 
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                
                // Checa se o status HTTP indica sucesso (200)
                response.EnsureSuccessStatusCode(); 
                
                string responseBody = await response.Content.ReadAsStringAsync();
                
                // Desserializa a resposta JSON
                var data = JsonSerializer.Deserialize<CotacaoResponse>(responseBody);

                if (data?.results != null && data.results.Length > 0)
                {
                    return data.results[0].regularMarketPrice;
                }
                
                Console.WriteLine("Aviso: Resposta da API não contém o preço.");
                return -1;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro HTTP (API): Verifique se a chave '{apiKey}' e o ativo '{ativo}' estão corretos.");
                Console.WriteLine($"Detalhes: {ex.Message}");
                return -1;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Erro JSON: Falha ao processar a resposta da API. {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro Inesperado na Cotação: {ex.Message}");
                return -1;
            }
        }
    }

    // --- 6. Lógica de Verificação e Gerenciamento de Alertas ---
    private static void VerificarLimites(string ativo, decimal precoAtual, decimal precoVenda, decimal precoCompra, SmtpConfig config, ref bool alertaVendaDisparado, ref bool alertaCompraDisparado)
    {
        // Alerta de VENDA (Preço subiu)
        if (precoAtual >= precoVenda)
        {
            if (!alertaVendaDisparado)
            {
                string assunto = $"ALERTA DE VENDA: {ativo} acima de R$ {precoVenda:N2}";
                string corpo = $"O ativo {ativo} atingiu R$ {precoAtual:N2}, que é igual ou superior ao preço de venda de referência R$ {precoVenda:N2}. Aconselha-se a Venda.";
                EnviarEmailAlerta(assunto, corpo, config);
                alertaVendaDisparado = true; // Marca como disparado
                alertaCompraDisparado = false; // Permite que o alerta de compra seja disparado novamente se o preço cair
            }
        }
        // Alerta de COMPRA (Preço caiu)
        else if (precoAtual <= precoCompra)
        {
            if (!alertaCompraDisparado)
            {
                string assunto = $"ALERTA DE COMPRA: {ativo} abaixo de R$ {precoCompra:N2}";
                string corpo = $"O ativo {ativo} atingiu R$ {precoAtual:N2}, que é igual ou inferior ao preço de compra de referência R$ {precoCompra:N2}. Aconselha-se a Compra.";
                EnviarEmailAlerta(assunto, corpo, config);
                alertaCompraDisparado = true; // Marca como disparado
                alertaVendaDisparado = false; // Permite que o alerta de venda seja disparado novamente se o preço subir
            }
        }
        // Preço entre os limites
        else
        {
            // Resetar alertas (permitindo novo disparo se o preço retornar à zona de alerta)
            alertaVendaDisparado = false;
            alertaCompraDisparado = false;
        }
    }

    // --- 7. Lógica de Envio de E-mail ---
    private static void EnviarEmailAlerta(string assunto, string corpo, SmtpConfig config)
    {
        Console.WriteLine($"\n*** Disparando Alerta por E-mail: {assunto} ***");
        
        using (MailMessage mail = new MailMessage())
        using (SmtpClient smtp = new SmtpClient(config.Host, config.Port))
        {
            mail.From = new MailAddress(config.FromEmail, "Monitor B3");
            mail.To.Add(config.ToEmail);
            mail.Subject = assunto;
            mail.Body = corpo;
            mail.IsBodyHtml = false;

            // Configurações de credenciais e segurança
            smtp.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
            smtp.EnableSsl = true; 

            try
            {
                smtp.Send(mail);
                Console.WriteLine("E-mail enviado com sucesso!");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"Falha ao enviar e-mail (SMTP): Verifique as configurações de HOST, Porta e credenciais.");
                Console.WriteLine($"Erro: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Falha geral ao enviar e-mail: {ex.Message}");
            }
        }
        Console.WriteLine($"----------------------------------------------------------------------");
    }
}
