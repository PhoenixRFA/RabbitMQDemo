using ExternalLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQDemo.Models;

namespace RabbitMQDemo.Pages
{
    public class EmailingModel : PageModel
    {
        private readonly IEmailingService _emailingService;

        public EmailingModel(IEmailingService emailingService)
        {
            _emailingService = emailingService;
        }

        public void OnGet()
        {
        }

        [BindProperty]
        public SendEmailModel? EmailsModel { get; set; }

        public IActionResult OnPostNewEmail()
        {
            string[]? emails = EmailsModel?.Emails?.Split(',');

            if (emails == null || emails.Length == 0 || string.IsNullOrEmpty(EmailsModel?.Subject) || string.IsNullOrEmpty(EmailsModel?.Body))
            {
                return RedirectToPage("./Emailing");
            }

            string id = _emailingService.EnqueueEmailing(emails, EmailsModel.Subject, EmailsModel.Body);

            return RedirectToPage("./MessagingProgress", new { id });
        }
    }
}
