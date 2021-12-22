namespace ExternalLogic.Models
{
    public class ProcessMessageResult
    {
        public ProcessMessageResult(bool isSuccessfullyProcessed, bool isNeedToBeReprocessed)
        {
            IsSuccess = isSuccessfullyProcessed;
            Reprocess = isNeedToBeReprocessed;
        }

        public static ProcessMessageResult Ok()
        {
            return new ProcessMessageResult(true, false);
        }

        public bool IsSuccess { get; private set; }
        public bool Reprocess { get; private set; }
    }
}
