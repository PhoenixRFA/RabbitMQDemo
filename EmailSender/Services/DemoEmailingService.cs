using ExternalLogic;
using ExternalLogic.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace EmailSender.Services
{
    public class DemoEmailingService : EmailingService
    {
        private readonly ILogger<DemoEmailingService> _logger;
        private readonly Random _rnd;

        public DemoEmailingService(IConnection rabbitmq, IConnectionMultiplexer redis, ILogger<DemoEmailingService> logger, IOptions<EmailingOptions> options)
            : base(rabbitmq, redis, options)
        {
            _logger = logger;
            _rnd = new Random();
        }

        protected override ProcessMessageResult ProcessMessage(EmailModel model)
        {
            Thread.Sleep(1000);

            _logger.LogInformation("Email {subject} sent to {email} successfully", model.Subject, model.Email);

            bool result = _rnd.Next(0, 100) > 50;

            return new ProcessMessageResult(isSuccessfullyProcessed: result, isNeedToBeReprocessed: false);
        }

        protected override void ProcessDeadLetter(EmailModel model, IBasicProperties properties)
        {
            DeadLetteringHeaders? headers = properties.GetDeadLetteringHeaders();
            if (headers == null)
            {
                _logger.LogInformation("Failed message processed");
                return;
            }

            _logger.LogInformation("Failed message: {reason}. From: (E){exchange}:{queue}(q)", headers.FirstDeathReason, headers.FirstDeathExchange, headers.FirstDeathQueue);
            int count = 0;
            foreach (DeadLetteringHeader h in headers.Deaths)
            {
                _logger.LogInformation("\t[{i}] ({date}) Reason: {reason} (x{times}). From: (E){exchange}:{queue}(q). RoutingKeys: {routingKeys}",
                    ++count, h.Time, h.Reason, h.Count, h.Exchange, h.Queue, string.Join(", ", h.RoutingKeys));
            }
        }
    }
}
