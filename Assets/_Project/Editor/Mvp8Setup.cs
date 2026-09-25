using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Palsoul.Base;
using Palsoul.Combat;
using Palsoul.Progression;
using Palsoul.UI;

namespace Palsoul.Editor
{
    /// <summary>Additive migration: preserves the authored scene and existing prototype data.</summary>
    public static class Mvp8Setup
    {
        [MenuItem("Palsoul/Upgrade Prototype to MVP 8")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isDirty) throw new System.InvalidOperationException("Save the current scene before upgrading MVP 8.");
            string root = PrototypeBuilder.Root;
            string markerPath = root + "/Eco.prefab";
            var marker = AssetDatabase.LoadAssetAtPath<GameObject>(markerPath);
            if (marker == null)
            {
                var go = new GameObject("Eco", typeof(SpriteRenderer), typeof(DeathMarker));
                var sprite = go.GetComponent<SpriteRenderer>();
                sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Anchor.png");
                sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(root + "/SpritesUnlit.mat");
                sprite.color = Color.cyan;
                sprite.sortingOrder = 4;
                marker = PrefabUtility.SaveAsPrefabAsset(go, markerPath);
                Object.DestroyImmediate(go);
            }
            string playerPath = root + "/Player.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var system = prefab.GetComponent<PlayerDeathSystem>();
                bool changed = system == null;
                if (system == null) system = prefab.AddComponent<PlayerDeathSystem>();
                changed |= SetIfMissing(system, "markerPrefab", marker.GetComponent<DeathMarker>());
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefab, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            if (scene.path != PrototypeBuilder.ScenePath)
                scene = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var death = player.GetComponent<PlayerDeathSystem>();
            bool sceneChanged = death == null;
            if (death == null) death = player.gameObject.AddComponent<PlayerDeathSystem>();
            var anchor = Object.FindAnyObjectByType<AnchorpointController>();
            sceneChanged |= SetIfMissing(death, "markerPrefab", marker.GetComponent<DeathMarker>());
            sceneChanged |= SetIfMissing(death, "initialAnchor", anchor);
            var hud = Object.FindAnyObjectByType<PrototypeHUD>();
            sceneChanged |= SetIfMissing(hud, "humanPortrait", AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Human.png"));
            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }

        private static bool SetIfMissing(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            if (serialized.FindProperty(field).objectReferenceValue != null) return false;
            PrototypeBuilder.Set(target, field, value);
            return true;
        }
    }
}
