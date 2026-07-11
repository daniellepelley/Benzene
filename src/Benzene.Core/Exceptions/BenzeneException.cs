using System;

namespace Benzene.Core.Exceptions
{
    /// <summary>
    /// Represents errors that occur within the Benzene framework.
    /// </summary>
    public class BenzeneException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BenzeneException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BenzeneException(string message)
            : base(message)
        { }
    }
}
