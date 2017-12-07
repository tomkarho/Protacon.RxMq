using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus
{
    public class AzureQueueManagement
    {
        private readonly IOptions<MqSettings> _settings;

        public AzureQueueManagement(IOptions<MqSettings> settings)
        {
            _settings = settings;
        }

        public void Create(string queuName)
        {
            var subscriptionId = "8074f004-3a2e-4cac-b516-56500f988628";

            var azureCredentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    clientId: "58470d53-30f0-4561-9643-f919266071fe", 
                    clientSecret: "06a7f5d3-38a1-4e87-9b71-fc5a058da294", 
                    tenantId: "7728b60e-9961-4c7d-b6dd-8b5b8a2f887a",
                    environment: AzureEnvironment.AzureGlobalCloud);
                
            var serviceBusManager = ServiceBusManager.Authenticate(azureCredentials, subscriptionId);

            // namespace manager
            var @namespace = serviceBusManager.Namespaces.GetByResourceGroup("rxmq-tests", "rxmq-test");

            // check if queue exists
            var queue = @namespace.Queues.List().FirstOrDefault(x => x.Name == queuName);
            if (queue == null)
            {
                // create a queue
                var createdQueue = @namespace.Queues.Define(queuName)
                    .WithSizeInMB(1024)
                    .WithMessageMovedToDeadLetterQueueOnMaxDeliveryCount(10)
                    .WithMessageLockDurationInSeconds(30)
                    .Create();
            }
        }
    }
}
