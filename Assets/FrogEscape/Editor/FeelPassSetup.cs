using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogEscape.Editor
{
    public static class FeelPassSetup
    {
        [MenuItem("Frog Escape/Apply Rider Presentation")]
        public static string Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop play mode and save the scene first.");
            const string path="Assets/FrogEscape/Materials/DodgeRibbon.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);
                material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
                AssetDatabase.CreateAsset(material,path);
            }
            foreach(string name in FrogEscapeBuilder.SceneNames)
            {
                var scene=EditorSceneManager.OpenScene("Assets/FrogEscape/Scenes/"+name+".unity");
                var stage=UnityEngine.Object.FindFirstObjectByType<StageController>();
                var effect=stage.player.GetComponent<RiderPresentation>();
                if(!effect)effect=stage.player.gameObject.AddComponent<RiderPresentation>();
                effect.bike=stage.player;effect.trailMaterial=material;
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/FrogEscape/Scenes/MainMenu.unity");return "Rider presentation applied to 5 scenes";
        }
    }
}
