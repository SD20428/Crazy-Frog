using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrazyFrog.AutomatedTests.EditMode
{
    public class GameConfigAssetTests
    {
        [Test]
        public void BothDifficultySettingsHaveUsableMovementAndTimingValues()
        {
            using (var config = new SerializedObject(TestAssets.LoadConfig()))
            {
                foreach (var mode in new[] { "easy", "normal" })
                {
                    Assert.That(TestAssets.Property(config, mode + ".label").stringValue, Is.Not.Null.And.Not.Empty);
                    foreach (var field in new[] { "health", "obstacleStride" })
                        Assert.That(TestAssets.Property(config, mode + "." + field).intValue, Is.GreaterThan(0), mode + "." + field);
                    foreach (var field in new[] { "speed", "lateralSpeed", "warningTime", "invulnerability",
                        "dodgeDuration", "dodgeCooldown", "missileDodgeWindow", "attackInterval",
                        "policeGap", "policeSpeed", "missileTurnRate", "missileMaxSpeed" })
                        TestAssets.Positive(TestAssets.Property(config, mode + "." + field).floatValue, mode + "." + field);
                    Assert.That(TestAssets.Property(config, mode + ".dodgeCooldown").floatValue,
                        Is.GreaterThanOrEqualTo(TestAssets.Property(config, mode + ".dodgeDuration").floatValue),
                        mode + " cooldown must not end before the current dodge.");
                    Assert.That(TestAssets.Property(config, mode + ".missileDodgeWindow").floatValue,
                        Is.LessThanOrEqualTo(TestAssets.Property(config, mode + ".warningTime").floatValue),
                        mode + " dodge window must fit inside the base warning duration.");
                }
            }
        }

        [Test]
        public void StageDurationsAreFinitePositiveAndMatchStageCount()
        {
            using (var config = new SerializedObject(TestAssets.LoadConfig()))
            {
                var durations = TestAssets.Property(config, "stageDurations");
                Assert.That(durations.arraySize, Is.EqualTo(TestAssets.Property(config, "stageScenes").arraySize));
                Assert.That(durations.arraySize, Is.EqualTo(3));
                for (var i = 0; i < durations.arraySize; i++)
                    TestAssets.Positive(durations.GetArrayElementAtIndex(i).floatValue, "stageDurations[" + i + "]");
            }
        }

        [Test]
        public void RequiredAudioMaterialsAndTimelinesResolve()
        {
            using (var config = new SerializedObject(TestAssets.LoadConfig()))
            {
                foreach (var field in new[] { "music", "engine", "siren", "warning", "dodge", "impact", "landing", "victory" })
                    Assert.That(TestAssets.Property(config, field).objectReferenceValue, Is.InstanceOf<AudioClip>(), "Missing/wrong audio reference: " + field);
                foreach (var field in new[] { "warningMaterial", "successMaterial" })
                    Assert.That(TestAssets.Property(config, field).objectReferenceValue, Is.InstanceOf<Material>(), "Missing/wrong material reference: " + field);
                var transitions = TestAssets.Property(config, "transitions");
                Assert.That(transitions.arraySize, Is.EqualTo(2), "Two inter-stage transitions are required.");
                for (var i = 0; i < transitions.arraySize; i++)
                    AssertTimeline(transitions.GetArrayElementAtIndex(i).objectReferenceValue, "transitions[" + i + "]");
                AssertTimeline(TestAssets.Property(config, "ending").objectReferenceValue, "ending");
            }
        }

        static void AssertTimeline(Object asset, string field)
        {
            Assert.That(asset, Is.Not.Null, "Missing Timeline: " + field);
            Assert.That(asset.GetType().FullName, Is.EqualTo("UnityEngine.Timeline.TimelineAsset"), "Wrong reference type: " + field);
        }
    }
}
