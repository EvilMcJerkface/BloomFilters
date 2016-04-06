﻿namespace TBag.BloomFilters.Invertible
{
    using Configurations;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods for invertible Bloom filter data.
    /// </summary>
    public static class InvertibleBloomFilterDataExtensions
    {
        /// <summary>
        /// <c>true</c> when the filters are compatible, else <c>false</c>
        /// </summary>
        /// <typeparam name="TId">The type of entity identifier</typeparam>
        /// <typeparam name="THash">The type of the entity hash.</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>
        /// <typeparam name="TEntity">Type of the entity</typeparam>
        /// <param name="filter">Bloom filter data</param>
        /// <param name="otherFilter">The Bloom filter data to compare against</param>
        /// <param name="configuration">THe Bloom filter configuration</param>
        /// <returns></returns>
        public static bool IsCompatibleWith<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IInvertibleBloomFilterData<TId, THash, TCount> otherFilter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TId : struct
            where THash : struct
            where TCount : struct
        {
            if (filter == null || otherFilter == null) return true;
            if (!filter.IsValid() || !otherFilter.IsValid()) return false;
            if (filter.IsReverse != otherFilter.IsReverse ||
               filter.HashFunctionCount != otherFilter.HashFunctionCount ||
                (filter.SubFilter != otherFilter.SubFilter &&
               !filter.SubFilter.IsCompatibleWith(otherFilter.SubFilter, configuration.SubFilterConfiguration)))
                return false;
            if (filter.BlockSize != otherFilter.BlockSize)
            {
                var foldFactors = configuration.FoldingStrategy?.GetFoldFactors(filter.BlockSize, otherFilter.BlockSize);
                if (foldFactors?.Item1 > 1 || foldFactors?.Item2 > 1)
                {
                    return true;
                }
            }
            return filter.BlockSize == otherFilter.BlockSize &&
                   filter.IsReverse == otherFilter.IsReverse &&
                   filter.Counts?.LongLength == otherFilter.Counts?.LongLength &&
                   filter.HashSums?.LongLength == otherFilter.HashSums?.LongLength &&
                   filter.IdSums?.LongLength == otherFilter.IdSums?.LongLength;
        }

        /// <summary>
        /// <c>true</c> when the filter is valid, else <c>false</c>.
        /// </summary>
        /// <typeparam name="TId">The type of entity identifier</typeparam>
        /// <typeparam name="TEntityHash">The type of the entity hash.</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>
        /// <param name="filter">The Bloom filter data to validate.</param>
        /// <returns></returns>
        public static bool IsValid<TId, TEntityHash, TCount>(this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter)
            where TCount : struct
            where TEntityHash : struct
            where TId : struct
        {
            if (filter == null) return false;
            if (!filter.IsReverse &&
                    (filter.Counts == null ||
                filter.IdSums == null ||
                filter.HashSums == null)) return false;
            if (filter.Counts?.LongLength != filter.HashSums?.LongLength ||
                filter.Counts?.LongLength != filter.IdSums?.LongLength ||
               filter.BlockSize != (filter.Counts?.LongLength ?? filter.BlockSize)) return false;
            return true;
        }     

        /// <summary>
        /// Try to compress the data
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="filterData">The Bloom filter data to compress.</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>The compressed data, or <c>null</c> when compression failed.</returns>
        public static IInvertibleBloomFilterData<TId, THash, TCount> Compress<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filterData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
             where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filterData == null || configuration?.FoldingStrategy == null) return null;
            var fold = configuration.FoldingStrategy.FindFoldFactor(filterData.BlockSize, filterData.Capacity, filterData.ItemCount);
            var res = fold.HasValue ? filterData.Fold(configuration, fold.Value) : null;
            if (res == null) return null;
            res.SubFilter = filterData.SubFilter.Compress(configuration).ConvertToBloomFilterData(configuration) ?? filterData.SubFilter;
            return res;
        }     

        /// <summary>
        /// Remove an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter">The filter</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="idValue">The identifier to remove</param>
        /// <param name="hashValue">The hash value to remove</param>
        /// <param name="position">The position of the cell to remove the identifier and hash from.</param>
        internal static void Remove<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            TId idValue,
            THash hashValue,
            long position)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountConfiguration.Decrease(filter.Counts[position]);
            filter.IdSums[position] = configuration.IdXor(filter.IdSums[position], idValue);
            filter.HashSums[position] = configuration.HashXor(filter.HashSums[position], hashValue);
        }

        /// <summary>
        /// Add an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter"></param>
        /// <param name="configuration"></param>
        /// <param name="idValue"></param>
        /// <param name="hashValue"></param>
        /// <param name="position"></param>
        internal static void Add<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            TId idValue,
            THash hashValue,
            long position)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountConfiguration.Increase(filter.Counts[position]);
            filter.IdSums[position] = configuration.IdXor(filter.IdSums[position], idValue);
            filter.HashSums[position] = configuration.HashXor(filter.HashSums[position], hashValue);
        }

        /// <summary>
        /// Add two filters.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filterData"></param>
        /// <param name="configuration"></param>
        /// <param name="otherFilterData"></param>
        /// <param name="inPlace">When <c>true</c> the <paramref name="otherFilterData"/> will be added to the <paramref name="filterData"/> instance, otheerwise a new instance of the filter data will be returned.</param>
        /// <returns>The filter data or <c>null</c> when the addition failed.</returns>
        ///<remarks></remarks>
        public static IInvertibleBloomFilterData<TId, THash, TCount> Add<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filterData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            IInvertibleBloomFilterData<TId, THash, TCount> otherFilterData,
            bool inPlace = true)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filterData == null && otherFilterData == null) return null;
            if (filterData == null)
            {
                filterData = otherFilterData.CreateDummy(configuration);
                inPlace = true;
            }
            if (otherFilterData == null)
            {
                otherFilterData = filterData.CreateDummy(configuration);
            }
            if (!filterData.IsCompatibleWith(otherFilterData, configuration)) return null;
            var foldFactors = configuration.FoldingStrategy?.GetFoldFactors(filterData.BlockSize, otherFilterData.BlockSize);
            var res = inPlace && foldFactors?.Item1 <= 1 ?
                filterData :
                (foldFactors == null || foldFactors.Item1 <= 1 ?
                filterData.CreateDummy(configuration) :
                configuration.DataFactory.Create<TId, THash, TCount>(
                    filterData.Capacity / foldFactors.Item1,
                    filterData.BlockSize / foldFactors.Item1,
                    filterData.HashFunctionCount));
            for (var i = 0L; i < res.BlockSize; i++)
            {
                res.Counts[i] = configuration.CountConfiguration.Add(
                    filterData.Counts.GetFolded(i, foldFactors?.Item1, configuration.CountConfiguration.Add),
                    otherFilterData.Counts.GetFolded(i, foldFactors?.Item2, configuration.CountConfiguration.Add));
                res.HashSums[i] = configuration.HashXor(
                    filterData.HashSums.GetFolded(i, foldFactors?.Item1, configuration.HashXor),
                   otherFilterData.HashSums.GetFolded(i, foldFactors?.Item2, configuration.HashXor));
                res.IdSums[i] = configuration.IdXor(
                    filterData.IdSums.GetFolded(i, foldFactors?.Item1, configuration.IdXor),
                    otherFilterData.IdSums.GetFolded(i, foldFactors?.Item2, configuration.IdXor));
            }
            res.SubFilter = filterData
                .SubFilter
                .Add(configuration.SubFilterConfiguration, otherFilterData.SubFilter, inPlace)
                .ConvertToBloomFilterData(configuration);
            res.ItemCount = filterData.ItemCount + otherFilterData.ItemCount;
            return res;
        }

        /// <summary>
        /// Fold the data by the given factor
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <param name="data"></param>
        /// <param name="configuration"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        /// <remarks>Captures the concept of reducing the size of a Bloom filter.</remarks>
        public static IInvertibleBloomFilterData<TId, THash, TCount> Fold<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> data,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            uint factor)
            where TId : struct
            where TCount : struct
            where THash : struct
        {
            if (factor <= 0)
                throw new ArgumentException($"Fold factor should be a positive number (given value was {factor}.");
            if (data == null) return null;
            if (data.BlockSize % factor != 0)
                throw new ArgumentException($"Bloom filter data cannot be folded by {factor}.", nameof(factor));
            var res = configuration.DataFactory.Create<TId, THash, TCount>(data.Capacity / factor, data.BlockSize / factor, data.HashFunctionCount);
            res.IsReverse = data.IsReverse;
            res.ItemCount = data.ItemCount;
            for (var i = 0L; i < data.Counts.LongLength; i++)
            {
                if (i < res.BlockSize)
                {
                    res.Counts[i] = data.Counts[i];
                    res.HashSums[i] = data.HashSums[i];
                    res.IdSums[i] = data.IdSums[i];
                }
                else
                {
                    var pos = i % res.BlockSize;
                    res.Counts[pos] = configuration.CountConfiguration.Add(res.Counts[pos], data.Counts[i]);
                    res.HashSums[pos] = configuration.HashXor(res.HashSums[pos], data.HashSums[i]);
                    res.IdSums[pos] = configuration.IdXor(res.IdSums[pos], data.IdSums[i]);
                }
            }
            res.SubFilter = data
                .SubFilter?
                .Fold(configuration.SubFilterConfiguration, factor)
                .ConvertToBloomFilterData(configuration);
            return res;
        }

        /// <summary>
        /// Subtract the given filter and decode for any changes
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter">Filter</param>
        /// <param name="subtractedFilter">The Bloom filter to subtract</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in <paramref name="filter"/>, but not in <paramref name="subtractedFilter"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilter"/>, but not in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">items in both filters, but with a different value.</param>
        /// <param name="destructive">Optional parameter, when <c>true</c> the filter <paramref name="filter"/> will be modified, and thus rendered useless, by the decoding.</param>
        /// <returns><c>true</c> when the decode was successful, else <c>false</c>.</returns>
        public static bool? SubtractAndDecode<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterData<TId, int, TCount> subtractedFilter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities,
            bool destructive = false)
            where TId : struct
            where TCount : struct
        {
            if (filter == null && subtractedFilter == null) return true;
            if (filter == null)
            {
                //handle null filters as elegant as possible at this point.
                filter = subtractedFilter.CreateDummy(configuration);
                destructive = true;
            }
            if (subtractedFilter == null)
            {
                //swap the filters and the sets so we can still apply the destructive setting to temporarily created filter data 
                subtractedFilter = filter;
                filter = subtractedFilter.CreateDummy(configuration);
                var swap = listA;
                listA = listB;
                listB = swap;
                destructive = true;
            }
            if (!filter.IsCompatibleWith(subtractedFilter, configuration))
                return null;
            bool? valueRes = true;
            var pureList = new Stack<long>();
            var hasSubFilter = filter.SubFilter != null || subtractedFilter.SubFilter != null;
            //add a dummy mod set when there is a reverse filter, because a regular filter is pretty bad at recognizing modified entites.
            var idRes = filter.Counts == null && filter.IsReverse ? true
                : filter.Subtract(subtractedFilter, configuration, listA, listB, pureList, destructive)
                .Decode(configuration, listA, listB, hasSubFilter ? null : modifiedEntities, pureList);
            if (hasSubFilter)
            {
                valueRes = filter
                         .SubFilter
                         .SubtractAndDecode(
                             subtractedFilter.SubFilter,
                             configuration.SubFilterConfiguration,
                             listA,
                             listB,
                             modifiedEntities,
                             destructive);
            }
            if (!valueRes.HasValue || !idRes.HasValue) return null;
            return idRes.Value && valueRes.Value;
        }

        /// <summary>
        /// Convert a <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/> to a concrete <see cref="InvertibleBloomFilterData{TId, TEntityHash, TCount}"/>.
        /// </summary>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="TEntityHash">The entity hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="filterData">The IBF data</param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal static InvertibleBloomFilterData<TId, TEntityHash, TCount> ConvertToBloomFilterData<TEntity, TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filterData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, TEntityHash, TCount> configuration)
            where TId : struct
            where TEntityHash : struct
            where TCount : struct
        {
            if (filterData == null) return null;
            var result = filterData as InvertibleBloomFilterData<TId, TEntityHash, TCount>;
            if (result != null) return result;
            return new InvertibleBloomFilterData<TId, TEntityHash, TCount>
            {
                HashFunctionCount = filterData.HashFunctionCount,
                BlockSize = filterData.BlockSize,
                Counts = filterData.Counts,
                HashSums = filterData.HashSums,
                IdSums = filterData.IdSums,
                IsReverse = filterData.IsReverse,
                SubFilter = filterData.SubFilter,
                Capacity = filterData.Capacity,
                ItemCount = filterData.ItemCount
            };
        }

        #region Private Methods
        /// <summary>
        /// Decode the filter.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence count for the invertible Bloom filter.</typeparam>
        /// <param name="filter">The Bloom filter data to decode</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in the original set, but not in the subtracted set.</param>
        /// <param name="listB">Items not in the original set, but in the subtracted set.</param>
        /// <param name="modifiedEntities">items in both sets, but with a different value.</param>
        /// <param name="pureList">Optional list of pure items</param>
        /// <returns><c>true</c> when the decode was successful, else <c>false</c>.</returns>
        private static bool? Decode<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities = null,
            Stack<long> pureList = null)
            where TId : struct
            where TCount : struct
        {
            if (filter == null) return null;
            var countComparer = Comparer<TCount>.Default;
            if (pureList == null)
            {
                pureList = new Stack<long>(LongEnumerable.Range(0L, filter.Counts.LongLength)
                    .Where(i => configuration.IsPure(filter, i))
                    .Select(i => i));
            }
            var countsIdentity = configuration.CountConfiguration.Identity();
            while (pureList.Any())
            {
                var pureIdx = pureList.Pop();
                if (!configuration.IsPure(filter, pureIdx))
                {
                    continue;
                }
                var id = filter.IdSums[pureIdx];
                var hashSum = filter.HashSums[pureIdx];
                var count = filter.Counts[pureIdx];
                var negCount = countComparer.Compare(count, countsIdentity) < 0;
                var isModified = false;
                foreach (var position in configuration.Probe(filter, hashSum))
                {
                    var wasZero = configuration.CountConfiguration.Comparer.Compare(filter.Counts[position], countsIdentity) == 0;
                    if (configuration.IsPure(filter, position) &&
                        !configuration.HashEqualityComparer.Equals(filter.HashSums[position], hashSum) &&
                        configuration.IdEqualityComparer.Equals(id, filter.IdSums[position]))
                    {
                        modifiedEntities?.Add(id);
                        isModified = true;
                        if (negCount)
                        {
                            filter.Add(configuration, id, filter.HashSums[position], position);
                        }
                        else
                        {
                            filter.Remove(configuration, id, filter.HashSums[position], position);
                        }
                    }
                    else
                    {
                        if (negCount)
                        {
                            filter.Add(configuration, id, hashSum, position);
                        }
                        else
                        {
                            filter.Remove(configuration, id, hashSum, position);
                        }
                    }
                    if (!wasZero && configuration.IsPure(filter, position))
                    {
                        //count became pure, add to the list.
                        pureList.Push(position);
                    }
                }
                if (isModified) continue;
                if (negCount)
                {
                    listB.Add(id);
                }
                else
                {
                    listA.Add(id);
                }
            }
            modifiedEntities?.MoveModified(listA, listB);
            return filter.IsCompleteDecode(configuration);
        }

        /// <summary>
        /// Subtract the Bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <typeparam name="THash">The hash type.</typeparam>
        /// <param name="filterData">The filter data</param>
        /// <param name="subtractedFilterData">The Bloom filter data to subtract</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in <paramref name="filterData"/>, but not in <paramref name="subtractedFilterData"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilterData"/>, but not in <paramref name="filterData"/></param>
        /// <param name="pureList">Optional list of pure items.</param>
        /// <param name="destructive">When <c>true</c> the <paramref name="filterData"/> will no longer be valid after the subtract operation, otherwise <c>false</c></param>
        /// <returns>The resulting Bloom filter data</returns>
        private static IInvertibleBloomFilterData<TId, THash, TCount> Subtract<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filterData,
            IInvertibleBloomFilterData<TId, THash, TCount> subtractedFilterData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            Stack<long> pureList = null,
            bool destructive = false
            )
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (!filterData.IsCompatibleWith(subtractedFilterData, configuration))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(subtractedFilterData));
            var foldFactors = configuration.FoldingStrategy?.GetFoldFactors(filterData.BlockSize, subtractedFilterData.BlockSize);
            if (filterData.BlockSize / (foldFactors?.Item1 ?? 1L) !=
                subtractedFilterData.BlockSize / (foldFactors?.Item2 ?? 1L))
            {
                //failed to find folding factors that will make the size of the filters match.
                return null;
            }
            var result = destructive && foldFactors?.Item1 <= 1 ?
               filterData :
             (foldFactors == null || foldFactors.Item1 <= 1 ?
               filterData.CreateDummy(configuration) :
               configuration.DataFactory.Create<TId, THash, TCount>(filterData.Capacity / foldFactors.Item1, filterData.BlockSize / foldFactors.Item1, filterData.HashFunctionCount));
            var idIdentity = configuration.IdIdentity();
            var hashIdentity = configuration.HashIdentity();

            for (var i = 0L; i < result.BlockSize; i++)
            {
                var hashSum = configuration.HashXor(
                   filterData.HashSums.GetFolded(i, foldFactors?.Item1, configuration.HashXor),
                   subtractedFilterData.HashSums.GetFolded(i, foldFactors?.Item2, configuration.HashXor));
                var filterIdSum = filterData.IdSums.GetFolded(i, foldFactors?.Item1, configuration.IdXor);
                var subtractedIdSum = subtractedFilterData.IdSums.GetFolded(i, foldFactors?.Item2, configuration.IdXor);
                var filterCount = filterData.Counts.GetFolded(i, foldFactors?.Item1, configuration.CountConfiguration.Add);
                var subtractedCount = subtractedFilterData.Counts.GetFolded(i, foldFactors?.Item2, configuration.CountConfiguration.Add);
                var idXorResult = configuration.IdXor(filterIdSum, subtractedIdSum);
                if ((!configuration.IdEqualityComparer.Equals(idIdentity, idXorResult) ||
                    !configuration.HashEqualityComparer.Equals(hashIdentity, hashSum)) &&
                    configuration.CountConfiguration.IsPure(filterCount) &&
                    configuration.CountConfiguration.IsPure(subtractedCount))
                {
                    //pure count went to zero: both filters were pure at the given position.
                    listA.Add(filterIdSum);
                    listB.Add(subtractedIdSum);
                    idXorResult = idIdentity;
                    hashSum = hashIdentity;
                }
                result.Counts[i] = configuration.CountConfiguration.Subtract(filterCount, subtractedCount);
                result.HashSums[i] = hashSum;
                result.IdSums[i] = idXorResult;
                if (configuration.IsPure(result, i))
                {
                    pureList?.Push(i);
                }
            }
            //no longer really meaningful.
            result.ItemCount = configuration.CountConfiguration.GetEstimatedCount(result.Counts, result.HashFunctionCount);
            return result;
        }

        /// <summary>
        /// Determine if the decode succeeded.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter</typeparam>
        /// <param name="filter">The IBF data</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns><c>true</c> when the decode was successful, else <c>false</c>.</returns>
        private static bool IsCompleteDecode<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            var idIdentity = configuration.IdIdentity();
            var hashIdentity = configuration.HashIdentity();
            var countIdentity = configuration.CountConfiguration.Identity();
            for (var position = 0L; position < filter.Counts.LongLength; position++)
            {
                if (configuration.CountConfiguration.IsPure(filter.Counts[position]))
                {
                    //item is pure and was skipped on purpose.
                    continue;
                }
                if (!configuration.IdEqualityComparer.Equals(idIdentity, filter.IdSums[position]) ||
                    !configuration.HashEqualityComparer.Equals(hashIdentity, filter.HashSums[position]) ||
                    configuration.CountConfiguration.Comparer.Compare(filter.Counts[position], countIdentity) != 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Duplicate the invertible Bloom filter data
        /// </summary>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="THash">The entity hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="data">The data to duplicate.</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>Bloom filter data configured the same as <paramref name="data"/>, but with empty arrays.</returns>
        /// <remarks>Explicitly does not duplicate the reverse IBF data.</remarks>
        private static InvertibleBloomFilterData<TId, THash, TCount> CreateDummy<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> data,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (data == null) return null;
            var result = configuration.DataFactory.Create<TId, THash, TCount>(data.Capacity, data.BlockSize, data.HashFunctionCount);
            result.IsReverse = data.IsReverse;
            return result;
        }
        #endregion
    }
}