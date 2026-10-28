using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrazyFrog.AutomatedTests.EditMode
{
    internal static class TestAssets
    {
        internal const string ConfigPath = "Assets/FrogEscape/ScriptableObjects/GameConfig.asset";
        internal static readonly string[] GameScenes =
        {
            "Assets/FrogEscape/Scenes/MainMenu.unity",
            "Assets/FrogEscape/Scenes/Stage01_Overpass.unity",
            "Assets/FrogEscape/Scenes/Stage02_Sewer.unity",
            "Assets/FrogEscape/Scenes/Stage03_MissileChase.unity",
            "Assets/FrogEscape/Scenes/Ending.unity"
        };

        internal static ScriptableObject LoadConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ConfigPath);
            Assert.That(config, Is.Not.Null, "GameConfig must load through AssetDatabase.");
            Assert.That(config.GetType().FullName, Is.EqualTo("FrogEscape.GameConfig"),
                "The config asset must retain its actual runtime script type.");
            return config;
        }

        internal static SerializedProperty Property(SerializedObject target, string path)
        {
            var property = target.FindProperty(path);
            Assert.That(property, Is.Not.Null, "Missing actual serialized GameConfig property: " + path);
            return property;
        }

        internal static void Positive(float value, string field)
        {
            Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False, field + " must be finite.");
            Assert.That(value, Is.GreaterThan(0f), field + " must be positive.");
        }
    }
}
