using ExternalLogic.Models;

namespace ExternalLogic
{
    public interface IEmailingService
    {
        string EnqueueEmailing(string[] emails, string subject, string body);
        string StartMessageHandler(bool useDeadLettering = false);
        void StopHandler(string handlerID);
        string StartDeadLetterHandler();
        ProgressModel GetProgress(string id);
    }
}
