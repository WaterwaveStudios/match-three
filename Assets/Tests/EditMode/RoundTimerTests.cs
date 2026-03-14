using NUnit.Framework;
using MatchThree.Core;

namespace MatchThree.Tests
{
    [TestFixture]
    public class RoundTimerTests
    {
        [Test]
        public void StartsWithFullDuration()
        {
            var timer = new RoundTimer(20f);
            Assert.AreEqual(20f, timer.TimeRemaining, 0.001f);
        }

        [Test]
        public void IsExpired_FalseWhenTimeRemaining()
        {
            var timer = new RoundTimer(20f);
            Assert.IsFalse(timer.IsExpired);
        }

        [Test]
        public void Tick_ReducesTimeRemaining()
        {
            var timer = new RoundTimer(20f);
            timer.Tick(5f);
            Assert.AreEqual(15f, timer.TimeRemaining, 0.001f);
        }

        [Test]
        public void Tick_ClampsAtZero()
        {
            var timer = new RoundTimer(10f);
            timer.Tick(15f);
            Assert.AreEqual(0f, timer.TimeRemaining, 0.001f);
        }

        [Test]
        public void IsExpired_TrueWhenTimeReachesZero()
        {
            var timer = new RoundTimer(5f);
            timer.Tick(5f);
            Assert.IsTrue(timer.IsExpired);
        }

        [Test]
        public void IsExpired_TrueWhenTickedPastZero()
        {
            var timer = new RoundTimer(5f);
            timer.Tick(10f);
            Assert.IsTrue(timer.IsExpired);
        }

        [Test]
        public void Reset_RestoresFullDuration()
        {
            var timer = new RoundTimer(20f);
            timer.Tick(15f);
            timer.Reset();
            Assert.AreEqual(20f, timer.TimeRemaining, 0.001f);
            Assert.IsFalse(timer.IsExpired);
        }

        [Test]
        public void MultipleTicks_AccumulateCorrectly()
        {
            var timer = new RoundTimer(20f);
            timer.Tick(3f);
            timer.Tick(7f);
            timer.Tick(2f);
            Assert.AreEqual(8f, timer.TimeRemaining, 0.001f);
        }

        [Test]
        public void OnExpired_FiresWhenTimerReachesZero()
        {
            var timer = new RoundTimer(5f);
            bool fired = false;
            timer.OnExpired += () => fired = true;
            timer.Tick(5f);
            Assert.IsTrue(fired);
        }

        [Test]
        public void OnExpired_FiresOnlyOnce()
        {
            var timer = new RoundTimer(5f);
            int fireCount = 0;
            timer.OnExpired += () => fireCount++;
            timer.Tick(5f);
            timer.Tick(1f);
            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void Reset_AllowsOnExpiredToFireAgain()
        {
            var timer = new RoundTimer(5f);
            int fireCount = 0;
            timer.OnExpired += () => fireCount++;
            timer.Tick(5f);
            timer.Reset();
            timer.Tick(5f);
            Assert.AreEqual(2, fireCount);
        }

        [Test]
        public void Duration_ReturnsInitialDuration()
        {
            var timer = new RoundTimer(20f);
            Assert.AreEqual(20f, timer.Duration, 0.001f);
        }
    }
}
