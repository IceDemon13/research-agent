using System;
using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Parser
{
    public sealed class ComparsionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComparsionResult"/> class.
        /// </summary>
        /// <param name="proposedValues">The proposed values.</param>
        /// <param name="guaranteedValue">The guaranteed value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="proposedValues"/> is <see langword="null" />.</exception>
        public ComparsionResult(
            IReadOnlyCollection<ProductComparsionDto> proposedValues,
            ProductComparsionDto guaranteedValue = null)
        {
            if (proposedValues == null)
            {
                throw new ArgumentNullException(nameof(proposedValues));
            }

            ProposedValues = proposedValues;
            GuaranteedValue = guaranteedValue;
        }

        public IReadOnlyCollection<ProductComparsionDto> ProposedValues { get; }

        public ProductComparsionDto GuaranteedValue { get; }

        public int? ProposedValuesCount => ProposedValues.Count;
    }
}