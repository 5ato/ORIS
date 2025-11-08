using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace MiniHttpServer.Services
{
    public static class EmailService
    {
        public static void SendEmail(string to, string subject, string body, string? attachment = null)
        {
            var fromAddress = new MailAddress("pomidkil@gmail.com", "Nail");
            var toAddress = new MailAddress(to);

            using var smpt = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("pomidkil@gmail.com", "qqxa utnj yanz iopb")
            };

            var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            

            if (attachment != null)
                message.Attachments.Add(new Attachment(attachment, MediaTypeNames.Application.Zip));

            try
            {
                smpt.Send(message);

                Console.WriteLine("Email sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}