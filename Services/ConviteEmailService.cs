using FutPlay.Models;
using FutPlay.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace FutPlay.Services
{
    public class ConviteEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<ConviteEmailService> _logger;

        public ConviteEmailService(
            IOptions<EmailOptions> options,
            ILogger<ConviteEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ConviteEmailResultado> EnviarConviteAsync(
            LigaConvite convite,
            string linkConvite)
        {
            if (!_options.EstaConfigurado)
            {
                return ConviteEmailResultado.NaoConfigurado();
            }

            if (convite.Liga == null)
            {
                return ConviteEmailResultado.Falha("Liga do convite não carregada.");
            }

            try
            {
                using var mensagem = new MailMessage
                {
                    From = new MailAddress(_options.FromEmail, _options.FromName, Encoding.UTF8),
                    Subject = $"Convite para {convite.Liga.Nome}",
                    Body = MontarCorpo(convite, linkConvite),
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8,
                    IsBodyHtml = true
                };

                mensagem.To.Add(new MailAddress(convite.Email, convite.NomeConvidado ?? convite.Email, Encoding.UTF8));

                using var smtp = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl
                };

                if (!string.IsNullOrWhiteSpace(_options.UserName))
                {
                    smtp.Credentials = new NetworkCredential(_options.UserName, _options.Password);
                }

                await smtp.SendMailAsync(mensagem);

                return ConviteEmailResultado.Enviado();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao enviar convite por e-mail. LigaId: {LigaId}. Email: {Email}",
                    convite.LigaId,
                    convite.Email);

                return ConviteEmailResultado.Falha("Não foi possível enviar o e-mail do convite.");
            }
        }

        private static string MontarCorpo(LigaConvite convite, string linkConvite)
        {
            var nome = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(convite.NomeConvidado)
                    ? convite.Email
                    : convite.NomeConvidado);

            var liga = WebUtility.HtmlEncode(convite.Liga?.Nome ?? "liga");
            var campeonato = WebUtility.HtmlEncode(convite.Liga?.Campeonato?.Nome ?? "campeonato");
            var link = WebUtility.HtmlEncode(linkConvite);

            return $"""
                <p>Olá, {nome}.</p>
                <p>Você recebeu um convite para participar da liga <strong>{liga}</strong> no {campeonato}.</p>
                <p>
                    <a href="{link}">Aceitar convite</a>
                </p>
                <p>Se o botão não abrir, copie este link no navegador:</p>
                <p>{link}</p>
                """;
        }
    }

    public class ConviteEmailResultado
    {
        public bool Sucesso { get; set; }

        public bool Configurado { get; set; }

        public string Mensagem { get; set; } = string.Empty;

        public static ConviteEmailResultado Enviado()
        {
            return new ConviteEmailResultado
            {
                Sucesso = true,
                Configurado = true,
                Mensagem = "Convite enviado por e-mail."
            };
        }

        public static ConviteEmailResultado NaoConfigurado()
        {
            return new ConviteEmailResultado
            {
                Sucesso = false,
                Configurado = false,
                Mensagem = "SMTP não configurado."
            };
        }

        public static ConviteEmailResultado Falha(string mensagem)
        {
            return new ConviteEmailResultado
            {
                Sucesso = false,
                Configurado = true,
                Mensagem = mensagem
            };
        }
    }
}
