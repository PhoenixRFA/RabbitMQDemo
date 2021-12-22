using ExternalLogic.Models;
using Microsoft.Extensions.Options;
using ProtoBuf;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace ExternalLogic
{
    public class SupportService : ISupportService
    {
        private static readonly ConcurrentDictionary<string, IModel> Connections = new();

        private readonly IConnection _rmq;
        private readonly IConnectionMultiplexer _redis;
        private readonly SupportOptions _opts;

        public SupportService(IConnection rmq, IConnectionMultiplexer redis, IOptions<SupportOptions>? options = null)
        {
            _rmq = rmq;
            _redis = redis;
            _opts = options?.Value ?? _defaultOptions;
        }

        public void AskQuestion(string email, string text)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(text)) return;

            IModel channel = _rmq.CreateModel();
            channel.QueueDeclare(_opts.QueueName, durable: true, exclusive: false, autoDelete: false);

            IBasicProperties props = channel.CreateBasicProperties();
            props.Persistent = true;

            var question = new QuestionModel(email, text);

            using var s = new MemoryStream();
            Serializer.Serialize(s, question);
            byte[] bytes = s.ToArray();

            channel.BasicPublish("", _opts.QueueName, props, bytes);
        }

        public string Connect()
        {
            string g = Guid.NewGuid().ToString();

            _addConnection(g, _rmq.CreateModel());

            _clearOldConnections();

            return g;
        }

        public void Connect(string key)
        {
            if (Connections.ContainsKey(key)) return;

            _addConnection(key, _rmq.CreateModel());

            _clearOldConnections();
        }

        public QuestionModel? GetNextQuestion(string key, out ulong deliveryTag)
        {
            deliveryTag = 0;
            if (!Connections.ContainsKey(key)) return null;
            IModel? channel = _getConnection(key);
            if (channel == null) return null;

            channel.QueueDeclare(_opts.QueueName, durable: true, exclusive: false, autoDelete: false);

            BasicGetResult? message = channel.BasicGet(_opts.QueueName, autoAck: false);

            if (message == null) return null;

            QuestionModel res = Serializer.Deserialize<QuestionModel>(message.Body);
            deliveryTag = message.DeliveryTag;

            return res;
        }

        public void MarkAsAnswered(string key, ulong deliveryTag)
        {
            if (!Connections.ContainsKey(key)) return;
            IModel? channel = _getConnection(key);
            if (channel == null) return;

            channel.BasicAck(deliveryTag, multiple: false);
        }

        public bool IsConnectionAlive(string key)
        {
            IDatabase db = _redis.GetDatabase();

            return db.KeyExists(key);
        }

        public void DiscardMessage(string key, ulong deliveryTag)
        {
            if (!Connections.ContainsKey(key)) return;
            IModel? channel = _getConnection(key);
            if (channel == null) return;

            channel.BasicNack(deliveryTag, multiple: false, requeue: true);
        }

        private static SupportOptions _defaultOptions => new()
        {
            QueueName = "questions_queue",
            SessionTTL = TimeSpan.FromMinutes(5)
        };

        private IModel? _getConnection(string key)
        {
            Connections.TryGetValue(key, out IModel? connection);

            IDatabase db = _redis.GetDatabase();
            db.KeyExpire(key, _opts.SessionTTL);

            return connection;
        }
        private void _addConnection(string key, IModel channel)
        {
            Connections.TryAdd(key, channel);

            IDatabase db = _redis.GetDatabase();
            db.StringSet(key, 1, expiry: _opts.SessionTTL);
        }
        private void _clearOldConnections()
        {
            IDatabase db = _redis.GetDatabase();

            foreach (string key in Connections.Keys)
            {
                if (db.KeyExists(key)) continue;

                Connections.TryRemove(key, out _);
            }
        }
    }
}
