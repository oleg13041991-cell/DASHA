#nullable enable
using System;
using NUnit.Framework;
using RagsToRiches.Core.Timing;

namespace RagsToRiches.Tests
{
    /// <summary>
    /// Первые тесты Core-слоя: проверяют инжектируемое время
    /// и заодно верифицируют, что пайплайн Unity Test Runner (edit mode) работает.
    /// </summary>
    public sealed class ManualTimeProviderTests
    {
        [Test]
        public void Now_StartsAtZero()
        {
            var time = new ManualTimeProvider();

            Assert.That(time.Now, Is.EqualTo(0d));
        }

        [Test]
        public void Advance_AccumulatesSeconds()
        {
            var time = new ManualTimeProvider();

            time.Advance(1.5d);
            time.Advance(2.5d);

            Assert.That(time.Now, Is.EqualTo(4d).Within(1e-9));
        }

        [Test]
        public void Advance_NegativeDelta_Throws()
        {
            var time = new ManualTimeProvider();

            Assert.Throws<ArgumentOutOfRangeException>(() => time.Advance(-0.1d));
        }

        [Test]
        public void UtcNowUnixSeconds_IsSettable()
        {
            var time = new ManualTimeProvider { UtcNowUnixSeconds = 1_770_000_000L };

            Assert.That(time.UtcNowUnixSeconds, Is.EqualTo(1_770_000_000L));
        }
    }
}
