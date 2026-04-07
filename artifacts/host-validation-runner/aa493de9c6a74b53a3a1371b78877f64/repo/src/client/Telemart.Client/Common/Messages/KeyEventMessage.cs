using System;
using System.Windows.Input;

namespace Telemart.Client.Common.Messages
{
    public sealed class KeyEventMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyEventMessage"/> class.
        /// </summary>
        /// <param name="args">The key gesture.</param>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null" />.</exception>
        public KeyEventMessage(KeyEventArgs args)
        {
            Args = args ?? throw new ArgumentNullException(nameof(args));
        }

        public KeyEventArgs Args { get; }
    }
}
