using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheTasteReviver.Editor
{
    public static class IngredientVisualPrefabMigration
    {
        private const string UniformGroundScaleRequestPath = "Temp/SetIngredientAfterGrindingScaleToPointFour.request";

        [InitializeOnLoadMethod]
        private static void QueueMigrationForLegacyPrefabs()
        {
            EditorApplication.update -= MigrateWhenEditorIsReady;
            EditorApplication.update += MigrateWhenEditorIsReady;
        }

        private static void MigrateWhenEditorIsReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= MigrateWhenEditorIsReady;
            if (HasLegacyIngredientPrefabs())
            {
                Migrate();
            }

            if (File.Exists(UniformGroundScaleRequestPath))
            {
                SetAllAfterGrindingScalesToPointFour();
                File.Delete(UniformGroundScaleRequestPath);
            }
        }

        private static bool HasLegacyIngredientPrefabs()
        {
            string[] ingredientGuids = AssetDatabase.FindAssets("t:IngredientData", new[] { "Assets/Data/GeneratedAssets/Ingredients" });
            foreach (string ingredientGuid in ingredientGuids)
            {
                IngredientData ingredient = AssetDatabase.LoadAssetAtPath<IngredientData>(
                    AssetDatabase.GUIDToAssetPath(ingredientGuid));
                if (ingredient != null && ingredient.prefab != null &&
                    ingredient.prefab.GetComponent<IngredientVisualState>() == null)
                {
                    return true;
                }
            }

            return false;
        }

        [MenuItem("Tools/The Taste Reviver/Migrate Ingredient Prefabs To Visual States")]
        public static void Migrate()
        {
            string[] ingredientGuids = AssetDatabase.FindAssets("t:IngredientData", new[] { "Assets/Data/GeneratedAssets/Ingredients" });
            int migrated = 0;
            foreach (string ingredientGuid in ingredientGuids)
            {
                string ingredientPath = AssetDatabase.GUIDToAssetPath(ingredientGuid);
                IngredientData ingredient = AssetDatabase.LoadAssetAtPath<IngredientData>(ingredientPath);
                if (ingredient == null || ingredient.prefab == null)
                {
                    continue;
                }

                string prefabPath = AssetDatabase.GetAssetPath(ingredient.prefab);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    IngredientVisualState visualState = root.GetComponent<IngredientVisualState>();
                    if (visualState == null)
                    {
                        visualState = root.AddComponent<IngredientVisualState>();
                    }

                    Transform before = root.transform.Find("Before Grinding");
                    if (before == null)
                    {
                        before = new GameObject("Before Grinding").transform;
                        before.SetParent(root.transform, false);
                        List<Transform> existingChildren = root.transform.Cast<Transform>()
                            .Where(child => child != before && child.name != "After Grinding")
                            .ToList();
                        foreach (Transform child in existingChildren)
                        {
                            child.SetParent(before, true);
                        }
                    }

                    Transform after = root.transform.Find("After Grinding");
                    if (after == null && ingredient.groundPrefab != null)
                    {
                        GameObject afterInstance = (GameObject)PrefabUtility.InstantiatePrefab(ingredient.groundPrefab, root.transform);
                        afterInstance.name = "After Grinding";
                        after = afterInstance.transform;
                    }

                    visualState.beforeGrinding = before.gameObject;
                    visualState.afterGrinding = after != null ? after.gameObject : null;
                    before.gameObject.SetActive(true);
                    if (after != null)
                    {
                        after.gameObject.SetActive(false);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    migrated++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Migrated ingredient prefabs to authored Before Grinding / After Grinding visual states: " + migrated);
        }

        [MenuItem("Tools/The Taste Reviver/Set All After Grinding Scales To 0.4")]
        public static void SetAllAfterGrindingScalesToPointFour()
        {
            string[] ingredientGuids = AssetDatabase.FindAssets("t:IngredientData", new[] { "Assets/Data/GeneratedAssets/Ingredients" });
            int updated = 0;
            foreach (string ingredientGuid in ingredientGuids)
            {
                IngredientData ingredient = AssetDatabase.LoadAssetAtPath<IngredientData>(
                    AssetDatabase.GUIDToAssetPath(ingredientGuid));
                if (ingredient == null || ingredient.prefab == null)
                {
                    continue;
                }

                string prefabPath = AssetDatabase.GetAssetPath(ingredient.prefab);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    IngredientVisualState visualState = root.GetComponent<IngredientVisualState>();
                    if (visualState == null || visualState.afterGrinding == null)
                    {
                        continue;
                    }

                    visualState.afterGrinding.transform.localScale = Vector3.one * 0.4f;
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    updated++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Set authored After Grinding scale to 0.4 for ingredient prefabs: " + updated);
        }
    }
}
