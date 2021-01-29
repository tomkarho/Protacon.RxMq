using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests
{
    public class LoggingConfigurationUnitTests : IDisposable
    {
        private const string ConfigFileName = "logging-configuration.json";

        public void Dispose()
        {
            try
            {
                var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
                FileInfo fileInfo = new FileInfo(fileName);
                fileInfo.Delete();
            }
            catch
            {
            }
        }

        [Fact]
        public void WhenInitializedWithoutConfiguration_ThenLoggingFilterIsEmpty()
        {
            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(0);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(0);
        }

        [Fact]
        public void WhenInitializedWithEmptyConfiguration_ThenLoggingFilterIsEmpty()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (File.Create(Path.Combine(fileName)))
            {
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(0);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(0);
        }

        [Fact]
        public void WhenInitializedWithInvalidConfiguration_ThenLoggingFilterIsEmpty()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (FileStream fs = File.Create(Path.Combine(fileName)))
            {
                var content = new UTF8Encoding(true).GetBytes("{\"NoExcludeKeyHere\": {\"NoQueuesKeyHere\": \"invalid\", \"NoTopicsKeyHere\": \"invalid\"}}");
                fs.Write(content, 0, content.Length);
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(0);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(0);
        }

        [Fact]
        public void WhenInitializedWithCorrentKeyConfiguration_WithoutDataKeys_ThenLoggingFilterIsEmpty()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (FileStream fs = File.Create(Path.Combine(fileName)))
            {
                var content = new UTF8Encoding(true).GetBytes("{\"Exclude\": {}}");
                fs.Write(content, 0, content.Length);
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(0);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(0);
        }

        [Fact]
        public void WhenInitializedWithCorrentKeyConfiguration_WithInvalidData_ThenLoggingFilterIsEmpty()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (FileStream fs = File.Create(Path.Combine(fileName)))
            {
                var content = new UTF8Encoding(true).GetBytes("{\"Exclude\": {\"Queues\": \"invalid\", \"Topics\": \"invalid\"}}");
                fs.Write(content, 0, content.Length);
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(0);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(0);
        }

        [Fact]
        public void WhenInitializedWithCorrentKeyConfiguration_WithEmptyData_ThenLoggingFilterIsEmpty()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (FileStream fs = File.Create(Path.Combine(fileName)))
            {
                var content = new UTF8Encoding(true).GetBytes("{\"Exclude\": {\"Queues\": [], \"Topics\": []}}");
                fs.Write(content, 0, content.Length);
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(0);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(0);
        }

        [Fact]
        public void WhenInitializedWithCorrentKeyConfiguration_WithEmptyValues_ThenLoggingFilterIsEmpty()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (FileStream fs = File.Create(Path.Combine(fileName)))
            {
                var content = new UTF8Encoding(true).GetBytes("{\"Exclude\": {\"Queues\": [\"\",\"\"], \"Topics\": [\"\",\"\"]}}");
                fs.Write(content, 0, content.Length);
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(0);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEmpty();
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(0);
        }

        [Fact]
        public void WhenInitializedWithCorrentKeyConfiguration_WithValues_ThenLoggingFilterIsSet()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (FileStream fs = File.Create(Path.Combine(fileName)))
            {
                var content = new UTF8Encoding(true).GetBytes("{\"Exclude\": {\"Queues\": [\"testqueue\",\"anothertestqueue\"], \"Topics\": [\"testtopic\",\"anothertesttopic\"]}}");
                fs.Write(content, 0, content.Length);
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEquivalentTo("testqueue", "anothertestqueue");
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(2);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEquivalentTo("testtopic", "anothertesttopic");
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(2);
        }

        [Fact]
        public void WhenInitializedWithCorrentKeyConfiguration_WithMixedValues_ThenLoggingFilterIsSet()
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            using (FileStream fs = File.Create(Path.Combine(fileName)))
            {
                var content = new UTF8Encoding(true).GetBytes("{\"Exclude\": {\"Invalid\": \"Values\",\"Queues\": [\"\", \"testqueue\", \"       \", \"anothertestqueue\"], \"Topics\": [\"\", \"testtopic\", \"       \", \"anothertesttopic\"]}}");
                fs.Write(content, 0, content.Length);
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.ExcludeQueuesFromLogging().Should().BeEquivalentTo("testqueue", "anothertestqueue");
            loggingConfiguration.ExcludeQueuesFromLogging().Count.Should().Be(2);
            loggingConfiguration.ExcludeTopicsFromLogging().Should().BeEquivalentTo("testtopic", "anothertesttopic");
            loggingConfiguration.ExcludeTopicsFromLogging().Count.Should().Be(2);
        }
    }
}
