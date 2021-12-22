using ExternalLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RabbitMQDemo.Pages
{
    public class SupportModel : PageModel
    {
        private readonly ISupportService _support;
        public SupportModel(ISupportService support)
        {
            _support = support;
        }

        public void OnGet()
        {
        }

        [BindProperty]
        public Models.QuestionModel? Question { get; set;}

        public ActionResult OnPost()
        {
            if(Question == null || string.IsNullOrEmpty(Question.Email) || string.IsNullOrEmpty(Question.Text)) return RedirectToPage("Support");

            _support.AskQuestion(Question.Email, Question.Text);

            return RedirectToPage("Support");
        }
    }
}
