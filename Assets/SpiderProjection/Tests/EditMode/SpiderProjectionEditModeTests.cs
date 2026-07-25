using NUnit.Framework;
using SpiderProjection.Runtime;
using UnityEngine;

namespace SpiderProjection.Tests.EditMode
{
    public sealed class SpiderProjectionEditModeTests
    {
        [Test]
        public void AnchorScore_PrefersAlignedForwardCandidate()
        {
            Vector2 origin = Vector2.zero;
            Vector2 aim = new Vector2(1f, 0.5f).normalized;
            float aligned = WebTargetResolver2D.ScoreCandidate(
                origin,
                new Vector2(4f, 2f),
                aim,
                1,
                8f);
            float behind = WebTargetResolver2D.ScoreCandidate(
                origin,
                new Vector2(-4f, 2f),
                aim,
                1,
                8f);

            Assert.That(aligned, Is.GreaterThan(behind));
            Assert.That(aligned, Is.InRange(0f, 1f));
        }

        [Test]
        public void StatePriority_ProtectsCriticalStates()
        {
            Assert.That(
                StatePriority.Resolve(GameplayState.Hurt, GameplayState.Run),
                Is.EqualTo(GameplayState.Hurt));
            Assert.That(
                StatePriority.Resolve(GameplayState.SwingLoop, GameplayState.Defeat),
                Is.EqualTo(GameplayState.Defeat));
            Assert.That(
                StatePriority.GetPriority(GameplayState.WallCling),
                Is.GreaterThan(StatePriority.GetPriority(GameplayState.Roll)));
        }

        [Test]
        public void InputBuffer_ExpiresAndConsumesOnce()
        {
            InputBufferTimer timer = default;
            timer.Buffer(10f, 0.12f);

            Assert.That(timer.IsBuffered(10.1f), Is.True);
            Assert.That(timer.Consume(10.1f), Is.True);
            Assert.That(timer.Consume(10.1f), Is.False);

            timer.Buffer(20f, 0.12f);
            Assert.That(timer.IsBuffered(20.13f), Is.False);
        }

        [Test]
        public void ProjectionCoordinates_ConvertNormalizedLayoutWithoutWarpingGameplay()
        {
            const float orthographicSize = 5.4f;
            const float aspect = 16f / 9f;

            Assert.That(
                ProjectionCoordinates.NormalizedToWorld(new Vector2(0.5f, 0.5f), orthographicSize, aspect),
                Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

            Rect world = ProjectionCoordinates.NormalizedRectToWorld(
                new Rect(0.25f, 0.25f, 0.5f, 0.5f),
                orthographicSize,
                aspect);
            Assert.That(world.center, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
            Assert.That(world.width, Is.EqualTo(9.6f).Within(0.0001f));
            Assert.That(world.height, Is.EqualTo(5.4f).Within(0.0001f));
        }

        private sealed class Vector2EqualityComparer : System.Collections.IEqualityComparer
        {
            public static readonly Vector2EqualityComparer Instance = new Vector2EqualityComparer();

            public new bool Equals(object left, object right)
            {
                return left is Vector2 a && right is Vector2 b && Vector2.Distance(a, b) < 0.0001f;
            }

            public int GetHashCode(object value)
            {
                return value.GetHashCode();
            }
        }
    }
}
