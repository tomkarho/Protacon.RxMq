using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy
{
    public class LoggingConfiguration : ILoggingConfiguration
    {
        private IList<string> _excludeQueuesFromLogging;
        private IList<string> _excludeTopicsFromLogging;

        private IList<string> ReadConfiguration(string configuration)
        {
            var queueConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logging-configuration.json");

            if (File.Exists(queueConfigFile))
            {
                string content = File.ReadAllText(queueConfigFile);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<string>();
                }

                try
                {
                    JObject queueConfigFileContent = JObject.Parse(content);
                    JArray queues = (JArray) queueConfigFileContent["Exclude"][configuration];

                    return queues?.Values<string>()?.Where(queue => !string.IsNullOrWhiteSpace(queue))?.ToList();
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public IList<string> ExcludeQueuesFromLogging()
        {
            if (_excludeQueuesFromLogging != null)
            {
                return _excludeQueuesFromLogging;
            }

            var queueConfiguration = ReadConfiguration("Queues");
            if (queueConfiguration != null)
            {
                _excludeQueuesFromLogging = queueConfiguration;
                return _excludeQueuesFromLogging;
            }
            else
            {
                _excludeQueuesFromLogging = new List<string>();
                return _excludeQueuesFromLogging;
            }
        }

        public IList<string> ExcludeTopicsFromLogging()
        {
            if (_excludeTopicsFromLogging != null)
            {
                return _excludeTopicsFromLogging;
            }

            var topicConfiguration = ReadConfiguration("Topics");
            if (topicConfiguration != null)
            {
                _excludeTopicsFromLogging = topicConfiguration;
                return _excludeTopicsFromLogging;
            }
            else
            {
                _excludeTopicsFromLogging = new List<string>();
                return _excludeTopicsFromLogging;
            }
        }
    }
}
