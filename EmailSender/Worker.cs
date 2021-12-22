using ExternalLogic;

namespace EmailSender
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEmailingService _emailingService;

        public Worker(ILogger<Worker> logger, IEmailingService emailingService)
        {
            _logger = logger;
            _emailingService = emailingService;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email sender worker is started");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email sender worker is stopped");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string id = _emailingService.StartMessageHandler(useDeadLettering: true);
            string dlID = _emailingService.StartDeadLetterHandler();

            _logger.LogInformation("Email sender worker is waiting for messages..");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);
            }

            _logger.LogInformation("Email sender worker received cancellation request. Stopping..");

            _emailingService.StopHandler(id);
            _emailingService.StopHandler(dlID);
        }
    }
}