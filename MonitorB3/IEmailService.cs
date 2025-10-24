using MonitorB3.App.Models;

namespace MonitorB3.App.Interfaces
{
    // define a responsabilidade de enviar o e-mail
    public interface IEmailService
    {
        void EnviarAlerta(string assunto, string corpo, SmtpConfig config, string toEmail);
    }
}