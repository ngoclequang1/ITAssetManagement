namespace ITAssetManagement.Services
{
    using System.Net;
    using System.Net.Mail;

    namespace ITAssetManagement.Services
    {
        public class EmailServices : IEmailService
        {
            private readonly IConfiguration _config;

            public EmailServices(IConfiguration config)
            {
                _config = config;
            }

            public void SendEmail(string to, string subject, string body)
            {
                var fromEmail = _config["Email:From"];
                var password = _config["Email:Password"];

                var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(fromEmail, password),
                    EnableSsl = true
                };

                var message = new MailMessage(fromEmail, to, subject, body);

                smtp.Send(message);
            }
        }
    }
}
