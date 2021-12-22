namespace ExternalLogic.Models
{
    public class EmailingOptions
    {
        public string? ExchangeName { get; set; }
        public string? ExchangeType { get; set; }
        public bool ExchangeAutoDelete { get; set; }
        public string? ExchangeRoutingKey { get; set; }
        public string? DeadLetterExchange { get; set; }
        public string? DeadLetterExchangeType { get; set; }
        public string? DeadLetterRoutingKey { get; set; }
        public bool DeadLetterAutoDelete { get; set; }
        public bool DeadLetterDurable { get; set; }
        public int DeadLetterTTL { get; set; }
        public int CacheTTL { get; set; }
    }
}
