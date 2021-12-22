using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RabbitMQDemo.Pages
{
    public class MessagingProgressModel : PageModel
    {
        public string? Id { get; set; }
        
        public void OnGet(string id = "0")
        {
            Id = id;
        }
    }
}
