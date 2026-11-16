using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class FirstMinuteRulesTests
    {
        [Test]
        public void Respawn_PrefersSafeFoodRichPointOverRichButDangerous()
        {
            float safeRich = RespawnRules.Score(40, float.PositiveInfinity);
            float safeEmpty = RespawnRules.Score(0, float.PositiveInfinity);
            float dangerousRich = RespawnRules.Score(40, 3f);

            Assert.That(safeRich, Is.GreaterThan(safeEmpty));
            Assert.That(safeEmpty, Is.GreaterThan(dangerousRich));
        }

        [Test]
        public void Respawn_FoodBeyondCapDoesNotKeepGrowingTheScore()
        {
            Assert.That(
                RespawnRules.Score(RespawnRules.FoodCap * 3, 10f),
                Is.EqualTo(RespawnRules.Score(RespawnRules.FoodCap, 10f)));
        }

        [Test]
        public void Respawn_PickBestReturnsHighestAndHandlesEmpty()
        {
            Assert.That(RespawnRules.PickBest(new[] { 0.5f, 2.5f, 1f }), Is.EqualTo(1));
            Assert.That(RespawnRules.PickBest(new[] { 1f, 1f }), Is.EqualTo(0));
            Assert.That(RespawnRules.PickBest(new float[0]), Is.EqualTo(-1));
        }

        [Test]
        public void DirectionArrow_StaysSmallOnScreenAtAnyZoom()
        {
            float near = DirectionArrowRules.WorldSize(3.6f, 0.035f);
            float far = DirectionArrowRules.WorldSize(8.9f, 0.035f);

            Assert.That(near / (2f * 3.6f), Is.EqualTo(0.035f).Within(1e-5f));
            Assert.That(far / (2f * 8.9f), Is.EqualTo(0.035f).Within(1e-5f));
        }

        [Test]
        public void DirectionArrow_SitsOutsideTheBodyEdge()
        {
            float size = DirectionArrowRules.WorldSize(3.6f, 0.035f);
            float distance = DirectionArrowRules.Distance(0.5f, size, 0.5f);

            Assert.That(distance, Is.GreaterThan(0.5f + size * 0.5f));
        }

        [Test]
        public void RankUpDust_PaysEveryRankCrossedOnce()
        {
            Assert.That(DustRules.RankUpDust(1, 1), Is.Zero);
            Assert.That(DustRules.RankUpDust(1, 2), Is.EqualTo(DustRules.RankUp(2)));
            Assert.That(
                DustRules.RankUpDust(1, 4),
                Is.EqualTo(DustRules.RankUp(2) + DustRules.RankUp(3) + DustRules.RankUp(4)));
            Assert.That(DustRules.RankUpDust(4, 2), Is.Zero);
        }

        [Test]
        public void RankUpDust_AllRanksStayBelowHalfOfPaidSkinCatalog()
        {
            const int paidSkins = 8;

            Assert.That(
                DustRules.RankUpDust(1, XpRules.MaxRank),
                Is.LessThan(paidSkins * DustRules.SkinPrice / 2));
        }

        [Test]
        public void OverlaySprites_ThickRingIsADistinctShape()
        {
            Assert.That(OverlaySprites.ThickRing, Is.Not.SameAs(OverlaySprites.Ring));
            Assert.That(OverlaySprites.ThickRing.texture.width, Is.EqualTo(OverlaySprites.Ring.texture.width));
        }
    }
}
