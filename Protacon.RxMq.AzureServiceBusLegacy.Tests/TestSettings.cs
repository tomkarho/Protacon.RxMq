namespace Protacon.RxMq.AzureServiceBusLegacy.Tests
{
    public static class TestSettings
    {
        public static MqSettings MqSettings => new MqSettings
        {
            ConnectionString = "Endpoint=sb://rxmq-test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=3h/VM8lOzs3D5M5qvz8NMlAwDPk4wqeLOF6IGX9GD8U="
        };
    }
}
