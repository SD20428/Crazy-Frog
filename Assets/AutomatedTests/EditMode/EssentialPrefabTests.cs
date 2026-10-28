using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrazyFrog.AutomatedTests.EditMode
{
    public class EssentialPrefabTests
    {
        [TestCase("FrogBike")]
        [TestCase("PoliceCar")]
        [TestCase("CometMissile")]
        public void VisualPrefabLoadsWithRequiredPartsAndValidRenderReferences(string name)
        {
            var path = "Assets/FrogEscape/Prefabs/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, "Essential prefab must load: " + path);
            Assert.That(PrefabUtility.IsPartOfPrefabAsset(prefab), Is.True);
            Assert.That(prefab.name, Is.EqualTo(name));
            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            var parts = name == "FrogBike" ? new[] { "Visual", "Rider" }
                : name == "PoliceCar" ? new[] { "RedLamp", "BlueLamp", "Wheel" }
                : new[] { "Model", "Body", "Nose" };
            foreach (var part in parts)
                Assert.That(transforms.Any(item => item.name == part), Is.True, name + " is missing required visual part: " + part);
            foreach (var item in transforms)
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject), Is.Zero,
                    "Missing script in prefab object: " + item.name);
            var meshes = prefab.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(meshes.Length, Is.GreaterThan(0), "Prefab has no mesh geometry: " + name);
            foreach (var mesh in meshes)
                Assert.That(mesh.sharedMesh, Is.Not.Null, "Missing shared mesh: " + mesh.name);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0));
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials.Length, Is.GreaterThan(0), "No materials: " + renderer.name);
                foreach (var material in renderer.sharedMaterials)
                    Assert.That(material, Is.Not.Null, "Missing material: " + renderer.name);
            }
            // These are visual templates: runtime controllers are assembled in scenes.
            // Never instantiate, save, or mutate the original prefab for this check.
        }
    }
}
