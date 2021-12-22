using ExternalLogic.Models;
using Microsoft.Extensions.Options;
using ProtoBuf;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace ExternalLogic
{
    public class EmailingService : IEmailingService
    {
        private readonly IConnection _rmq;
        private readonly IConnectionMultiplexer _redis;
        private readonly EmailingOptions _opts;

        public EmailingService(IConnection rabbitmq, IConnectionMultiplexer redis, IOptions<EmailingOptions>? options = null)
        {
            _rmq = rabbitmq;
            _redis = redis;

            _opts = options?.Value ?? _defaultOptions;
        }

        public string EnqueueEmailing(string[] emails, string subject, string body)
        {
            IModel channel = _rmq.CreateModel();

            channel.ExchangeDeclare(_opts.ExchangeName, _opts.ExchangeType, _opts.ExchangeAutoDelete);

            string sessionId = GenerateSessionID();
            int emailsCount = 0;

            foreach (string email in emails)
            {
                if (!IsEmail(email)) continue;
                emailsCount++;

                var message = new EmailModel
                {
                    Body = body,
                    Email = email,
                    Subject = subject
                };

                byte[] bytes = SerializeMessage(message);

                channel.BasicPublish(_opts.ExchangeName, GenerateRoutingKey(sessionId), body: bytes);
            }

            IDatabase db = _redis.GetDatabase();
            string redisKey = GenerateRedisKey(sessionId);

            db.HashSet(redisKey, new[]
            {
                new HashEntry(CacheKeys.Total, emails.Length),
                new HashEntry(CacheKeys.Processed, 0),
                new HashEntry(CacheKeys.Failed, 0),
                new HashEntry(CacheKeys.NotEmail, emails.Length - emailsCount)
            });
            db.KeyExpire(redisKey, TimeSpan.FromSeconds(_opts.CacheTTL));

            channel.Close();

            return redisKey;
        }

        private IDatabase? _database = null;
        private IDatabase _db => _database ??= _redis.GetDatabase();

        public string StartMessageHandler(bool useDeadLettering = false)
        {
            IDatabase db = _redis.GetDatabase();
            IModel channel = _rmq.CreateModel();

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            channel.ExchangeDeclare(_opts.ExchangeName, _opts.ExchangeType, _opts.ExchangeAutoDelete);
            Dictionary<string, object>? queueArgs = null;

            if (useDeadLettering)
            {
                queueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", _opts.DeadLetterExchange ?? string.Empty },
                    { "x-dead-letter-routing-key", _opts.DeadLetterRoutingKey ?? string.Empty },
                    { "x-message-ttl", _opts.DeadLetterTTL }
                };
            }
            string queueName = channel.QueueDeclare(arguments: queueArgs).QueueName;
            channel.QueueBind(queueName, _opts.ExchangeName, _opts.ExchangeRoutingKey);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender, ea) =>
            {
                EmailModel? model = DeserializeMessage(ea.Body);
                if (model == null) return;

                ProcessMessageResult res = ProcessMessage(model);
                if (res.IsSuccess)
                {
                    channel.BasicAck(ea.DeliveryTag, multiple: false);

                    _increaseCounter(ea.RoutingKey, CacheKeys.Processed);
                }
                else
                {
                    channel.BasicReject(ea.DeliveryTag, requeue: res.Reprocess);

                    if (res.Reprocess)
                    {
                        _updateCacheTTL(ea.RoutingKey);
                    }
                    else
                    {
                        _increaseCounter(ea.RoutingKey, CacheKeys.Failed);
                    }
                }
            };
            consumer.ConsumerCancelled += (sender, ea) =>
            {
                channel.Close();
            };

            string consumerTag = channel.BasicConsume(queueName, autoAck: false, consumer);

            return consumerTag;
        }

        public void StopHandler(string consumerTag)
        {
            IModel channel = _rmq.CreateModel();
            channel.BasicCancel(consumerTag);
            channel.Close();
        }

        public string StartDeadLetterHandler()
        {
            var channel = _rmq.CreateModel();

            channel.ExchangeDeclare(_opts.DeadLetterExchange, _opts.DeadLetterExchangeType, _opts.DeadLetterDurable, _opts.DeadLetterAutoDelete);

            string badmsgQueueName = channel.QueueDeclare(durable: _opts.DeadLetterDurable, autoDelete: _opts.DeadLetterAutoDelete).QueueName;
            channel.QueueBind(badmsgQueueName, _opts.DeadLetterExchange, _opts.DeadLetterRoutingKey);

            var badmsgConsumer = new EventingBasicConsumer(channel);
            badmsgConsumer.Received += (sender, ea) =>
            {
                EmailModel? model = DeserializeMessage(ea.Body);
                if (model == null) return;

                ProcessDeadLetter(model, ea.BasicProperties);
            };
            badmsgConsumer.ConsumerCancelled += (sender, ea) =>
            {
                channel.Close();
            };

            string consumerTag = channel.BasicConsume(badmsgQueueName, autoAck: true, badmsgConsumer);

            return consumerTag;
        }

        public ProgressModel GetProgress(string id)
        {
            var db = _redis.GetDatabase();

            if (!db.HashExists(id, "total")) return new ProgressModel();

            HashEntry[] res = db.HashGetAll(id);

            int total = 0, processed = 0, failed = 0;

            foreach (HashEntry e in res)
            {
                switch (e.Name)
                {
                    case CacheKeys.Total:
                        total = int.Parse(e.Value.ToString());
                        break;
                    case CacheKeys.Processed:
                        processed = int.Parse(e.Value.ToString());
                        break;
                    case CacheKeys.Failed:
                        failed = int.Parse(e.Value.ToString());
                        break;
                }
            }

            return new ProgressModel
            {
                Total = total,
                Failed = failed,
                Processed = processed
            };
        }

        protected static string GenerateSessionID() => DateTime.Now.ToString("ddMMyyyyhhmmssfff");
        protected static bool IsEmail(string email) => email.Contains('@');
        protected static string GenerateRoutingKey(string sessionID) => $"demo.{sessionID}";
        protected static string GenerateRedisKey(string sessionID) => $"demo.{sessionID}";
        protected static byte[] SerializeMessage(EmailModel message)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, message);
            return stream.ToArray();
        }
        protected static EmailModel DeserializeMessage(ReadOnlyMemory<byte> bytes) => Serializer.Deserialize<EmailModel>(bytes);
        protected virtual ProcessMessageResult ProcessMessage(EmailModel model)
        {
            Thread.Sleep(1000);

            return ProcessMessageResult.Ok();
        }
        protected virtual void ProcessDeadLetter(EmailModel model, IBasicProperties properties) { }

        private static EmailingOptions _defaultOptions => new()
        {
            ExchangeName = "emailing_exchange",
            ExchangeType = "topic",
            ExchangeAutoDelete = true,
            ExchangeRoutingKey = "demo.*",
            DeadLetterExchange = "emailing_badmsg_exchange",
            DeadLetterExchangeType = "fanout",
            DeadLetterRoutingKey = string.Empty,
            DeadLetterAutoDelete = false,
            DeadLetterDurable = true,
            DeadLetterTTL = 30000,
            CacheTTL = 60
        };
        private void _increaseCounter(string routingKey, string key)
        {
            if (_db.HashExists(routingKey, key))
            {
                _db.HashIncrement(routingKey, key);
                _db.KeyExpire(routingKey, TimeSpan.FromSeconds(_opts.CacheTTL));
            }
        }
        private void _updateCacheTTL(string routingKey)
        {
            if (_db.HashExists(routingKey, CacheKeys.Total))
            {
                _db.KeyExpire(routingKey, TimeSpan.FromSeconds(_opts.CacheTTL));
            }
        }
    }
}