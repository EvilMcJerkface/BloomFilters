﻿namespace TBag.BloomFilter.Test.Invertible.Hybrid
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using BloomFilters;
    using System.Linq;
    using BloomFilters.Invertible;
    using Infrastructure;    /// <summary>
                             /// Simple add and lookup test on Bloom filter.
                             /// </summary>
    [TestClass]
    public class ContainsTest
    {
        /// <summary>
        /// Hybrid has the same (or better ) false positive rates.
        /// </summary>
        [TestMethod]
        public void HybridFalsePositiveTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new HybridDefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = testData.Count(itm => !bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount == 0, "False negative error rate violated");
            notFoundCount = testData.Count(itm => !bloomFilter.ContainsKey(itm.Id));
            Assert.IsTrue(notFoundCount == 0, "False negative error rate violated on ContainsKey");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "False positive error rate violated");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.ContainsKey(itm.Id));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "False positive error rate violated on ContainsKey");
        }
    }
}
