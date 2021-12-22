using ExternalLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQDemo.Models;

namespace RabbitMQDemo.Pages
{
    public class SupportAnswerModel : PageModel
    {
        private readonly ISupportService _support;
        public SupportAnswerModel(ISupportService support)
        {
            _support = support;
        }

        public string? Message { get; set; }
        public Models.QuestionModel? Question { get; set; }

        public void OnGet()
        {
            string key;
            if (Request.Cookies.ContainsKey("support-session") && !string.IsNullOrEmpty(Request.Cookies["support-session"]))
            {
                key = Request.Cookies["support-session"]!;

                _support.Connect(key);
            }
            else
            {
                key = _support.Connect();
                Response.Cookies.Append("support-session", key, new CookieOptions { HttpOnly = true, IsEssential = true });
            }

            ExternalLogic.Models.QuestionModel? question = _support.GetNextQuestion(key, out ulong deliveryTag);

            if (question == null)
            {
                Message = "There is no new questions! Great job!";
                return;
            }

            Question = new Models.QuestionModel(question.Email, question.Text, deliveryTag);
        }

        [BindProperty]
        public AnswerModel? Answer { get; set; }

        public IActionResult OnPost()
        {
            if(Answer == null || string.IsNullOrEmpty(Answer.Text) || Answer.Id == 0) return RedirectToPage("SupportAnswer");
            string? key = Request.Cookies["support-session"];
            if(string.IsNullOrEmpty(key)) return RedirectToPage("SupportAnswer");
            
            //Send email with answer..

            _support.MarkAsAnswered(key, Answer.Id);

            return RedirectToPage("SupportAnswer");
        }
    }
}
