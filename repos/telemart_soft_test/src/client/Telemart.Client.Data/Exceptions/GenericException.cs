using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Telemart.Client.Core.Exceptions
{
    [Serializable]
    public class GenericException<TExceptionArgs> : Exception, ISerializable
        where TExceptionArgs : ExceptionArgs
    {
        private const string ArgsPropertyName = "Args";

        private readonly TExceptionArgs args;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericException{TExceptionArgs}"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public GenericException(string message = null, Exception innerException = null)
            : this(null, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericException{TExceptionArgs}"/> class.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public GenericException(TExceptionArgs args, string message = null, Exception innerException = null)
            : base(message, innerException)
        {
            this.args = args;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericException{TExceptionArgs}"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        private GenericException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            args = (TExceptionArgs)info.GetValue(ArgsPropertyName, typeof(TExceptionArgs));
        }

        /// <summary>
        /// Gets the arguments.
        /// </summary>
        /// <value>
        /// The arguments.
        /// </value>
        public TExceptionArgs Args => args;

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <returns>
        /// The error message that explains the reason for the exception, or an empty string("").
        /// </returns>
        public override string Message
        {
            get
            {
                string baseMessage = base.Message;
                return args == null ? baseMessage : $"{baseMessage} ({args.Message})";
            }
        }

        public static bool operator !=(GenericException<TExceptionArgs> left, GenericException<TExceptionArgs> right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(GenericException<TExceptionArgs> left, GenericException<TExceptionArgs> right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            GenericException<TExceptionArgs> other = obj as GenericException<TExceptionArgs>;

            if (other == null)
            {
                return false;
            }

            return Equals(other);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return EqualityComparer<TExceptionArgs>.Default.GetHashCode(args);
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter" />
        /// </PermissionSet>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(ArgsPropertyName, args);
            base.GetObjectData(info, context);
        }

        private bool Equals(GenericException<TExceptionArgs> other)
        {
            return EqualityComparer<TExceptionArgs>.Default.Equals(args, other.args);
        }
    }
}