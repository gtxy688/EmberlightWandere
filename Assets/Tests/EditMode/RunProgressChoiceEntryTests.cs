using System;
using System.Linq;
using NUnit.Framework;

namespace Emberlight.Tests
{
    public sealed class RunProgressChoiceEntryTests
    {
        [Test]
        public void Choices_OpensExactlyOnePendingPickThatCanBeCommitted()
        {
            var progress = new RunProgress();
            var offers = progress.Choices(new Random(42));
            Assert.That(progress.Pending, Is.EqualTo(1));
            var choice = offers.First(o => o.Kind == EmberOfferKind.Generic);
            Assert.That(progress.Choose(choice), Is.True);
            Assert.That(progress.Pending, Is.Zero);
            Assert.That(progress.Choose(choice), Is.False);
        }

        [Test]
        public void Refresh_ReplacesChoicesWithoutAccumulatingPendingPicks()
        {
            var progress = new RunProgress();
            var random = new Random(42);
            progress.Choices(random);
            EmberOffer[] next;
            Assert.That(progress.TryRefresh(random, out next), Is.True);
            Assert.That(progress.Pending, Is.EqualTo(1));
            Assert.That(progress.Choose(next.First(o => o.Kind == EmberOfferKind.Generic)), Is.True);
            Assert.That(progress.Pending, Is.Zero);
        }
    }
}
