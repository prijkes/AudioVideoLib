/*
 * Date: 2011-04-02
 */

using System;

namespace AudioVideoLib
{
    /// <summary>
    /// The exception that is thrown when an invalid version is passed to a class constructor or function parameter.
    /// </summary>
    public class InvalidVersionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioVideoLib.InvalidVersionException"/> class.
        /// </summary>
        public InvalidVersionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioVideoLib.InvalidVersionException"/> class with a specified error.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public InvalidVersionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioVideoLib.InvalidVersionException"/> class with a specified error message and the exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public InvalidVersionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
