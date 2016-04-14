﻿
namespace TBag.BloomFilters.Standard
{
    /// <summary>
    /// A standard Bloom filter.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="THash"></typeparam>
   internal interface IBloomFilter<TKey,THash>
        where TKey : struct
        where THash : struct
    {
    }
}