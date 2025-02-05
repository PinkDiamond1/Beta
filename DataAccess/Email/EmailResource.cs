﻿using Auctus.DataAccessInterfaces.Email;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auctus.DataAccess.Email
{
    public class EmailResource : IEmailResource
    {
        private readonly string SendGridKey;
        private readonly string WebSiteNewsLetterSendGridKey;
        private readonly string ForcedEmail;
        private readonly bool IsDevelopment;
        public readonly List<String> EmailErrorList;

        public EmailResource(IConfigurationRoot configuration, bool isDevelopment)
        {
            SendGridKey = configuration.GetSection("Email:SendGridKey").Get<string>();
            WebSiteNewsLetterSendGridKey = configuration.GetSection("Email:WebSiteNewsLetterSendGridKey").Get<string>();
            ForcedEmail = configuration.GetSection("Email:ForcedEmail").Get<string>();
            EmailErrorList = configuration.GetSection("Email:Error").Get<List<string>>();
            IsDevelopment = isDevelopment;
        }

        public async Task SendAsync(IEnumerable<string> to, string subject, string body, bool bodyIsHtml = true, string from = "noreply@auctus.org",
            IEnumerable<string> cc = null, IEnumerable<string> bcc = null, IEnumerable<SendGrid.Helpers.Mail.Attachment> attachment = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentNullException("subject");

            if (to == null || !to.Any())
                throw new ArgumentNullException("to");

            if (IsDevelopment && string.IsNullOrWhiteSpace(ForcedEmail))
                return;

            List<EmailAddress> toList = new List<EmailAddress>();
            if (!string.IsNullOrWhiteSpace(ForcedEmail))
                toList.Add(new EmailAddress(ForcedEmail));
            else
            {
                foreach (string t in to)
                    toList.Add(new EmailAddress(t));
            }

            var mailMessage = MailHelper.CreateSingleEmailToMultipleRecipients(new EmailAddress(from, "Auctus Mail Service"), toList, subject, bodyIsHtml ? null : body, bodyIsHtml ? body : null);

            if (attachment != null)
            {
                foreach (SendGrid.Helpers.Mail.Attachment a in attachment)
                    mailMessage.Attachments.Add(a);
            }

            if (cc != null && string.IsNullOrWhiteSpace(ForcedEmail))
            {
                foreach (string c in cc)
                    mailMessage.AddCc(c);
            }

            if (bcc != null && string.IsNullOrWhiteSpace(ForcedEmail))
            {
                foreach (string b in bcc)
                    mailMessage.AddBcc(b);
            }

            SendGridClient client = new SendGridClient(SendGridKey);
            await client.SendEmailAsync(mailMessage);
        }
        public async Task IncludeSubscribedEmailFromWebsite(string email, string firstName, string lastName)
        {
            var apiKey = WebSiteNewsLetterSendGridKey;
            var client = new SendGridClient(apiKey);

            var body = @"
            [{
                'email': '{email}',
                'first_name': '{firstName}',
                'last_name': '{lastName}' 
            }]".Replace("{email}", email).Replace("{firstName}", firstName).Replace("{lastName}", lastName);

            var json = JsonConvert.DeserializeObject<Object>(body);

            var response = await client.RequestAsync(SendGridClient.Method.POST, json.ToString(), null, "contactdb/recipients");
        }
    }
}
