using System;
using System.Net.Mail;
using MonitorB3.App.Interfaces;
using MonitorB3.App.Models;

namespace MonitorB3.App.Services
{
    public class EmailService : IEmailService
    {
        // Esta é a única implementação do método EnviarAlerta exigido pela interface IEmailService.
        public void EnviarAlerta(string assunto, string corpo, SmtpConfig config, string toEmail)
        {
            Console.WriteLine($"\n*** Disparando Alerta por E-mail: {assunto} ***");

            using (MailMessage mail = new MailMessage())
            using (SmtpClient smtp = new SmtpClient(config.Host, config.Port))
            {
                try
                {
                    mail.From = new MailAddress(config.FromEmail, "Monitor B3");
                    mail.To.Add(toEmail); // Usa o e-mail de destino passado por parâmetro
                    mail.Subject = assunto;
                    mail.Body = corpo;
                    mail.IsBodyHtml = false;

                    // Configurações de credenciais e segurança
                    smtp.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
                    smtp.EnableSsl = true;

                    smtp.Send(mail);
                    Console.WriteLine("E-mail enviado com sucesso!");
                }
                catch (SmtpException ex)
                {
                    Console.WriteLine($"Falha ao enviar e-mail (SMTP): Verifique Host, Porta e credenciais.");
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
}
