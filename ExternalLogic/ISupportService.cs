using ExternalLogic.Models;

namespace ExternalLogic
{
    public interface ISupportService
    {
        void AskQuestion(string email, string text);
        string Connect();
        void Connect(string key);
        QuestionModel? GetNextQuestion(string key, out ulong deliveryTag);
        void MarkAsAnswered(string key, ulong deliveryTag);
        void DiscardMessage(string key, ulong deliveryTag);
        bool IsConnectionAlive(string key);
    }
}
