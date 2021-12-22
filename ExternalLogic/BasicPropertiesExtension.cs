using ExternalLogic.Models;
using RabbitMQ.Client;
using System.Text;

namespace ExternalLogic
{
    public static class BasicPropertiesExtension
    {
        /// <summary> Dead Lettering Headers </summary>
        private static class DLH
        {
            public const string Deaths = "x-death";
            public const string FirstDeathExchange = "x-first-death-exchange";
            public const string FirstDeathQueue = "x-first-death-queue";
            public const string FirstDeathReason = "x-first-death-reason";
        }

        public static DeadLetteringHeaders? GetDeadLetteringHeaders(this IBasicProperties properties)
        {
            IDictionary<string, object>? headers = properties.Headers;

            if (headers == null || !headers.ContainsKey(DLH.Deaths)) return null;

            if (headers[DLH.Deaths] is not List<object> deaths) return null;

            var res = new DeadLetteringHeaders
            (
                FirstDeathExchange: _getString(headers[DLH.FirstDeathExchange]),
                FirstDeathQueue: _getString(headers[DLH.FirstDeathQueue]),
                FirstDeathReason: _getString(headers[DLH.FirstDeathReason]),
                Deaths: new List<DeadLetteringHeader>(deaths.Count)
            );

            foreach (Dictionary<string, object> item in deaths)
            {
                var header = new DeadLetteringHeader
                (
                    Count: _getValueIfExist(item, "count", 0L),
                    Reason: _getString(_getValueIfExist(item, "reason", Array.Empty<byte>())) ?? string.Empty,
                    Queue: _getString(_getValueIfExist(item, "queue", Array.Empty<byte>())) ?? string.Empty,
                    Time: _getTime(item),
                    Exchange: _getString(_getValueIfExist(item, "exchange", Array.Empty<byte>())) ?? string.Empty,
                    RoutingKeys: _getArray(item)
                );

                res.Deaths.Add(header);
            }

            return res;
        }

        private static string? _getString(object obj)
        {
            if (obj is not byte[] bytes) return null;

            return Encoding.UTF8.GetString(bytes);
        }

        private static T _getValueIfExist<T>(Dictionary<string, object> dict, string key, T defValue)
        {
            object? item = dict.ContainsKey(key) ? dict[key] : defValue;

            if (item is not T res) return defValue;

            return res;
        }
        private static DateTime? _getTime(Dictionary<string, object> dict, string key = "time")
        {
            if (!dict.ContainsKey(key)) return null;

            if (dict[key] is not AmqpTimestamp time) return null;

            var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(time.UnixTime);
            return date;
        }
        private static string[] _getArray(Dictionary<string, object> dict, string key = "routing-keys")
        {
            var res = Array.Empty<string>();
            if (!dict.ContainsKey(key)) return res;

            if (dict[key] is not List<object> list) return res;

            res = list.Select(x => x as byte[])
                .Where(x => x != null)
                .Select(x => Encoding.UTF8.GetString(x!))
                .ToArray();

            return res;
        }
    }
}
