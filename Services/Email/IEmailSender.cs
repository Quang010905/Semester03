using System.Threading;
using System.Threading.Tasks;

namespace Semester03.Services.Email
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage, CancellationToken cancellationToken = default);

        Task SendEmailWithInlineImageAsync(
            string toEmail,
            string subject,
            string htmlMessage,
            byte[] imageBytes,
            string imageContentId = "qrImage",
            CancellationToken cancellationToken = default);
    }
}
