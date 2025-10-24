namespace MonitorB3.App.Models
{
    // estrutura para ler as configurações de SMTP do appsettings.json e do .env
    public class SmtpConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
