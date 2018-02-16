using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests
{
    public static class TestSettings
    {
        public static MqSettings MqSettings()
        {
            var  secretFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client-secrets.json");
            JObject secretFileContent = new JObject();

            if(File.Exists(secretFile))
            {
                secretFileContent = JObject.Parse(File.ReadAllText(secretFile));
            }

            return new MqSettings
            {
                ConnectionString = secretFileContent["ConnectionString"]?.ToString()
                    ?? Environment.GetEnvironmentVariable("ConnectionString") ??
                    throw new InvalidOperationException()
            };
        }
    }
}
