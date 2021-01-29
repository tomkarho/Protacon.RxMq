using System.Collections.Generic;

namespace Protacon.RxMq.Abstractions
{
    /// <summary>
    /// Provide support to read configuration file to implement logging exclusion based on Queue or Topic.
    /// </summary>
    public interface ILoggingConfiguration
    {
        /// <summary>
        /// Get configured queue names to be excluded from logging.
        /// </summary>
        /// <returns>List of queue names</returns>
        IList<string> ExcludeQueuesFromLogging();
        /// <summary>
        /// Get configured topic names to be excluded from logging.
        /// </summary>
        /// <returns>List of topic names</returns>
        IList<string> ExcludeTopicsFromLogging();
    }
}
