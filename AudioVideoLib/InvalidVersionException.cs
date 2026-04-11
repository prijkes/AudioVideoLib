/*
 * Date: 2011-04-02
 */

using System;
using System.Runtime.Serialization;

namespace AudioVideoLib
{
    /// <summary>
    /// The exception that is thrown when an invalid version is passed to a class constructor or function parameter.
    /// </summary>
    [Serializable]
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioVideoLib.InvalidVersionException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0). </exception>
        protected InvalidVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
