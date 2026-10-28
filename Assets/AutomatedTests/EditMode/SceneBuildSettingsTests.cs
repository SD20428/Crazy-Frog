using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace CrazyFrog.AutomatedTests.EditMode
{
    public class SceneBuildSettingsTests
    {
        [TestCase("Assets/FrogEscape/Scenes/MainMenu.unity")]
        [TestCase("Assets/FrogEscape/Scenes/Stage01_Overpass.unity")]
        [TestCase("Assets/FrogEscape/Scenes/Stage02_Sewer.unity")]
        [TestCase("Assets/FrogEscape/Scenes/Stage03_MissileChase.unity")]
        [TestCase("Assets/FrogEscape/Scenes/Ending.unity")]
        public void RequiredSceneExistsAndIsEnabledExactlyOnce(string path)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null,
                "Required game scene is missing or cannot be loaded as a SceneAsset: " + path);
            var entries = EditorBuildSettings.scenes.Where(scene => scene.path == path).ToArray();
            Assert.That(entries.Length, Is.EqualTo(1), "Required scene must be registered exactly once: " + path);
            Assert.That(entries[0].enabled, Is.True, "Required scene is disabled: " + path);
            Assert.That(entries[0].guid.ToString(), Is.EqualTo(AssetDatabase.AssetPathToGUID(path)),
                "Build Settings GUID and actual scene asset disagree: " + path);
            // Additional scenes, including the original SampleScene, are permitted.
        }

        [Test]
        public void ConfigScenePathsMatchTheActualGameSequence()
        {
            using (var config = new SerializedObject(TestAssets.LoadConfig()))
            {
                Assert.That(TestAssets.Property(config, "menuScene").stringValue, Is.EqualTo(TestAssets.GameScenes[0]));
                Assert.That(TestAssets.Property(config, "endingScene").stringValue, Is.EqualTo(TestAssets.GameScenes[4]));
                var stages = TestAssets.Property(config, "stageScenes");
                Assert.That(stages.arraySize, Is.EqualTo(3), "The implemented game has three gameplay stages.");
                for (var i = 0; i < stages.arraySize; i++)
                    Assert.That(stages.GetArrayElementAtIndex(i).stringValue, Is.EqualTo(TestAssets.GameScenes[i + 1]),
                        "Incorrect configured stage path/order at index " + i);
            }
        }
    }
}
