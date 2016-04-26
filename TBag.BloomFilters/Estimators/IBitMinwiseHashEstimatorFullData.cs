﻿namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for bit minwise hash estimator data.
    /// </summary>
    public interface IBitMinwiseHashEstimatorFullData
    {
        /// <summary>
        /// The number of bits.
        /// </summary>
        byte BitSize { get; set; }

        /// <summary>
        /// The capacity (number of elements).
        /// </summary>
        long Capacity { get; set; }

        /// <summary>
        /// The number of hash functions
        /// </summary>
        int HashCount { get; set; }

        /// <summary>
        /// The hashed values.
        /// </summary>
        int[] Values { get; set; }

        /// <summary>
        /// The item count.
        /// </summary>
        long ItemCount { get; set; }

        /// <summary>
        /// Create new values.
        /// </summary>
        /// <param name="initialize">When <c>true</c> initialize.</param>
        void SetValues(bool initialize = true);
    }
}