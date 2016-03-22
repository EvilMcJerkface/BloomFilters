﻿namespace TBag.BloomFilters.Measurements.Test
{
    using System;
    using System.Text;
    using BloomFilters;
    using HashAlgorithms;

    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class IbfKeyValueLargeBloomFilterConfiguration : ReverseIbfConfigurationBase<TestEntity, int>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();

        public IbfKeyValueLargeBloomFilterConfiguration() : base(new HighUtilizationCountConfiguration())
        {}

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        protected override int GetEntityHashImpl(TestEntity entity)
        {
            return BitConverter.ToInt32(_murmurHash.Hash(Encoding.UTF32.GetBytes(entity.Value)), 0);
        }
    }
}