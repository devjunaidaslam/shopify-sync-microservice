using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ShopifySync_BusinessLogicLayer.Functions;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities.Email;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class EmailService : IEmailService
    {
        public void SendEmail(Message message)
        {
            try
            {
                // Create the email message
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("ShopifySync", Settings.EmailFrom()));
                emailMessage.To.AddRange(message.To);
                emailMessage.Subject = message.Subject;
                var bodyBuilder = new BodyBuilder { HtmlBody = message.Content };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                // Send the email message
                using var client = new SmtpClient();
                client.Connect(Settings.EmailSmtpServer(), Convert.ToInt32(Settings.EmailPort()), SecureSocketOptions.StartTls);
                client.AuthenticationMechanisms.Remove("XOAUTH2"); // Check if necessary to remove XOAUTH2
                client.Authenticate(Settings.EmailUsername(), Settings.EmailPassword());
                client.Send(emailMessage);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}
