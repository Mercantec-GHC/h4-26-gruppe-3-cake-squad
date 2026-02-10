using MailKit.Net.Smtp;
using MimeKit;

namespace Wavelength.Services
{
    /// <summary>
    /// Represents configuration options for connecting to an SMTP server.
    /// </summary>
    /// <remarks>Use this class to specify the connection details and authentication credentials required for
    /// sending email via SMTP. These options are typically used to configure email clients or services that send
    /// outgoing messages.</remarks>
    public class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

    /// <summary>
    /// Provides functionality for composing and sending email messages using SMTP, including support for plain text and
    /// template-based HTML emails.
    /// </summary>
    /// <remarks>The MailService class relies on configuration settings for SMTP server details and uses an
    /// IEmailTemplateLoader to retrieve email templates. It is designed to be used as a utility for sending emails
    /// within an application. This class is not thread-safe; create a separate instance for each concurrent operation
    /// if needed.</remarks>
    public class MailService
    {
        private readonly SmtpOptions options;
        private readonly IEmailTemplateLoader templateLoader;

        /// <summary>
        /// Initializes a new instance of the MailService class using the specified configuration and email template
        /// loader.
        /// </summary>
        /// <param name="configuration">The application configuration from which SMTP options are read. Must contain a 'Smtp' section with the
        /// relevant settings.</param>
        /// <param name="templateLoader">The email template loader used to retrieve email templates for outgoing messages.</param>
        public MailService(IConfiguration configuration, IEmailTemplateLoader templateLoader)
        {
            this.options = configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
            this.templateLoader = templateLoader;
        }

        /// <summary>
        /// Sends an email message to the specified recipient with the given subject and body content.
        /// </summary>
        /// <remarks>The email is sent using the configured SMTP server settings. If sending fails, the
        /// error is logged to the console. This method does not throw exceptions for send failures.</remarks>
        /// <param name="to">The email address of the recipient. Cannot be null or empty.</param>
        /// <param name="subject">The subject line of the email message. Cannot be null.</param>
        /// <param name="body">The body content of the email message as a text part. Cannot be null.</param>
        public void SendEmail(string to, string subject, TextPart body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(options.Name, options.Address));
            email.To.Add(new MailboxAddress("", to));
            email.Subject = subject;
            email.Body = body;

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(options.Host, options.Port, options.EnableSsl);
                    client.Authenticate(options.Username, options.Password);
                    client.Send(email);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }
                finally
                {
                    client.Disconnect(true);
                }
            }
        }

        /// <summary>
        /// Creates a plain text email body part using the specified text content.
        /// </summary>
        /// <param name="text">The text to include in the plain text body part. Cannot be null.</param>
        /// <returns>A <see cref="TextPart"/> representing the plain text content of the email.</returns>
        public TextPart BuildSimpleText(string text)
        {
            return new TextPart("plain") { Text = text };
        }

        /// <summary>
        /// Renders an HTML template by replacing placeholders with their corresponding values.
        /// </summary>
        /// <remarks>If a placeholder key in the dictionary does not exist in the template, it is ignored.
        /// The method performs a simple string replacement and does not support advanced templating features.</remarks>
        /// <param name="template">The name of the template to render. The template name is case-insensitive.</param>
        /// <param name="placeholders">A dictionary containing placeholder keys and their replacement values. If null, no replacements are
        /// performed.</param>
        /// <returns>A TextPart object containing the rendered HTML with placeholders replaced by their values.</returns>
        public TextPart RenderTemplate(string template, Dictionary<string, string>? placeholders)
        {
            var html = templateLoader.GetTemplate(template.ToLower());

            if (placeholders != null)
            {
                foreach (var placeholder in placeholders)
                {
                    html = html.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
                }
            }
            return new TextPart("html") { Text = html };
        }
    }
}
