using MassTransit;
using PersonalLedger.Infrastructure.Messaging.Contracts;

namespace PersonalLedger.Infrastructure.Messaging
{
    public class CustomEndpointNameFormatter : DefaultEndpointNameFormatter
    {
        public override string Consumer<T>()
        {
            var consumerName = typeof(T).Name.Replace("Consumer", "");
            var formattedName = KebabCaseEndpointNameFormatter.Instance.SanitizeName(consumerName);

            if (typeof(IPluggyConsumer).IsAssignableFrom(typeof(T)))
                return $"pluggy-{formattedName}";

            return formattedName;
        }
    }
}
