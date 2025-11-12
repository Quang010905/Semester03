using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Semester03.Areas.Client.Models.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Semester03.Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<MailSettings> mailOptions, ILogger<EmailSender> logger)
        {
            _mailSettings = mailOptions.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, CancellationToken cancellationToken = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_mailSettings.SmtpServer, _mailSettings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                _logger.LogInformation("Email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
            }
            finally
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

        public async Task SendEmailWithInlineImageAsync(string toEmail, string subject, string htmlMessage, byte[] imageBytes, string imageContentId = "qrImage", CancellationToken cancellationToken = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            var linked = builder.LinkedResources.Add($"{imageContentId}.png", imageBytes ?? Array.Empty<byte>());
            linked.ContentId = imageContentId;
            linked.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_mailSettings.SmtpServer, _mailSettings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                _logger.LogInformation("Email with inline image sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending inline image email to {Email}", toEmail);
            }
            finally
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
