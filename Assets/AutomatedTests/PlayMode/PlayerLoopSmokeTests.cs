using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrazyFrog.AutomatedTests.PlayMode
{
    // Compile/discover only in P3 step 1. Execution requires separate approval.
    public class PlayerLoopSmokeTests
    {
        Scene ownedScene;
        GameObject ownedObject;

        [UnityTest]
        public IEnumerator PlayerLoopAdvancesInAnIsolatedTemporaryScene()
        {
            // The Test Runner must supply its empty bootstrap scene, not a game scene.
            for (var i = 0; i < SceneManager.sceneCount; i++)
                Assert.That(SceneManager.GetSceneAt(i).path.StartsWith("Assets/FrogEscape/", StringComparison.Ordinal),
                    Is.False, "Stop: a game scene is loaded in the smoke-test environment.");

            ownedScene = SceneManager.CreateScene("CF-Smoke-" + Guid.NewGuid().ToString("N"));
            ownedObject = new GameObject("CF-Smoke-Probe");
            SceneManager.MoveGameObjectToScene(ownedObject, ownedScene);
            Assert.That(ownedScene.path, Is.Empty, "The temporary test scene must never be saved.");
            Assert.That(ownedScene.GetRootGameObjects().Length, Is.EqualTo(1));
            var firstFrame = Time.frameCount;
            var started = Time.realtimeSinceStartupAsDouble;
            while (Time.frameCount < firstFrame + 2)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble - started, Is.LessThan(5d),
                    "Player loop failed to advance two frames within five real seconds.");
                yield return null;
            }
            Assert.That(ownedObject, Is.Not.Null);
            Assert.That(ownedObject.scene, Is.EqualTo(ownedScene));
        }

        [UnityTearDown]
        public IEnumerator RemoveOnlyResourcesOwnedByThisTest()
        {
            if (ownedObject != null)
            {
                UnityEngine.Object.Destroy(ownedObject);
                yield return null;
            }
            if (ownedScene.IsValid() && ownedScene.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(ownedScene);
                Assert.That(unload, Is.Not.Null, "Temporary scene could not begin unloading.");
                var started = Time.realtimeSinceStartupAsDouble;
                while (!unload.isDone)
                {
                    Assert.That(Time.realtimeSinceStartupAsDouble - started, Is.LessThan(5d),
                        "Temporary scene unload did not complete within five real seconds.");
                    yield return null;
                }
            }
            ownedObject = null;
            ownedScene = default;
            // No Input System, timeScale, AudioListener, or game singleton writes.
        }
    }
}
