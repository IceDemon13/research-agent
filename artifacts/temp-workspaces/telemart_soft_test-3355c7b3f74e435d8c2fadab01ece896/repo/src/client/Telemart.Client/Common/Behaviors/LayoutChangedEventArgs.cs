using System;
using System.Collections.Generic;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class LayoutChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutChangedEventArgs"/> class.
        /// </summary>
        /// <param name="layoutChangedTypes">The layout changed types.</param>
        /// <exception cref="ArgumentNullException"><paramref name="layoutChangedTypes"/> is <see langword="null" />.</exception>
        public LayoutChangedEventArgs(IEnumerable<LayoutChangedType> layoutChangedTypes)
        {
            if (layoutChangedTypes == null)
            {
                throw new ArgumentNullException(nameof(layoutChangedTypes));
            }

            LayoutChangedTypes = new List<LayoutChangedType>(layoutChangedTypes);
        }

        public IReadOnlyCollection<LayoutChangedType> LayoutChangedTypes { get; }
    }
}