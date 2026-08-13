#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheTasteReviver.EditorTools
{
    public static class IngredientPrefabGenerator
    {
        private const string ArtRoot = "Assets/Art/Ingredients";
        private const string EnvironmentRoot = "Assets/Art/Environment";
        private const string IngredientDataRoot = "Assets/Data/GeneratedAssets/Ingredients";
        private const string BowlModelPath = "Assets/Art/Environment/Pestle/grinding_bowl_2DPlane.fbx";

        [MenuItem("The Taste Reviver/Generate Visible Art Prefabs")]
        public static void GenerateVisibleArtPrefabs()
        {
            GenerateAndAssignIngredientPrefabs();
            GenerateEnvironmentPrefabs();
        }

        [MenuItem("The Taste Reviver/Generate Ingredient Prefabs")]
        public static void GenerateAndAssignIngredientPrefabs()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Ingredients");
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "GeneratedAssets");
            EnsureFolder("Assets/Data/GeneratedAssets", "Ingredients");

            Dictionary<string, IngredientData> ingredients = LoadOrCreateIngredientAssets();
            foreach (IngredientData ingredient in ingredients.Values)
            {
                if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.ingredientID))
                {
                    continue;
                }

                GameObject prefab = CreateOrReplacePrefab(ingredient);
                ingredient.prefab = prefab;
                EditorUtility.SetDirty(ingredient);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated and assigned ingredient prefabs: " + ingredients.Count + ".");
        }

        [MenuItem("The Taste Reviver/Generate Environment Prefabs")]
        public static void GenerateEnvironmentPrefabs()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Environment");

            CreateMortarPrefab();
            CreatePestlePrefab();
            CreatePlatePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated visible environment prefabs.");
        }

        private static Dictionary<string, IngredientData> LoadOrCreateIngredientAssets()
        {
            Dictionary<string, IngredientData> result = new Dictionary<string, IngredientData>();
            foreach (string guid in AssetDatabase.FindAssets("t:IngredientData", new[] { IngredientDataRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                IngredientData asset = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
                if (asset == null || string.IsNullOrWhiteSpace(asset.ingredientID))
                {
                    continue;
                }

                EditorUtility.SetDirty(asset);
                result[asset.ingredientID] = asset;
            }

            if (result.Count == 0)
            {
                Debug.LogWarning("No IngredientData assets found. Import design data into Assets/Data/GeneratedAssets/Ingredients first.");
            }

            return result;
        }

        private static GameObject CreateOrReplacePrefab(IngredientData ingredient)
        {
            string ingredientFolder = ArtRoot + "/" + ingredient.ingredientID;
            EnsureIngredientFolders(ingredientFolder);

            Material material = CreateOrUpdateMaterial(ingredient, ingredientFolder + "/Materials/M_" + ingredient.ingredientID + ".mat");
            GameObject root = new GameObject("PF_" + ingredient.ingredientID);
            DraggableIngredient drag = root.AddComponent<DraggableIngredient>();

            BuildVisual(root.transform, ingredient.ingredientID, material);
            ConfigureCollider(root, ingredient.ingredientID);

            string prefabPath = ingredientFolder + "/Prefabs/PF_" + ingredient.ingredientID + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            if (prefab != null)
            {
                DraggableIngredient prefabDrag = prefab.GetComponent<DraggableIngredient>();
                if (prefabDrag != null)
                {
                    prefabDrag.sourcePrefab = prefab;
                    EditorUtility.SetDirty(prefabDrag);
                }
            }

            return prefab;
        }

        private static void CreateMortarPrefab()
        {
            string rootFolder = EnvironmentRoot + "/Mortar";
            EnsureEnvironmentFolders(rootFolder);

            Material stone = CreateOrUpdateMaterial("M_Mortar_Stone", new Color(0.72f, 0.68f, 0.6f), rootFolder + "/Materials/M_Mortar_Stone.mat");
            GameObject root = new GameObject("PF_Mortar");

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BowlModelPath);
            if (model != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Bowl Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * 0.75f;
                ApplyMaterialToRenderers(visual, stone);
            }
            else
            {
                GameObject bowl = AddPrimitive(root.transform, PrimitiveType.Cylinder, "Bowl Visual", stone);
                bowl.transform.localScale = new Vector3(0.9f, 0.22f, 0.9f);
            }

            GameObject inner = AddPrimitive(root.transform, PrimitiveType.Cylinder, "Grinding Surface", stone);
            inner.transform.localPosition = Vector3.up * 0.13f;
            inner.transform.localScale = new Vector3(0.62f, 0.05f, 0.62f);

            SavePrefab(root, rootFolder + "/Prefabs/PF_Mortar.prefab");
        }

        private static void CreatePestlePrefab()
        {
            string rootFolder = EnvironmentRoot + "/Pestle";
            EnsureEnvironmentFolders(rootFolder);

            Material wood = CreateOrUpdateMaterial("M_Pestle_Wood", new Color(0.56f, 0.38f, 0.22f), rootFolder + "/Materials/M_Pestle_Wood.mat");
            GameObject root = new GameObject("PF_Pestle");

            GameObject handle = AddPrimitive(root.transform, PrimitiveType.Cylinder, "Handle", wood);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
            handle.transform.localScale = new Vector3(0.13f, 0.58f, 0.13f);

            GameObject head = AddPrimitive(root.transform, PrimitiveType.Sphere, "Grinding Head", wood);
            head.transform.localPosition = new Vector3(0.14f, -0.48f, 0f);
            head.transform.localScale = new Vector3(0.22f, 0.16f, 0.22f);

            root.AddComponent<PestleController>();
            SavePrefab(root, rootFolder + "/Prefabs/PF_Pestle.prefab");
        }

        private static void CreatePlatePrefab()
        {
            string rootFolder = EnvironmentRoot + "/Props";
            EnsureEnvironmentFolders(rootFolder);

            Material ceramic = CreateOrUpdateMaterial("M_IngredientPlate_Ceramic", Color.white, rootFolder + "/Materials/M_IngredientPlate_Ceramic.mat");
            GameObject root = new GameObject("PF_IngredientPlate");
            GameObject plate = AddPrimitive(root.transform, PrimitiveType.Cylinder, "Plate", ceramic);
            plate.transform.localScale = new Vector3(0.75f, 0.08f, 0.75f);

            SavePrefab(root, rootFolder + "/Prefabs/PF_IngredientPlate.prefab");
        }

        private static Material CreateOrUpdateMaterial(IngredientData ingredient, string path)
        {
            return CreateOrUpdateMaterial("M_" + ingredient.ingredientID, ingredient.ingredientColor, path);
        }

        private static Material CreateOrUpdateMaterial(string materialName, Color color, string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = materialName;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildVisual(Transform root, string id, Material material)
        {
            switch (id)
            {
                case "Rice":
                case "GlutinousRice":
                    AddGrainCluster(root, material, id == "GlutinousRice" ? 7 : 5);
                    break;
                case "TeaLeaf":
                case "LotusLeaf":
                case "ScallionWhite":
                    AddLeafCluster(root, material, id == "LotusLeaf" ? 3 : 5);
                    break;
                case "BlackSesame":
                case "CoarseSalt":
                case "WhitePepper":
                case "YangjiangDouchi":
                    AddSeedCluster(root, material, id);
                    break;
                case "DriedTangerinePeel":
                case "Ginger":
                case "SandGinger":
                    AddSliceCluster(root, material, id);
                    break;
                case "RockSugar":
                    AddCrystalCluster(root, material);
                    break;
                case "Peanut":
                case "PeanutCrumb":
                    AddPeanutCluster(root, material);
                    break;
                case "RedBean":
                    AddBeanCluster(root, material);
                    break;
                case "Chili":
                    AddChiliShape(root, material);
                    break;
                default:
                    AddSeedCluster(root, material, id);
                    break;
            }
        }

        private static void AddGrainCluster(Transform root, Material material, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject grain = AddPrimitive(root, PrimitiveType.Sphere, "Grain", material);
                grain.transform.localPosition = CirclePosition(i, count, 0.18f) + Vector3.up * (0.02f * (i % 2));
                grain.transform.localRotation = Quaternion.Euler(0f, i * 37f, 18f);
                grain.transform.localScale = new Vector3(0.16f, 0.07f, 0.28f);
            }
        }

        private static void AddLeafCluster(Transform root, Material material, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject leaf = AddPrimitive(root, PrimitiveType.Cube, "Leaf", material);
                leaf.transform.localPosition = CirclePosition(i, count, 0.18f);
                leaf.transform.localRotation = Quaternion.Euler(0f, i * 53f, 8f);
                leaf.transform.localScale = new Vector3(0.08f, 0.012f, 0.38f);
            }
        }

        private static void AddSeedCluster(Transform root, Material material, string id)
        {
            int count = id == "CoarseSalt" ? 6 : 9;
            for (int i = 0; i < count; i++)
            {
                GameObject seed = AddPrimitive(root, id == "CoarseSalt" ? PrimitiveType.Cube : PrimitiveType.Sphere, "Piece", material);
                seed.transform.localPosition = CirclePosition(i, count, 0.2f) + Vector3.up * (0.025f * (i % 3));
                seed.transform.localRotation = Quaternion.Euler(i * 13f, i * 29f, i * 7f);
                seed.transform.localScale = id == "CoarseSalt" ? Vector3.one * 0.11f : new Vector3(0.11f, 0.08f, 0.11f);
            }
        }

        private static void AddSliceCluster(Transform root, Material material, string id)
        {
            int count = id == "DriedTangerinePeel" ? 4 : 3;
            for (int i = 0; i < count; i++)
            {
                GameObject slice = AddPrimitive(root, PrimitiveType.Cube, "Slice", material);
                slice.transform.localPosition = CirclePosition(i, count, 0.16f);
                slice.transform.localRotation = Quaternion.Euler(0f, i * 61f, 15f);
                slice.transform.localScale = new Vector3(0.28f, 0.055f, 0.13f);
            }
        }

        private static void AddCrystalCluster(Transform root, Material material)
        {
            for (int i = 0; i < 5; i++)
            {
                GameObject crystal = AddPrimitive(root, PrimitiveType.Cube, "Crystal", material);
                crystal.transform.localPosition = CirclePosition(i, 5, 0.17f);
                crystal.transform.localRotation = Quaternion.Euler(i * 18f, i * 41f, i * 23f);
                crystal.transform.localScale = Vector3.one * 0.15f;
            }
        }

        private static void AddPeanutCluster(Transform root, Material material)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject peanut = AddPrimitive(root, PrimitiveType.Sphere, "Peanut", material);
                peanut.transform.localPosition = new Vector3((i - 1) * 0.17f, 0f, 0f);
                peanut.transform.localRotation = Quaternion.Euler(0f, i * 35f, 12f);
                peanut.transform.localScale = new Vector3(0.18f, 0.1f, 0.28f);
            }
        }

        private static void AddBeanCluster(Transform root, Material material)
        {
            for (int i = 0; i < 6; i++)
            {
                GameObject bean = AddPrimitive(root, PrimitiveType.Sphere, "Bean", material);
                bean.transform.localPosition = CirclePosition(i, 6, 0.19f);
                bean.transform.localScale = new Vector3(0.14f, 0.1f, 0.18f);
            }
        }

        private static void AddChiliShape(Transform root, Material material)
        {
            GameObject body = AddPrimitive(root, PrimitiveType.Capsule, "Chili", material);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 72f);
            body.transform.localScale = new Vector3(0.12f, 0.34f, 0.12f);
        }

        private static GameObject AddPrimitive(Transform parent, PrimitiveType type, string name, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);

            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return obj;
        }

        private static void ConfigureCollider(GameObject root, string id)
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = id == "Chili" ? new Vector3(0.55f, 0.55f, 0.35f) : new Vector3(0.7f, 0.45f, 0.7f);
        }

        private static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Vector3 CirclePosition(int index, int count, float radius)
        {
            float angle = count <= 0 ? 0f : index * Mathf.PI * 2f / count;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static void EnsureIngredientFolders(string root)
        {
            EnsureNestedFolder(root);
            EnsureFolder(root, "Materials");
            EnsureFolder(root, "Models");
            EnsureFolder(root, "Prefabs");
            EnsureFolder(root, "Sprites");
            EnsureFolder(root, "Textures");
        }

        private static void EnsureEnvironmentFolders(string root)
        {
            EnsureNestedFolder(root);
            EnsureFolder(root, "Materials");
            EnsureFolder(root, "Models");
            EnsureFolder(root, "Prefabs");
            EnsureFolder(root, "Textures");
        }

        private static void EnsureNestedFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
