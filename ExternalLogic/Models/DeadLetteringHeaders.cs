namespace ExternalLogic.Models
{
    public record DeadLetteringHeaders(string? FirstDeathExchange, string? FirstDeathQueue, string? FirstDeathReason, List<DeadLetteringHeader> Deaths);
    public record DeadLetteringHeader(long Count, string Reason, string Queue, DateTime? Time, string Exchange, string[] RoutingKeys);
}
