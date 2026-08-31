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
        private const string BowlModelPath = "Assets/Art/Environment/Mortar/Models/lod_basic_pbr.fbx";
        private const string ImportedBowlMaterialPath = "Assets/Art/Environment/Mortar/Materials/M_Imported_Bowl_Stone.mat";
        private const string GrindingPestleRoot = "Assets/Art/Tools/GrindingPestle";
        private const string GrindingPestleModelPath = "Assets/Art/Tools/GrindingPestle/Models/lod_basic_pbr.fbx";
        private const string GrindingPestleMaterialPath = "Assets/Art/Tools/GrindingPestle/Materials/MAT_GrindingPestle.mat";
        private const string GrindingTableRoot = "Assets/Art/Tools/GrindingTable";
        private const string GrindingTableModelPath = "Assets/Art/Tools/GrindingTable/Models/lod_basic_pbr.fbx";
        private const string GrindingTableMaterialPath = "Assets/Art/Tools/GrindingTable/Materials/MAT_GrindingTable.mat";
        private const string IngredientPlateRoot = "Assets/Art/Environment/IngredientPlate";
        private const string IngredientPlateModelPath = "Assets/Art/Environment/IngredientPlate/Models/lod_basic_pbr.fbx";
        private const string IngredientPlateMaterialPath = "Assets/Art/Environment/IngredientPlate/Materials/MAT_IngredientPlate.mat";
        private const string GlutinousRiceModelPath = "Assets/Art/Ingredients/GlutinousRice/Models/lod_basic_pbr.fbx";
        private const string YangjiangDouchiModelPath = "Assets/Art/Ingredients/YangjiangDouchi/Models/lod_basic_pbr.fbx";
        private const string RockSugarModelPath = "Assets/Art/Ingredients/RockSugar/Models/lod_basic_pbr.fbx";

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
            CreateGrindingTablePrefab();
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

            if (!TryBuildImportedIngredientVisual(root.transform, ingredient.ingredientID, material))
            {
                BuildVisual(root.transform, ingredient.ingredientID, material);
            }

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

            GameObject root = new GameObject("PF_Mortar");

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BowlModelPath);
            if (model != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Mortar Bowl";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * 1.18422f;

                Material importedStone = AssetDatabase.LoadAssetAtPath<Material>(ImportedBowlMaterialPath);
                if (importedStone != null)
                {
                    ApplyMaterialToRenderers(visual, importedStone);
                }
            }
            else
            {
                Debug.LogWarning("Imported bowl model was not found. Mortar prefab was created without a visual.");
            }

            SavePrefab(root, rootFolder + "/Prefabs/PF_Mortar.prefab");
        }

        private static void CreatePestlePrefab()
        {
            EnsureNestedFolder(GrindingPestleRoot);
            EnsureFolder(GrindingPestleRoot, "Materials");
            EnsureFolder(GrindingPestleRoot, "Models");
            EnsureFolder(GrindingPestleRoot, "Prefabs");
            EnsureFolder(GrindingPestleRoot, "Textures");

            Material stone = AssetDatabase.LoadAssetAtPath<Material>(GrindingPestleMaterialPath);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GrindingPestleModelPath);
            GameObject root = new GameObject("GrindingPestle");
            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(root.transform, false);

            if (model != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "GrindingPestle_LOD0";
                visual.transform.SetParent(visualRoot.transform, false);
                visual.transform.localScale = Vector3.one * 0.92260665f;
                visual.transform.localPosition = new Vector3(-0.0003530383f, -0.23870549f, -0.0002593696f);
                if (stone != null)
                {
                    ApplyMaterialToRenderers(visual, stone);
                }
            }
            else
            {
                Debug.LogWarning("GrindingPestle model was not found. Prefab was created without a visual.");
            }

            GameObject grip = new GameObject("GripPoint");
            grip.transform.SetParent(root.transform, false);
            grip.transform.localPosition = Vector3.up * 0.72f;

            GameObject tip = new GameObject("GrindingTip");
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = Vector3.down * 0.72f;

            root.AddComponent<PestleController>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = 0.16f;
            collider.height = 1.85f;
            SavePrefab(root, GrindingPestleRoot + "/Prefabs/GrindingPestle.prefab");
        }

        private static void CreateGrindingTablePrefab()
        {
            EnsureNestedFolder(GrindingTableRoot);
            EnsureFolder(GrindingTableRoot, "Materials");
            EnsureFolder(GrindingTableRoot, "Models");
            EnsureFolder(GrindingTableRoot, "Prefabs");
            EnsureFolder(GrindingTableRoot, "Textures");

            Material wood = AssetDatabase.LoadAssetAtPath<Material>(GrindingTableMaterialPath);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GrindingTableModelPath);
            GameObject root = new GameObject("GrindingTable");
            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(root.transform, false);

            Bounds visualBounds = new Bounds(Vector3.zero, new Vector3(5.6f, 0.9f, 3.5f));
            if (model != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "GrindingTable_LOD0";
                visual.transform.SetParent(visualRoot.transform, false);

                Bounds sourceBounds = CalculateRendererBounds(visual);
                if (sourceBounds.size.x > 0f && sourceBounds.size.z > 0f)
                {
                    float scale = Mathf.Min(5.6f / sourceBounds.size.x, 3.5f / sourceBounds.size.z);
                    visual.transform.localScale = Vector3.one * scale;
                    visual.transform.localPosition = new Vector3(
                        -sourceBounds.center.x * scale,
                        -sourceBounds.min.y * scale,
                        -sourceBounds.center.z * scale);
                    visualBounds = new Bounds(
                        new Vector3(0f, sourceBounds.size.y * scale * 0.5f, 0f),
                        sourceBounds.size * scale);
                }

                if (wood != null)
                {
                    ApplyMaterialToRenderers(visual, wood);
                }
            }
            else
            {
                Debug.LogWarning("GrindingTable model was not found. Prefab was created with colliders and anchors only.");
            }

            float tabletopTop = Mathf.Max(visualBounds.max.y, 0.75f);
            float tabletopThickness = Mathf.Clamp(visualBounds.size.y * 0.16f, 0.12f, 0.22f);
            float width = Mathf.Max(visualBounds.size.x, 5.6f);
            float depth = Mathf.Max(visualBounds.size.z, 3.5f);

            GameObject colliders = new GameObject("Colliders");
            colliders.transform.SetParent(root.transform, false);
            AddBoxCollider(colliders.transform, "TabletopCollider", new Vector3(0f, tabletopTop - tabletopThickness * 0.5f, 0f), new Vector3(width, tabletopThickness, depth));

            float legHeight = Mathf.Max(tabletopTop - tabletopThickness, 0.45f);
            float legInsetX = width * 0.42f;
            float legInsetZ = depth * 0.42f;
            Vector3 legSize = new Vector3(0.18f, legHeight, 0.18f);
            AddBoxCollider(colliders.transform, "LegCollider_FL", new Vector3(-legInsetX, legHeight * 0.5f, legInsetZ), legSize);
            AddBoxCollider(colliders.transform, "LegCollider_FR", new Vector3(legInsetX, legHeight * 0.5f, legInsetZ), legSize);
            AddBoxCollider(colliders.transform, "LegCollider_BL", new Vector3(-legInsetX, legHeight * 0.5f, -legInsetZ), legSize);
            AddBoxCollider(colliders.transform, "LegCollider_BR", new Vector3(legInsetX, legHeight * 0.5f, -legInsetZ), legSize);

            GameObject bowlAnchor = new GameObject("GrindingBowlAnchor");
            bowlAnchor.transform.SetParent(root.transform, false);
            bowlAnchor.transform.localPosition = new Vector3(0f, tabletopTop + 0.03f, 0f);

            GameObject pestleRest = new GameObject("PestleRestPoint");
            pestleRest.transform.SetParent(root.transform, false);
            pestleRest.transform.localPosition = new Vector3(width * 0.28f, tabletopTop + 0.05f, -depth * 0.22f);

            SavePrefab(root, GrindingTableRoot + "/Prefabs/GrindingTable.prefab");
        }

        private static void CreatePlatePrefab()
        {
            EnsureNestedFolder(IngredientPlateRoot);
            EnsureFolder(IngredientPlateRoot, "Materials");
            EnsureFolder(IngredientPlateRoot, "Models");
            EnsureFolder(IngredientPlateRoot, "Prefabs");
            EnsureFolder(IngredientPlateRoot, "Textures");

            Material ceramic = AssetDatabase.LoadAssetAtPath<Material>(IngredientPlateMaterialPath);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(IngredientPlateModelPath);
            GameObject root = new GameObject("IngredientPlate");

            if (model != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "IngredientPlate_LOD0";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                RemoveLodGroups(visual);

                if (ceramic != null)
                {
                    ApplyMaterialToRenderers(visual, ceramic);
                }

                FitVisualToFootprint(root.transform, visual.transform, 0.92f);
            }
            else
            {
                Material fallback = CreateOrUpdateMaterial("MAT_IngredientPlate_Fallback", Color.white, IngredientPlateRoot + "/Materials/MAT_IngredientPlate_Fallback.mat");
                GameObject plate = AddPrimitive(root.transform, PrimitiveType.Cylinder, "Plate", fallback);
                plate.transform.localScale = new Vector3(0.75f, 0.08f, 0.75f);
                Debug.LogWarning("IngredientPlate model was not found. Prefab was created with a fallback cylinder.");
            }

            SavePrefab(root, IngredientPlateRoot + "/Prefabs/IngredientPlate.prefab");
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

        private static bool TryBuildImportedIngredientVisual(Transform root, string id, Material material)
        {
            if (id == "GlutinousRice")
            {
                return TryBuildImportedIngredientVisual(
                    root,
                    material,
                    GlutinousRiceModelPath,
                    "GlutinousRice_LOD0",
                    0.68f,
                    "Assets/Art/Ingredients/GlutinousRice/Textures");
            }

            if (id == "YangjiangDouchi")
            {
                return TryBuildImportedIngredientVisual(
                    root,
                    material,
                    YangjiangDouchiModelPath,
                    "YangjiangDouchi_LOD0",
                    0.72f,
                    "Assets/Art/Ingredients/YangjiangDouchi/Textures");
            }

            if (id == "RockSugar")
            {
                return TryBuildImportedIngredientVisual(
                    root,
                    material,
                    RockSugarModelPath,
                    "RockSugar_LOD0",
                    0.74f,
                    "Assets/Art/Ingredients/RockSugar/Textures");
            }

            return false;
        }

        private static bool TryBuildImportedIngredientVisual(
            Transform root,
            Material material,
            string modelPath,
            string visualName,
            float targetDiameter,
            string textureRoot)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                return false;
            }

            ConfigureImportedIngredientMaterial(material, textureRoot);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = visualName;
            visual.transform.SetParent(root, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            RemoveLodGroups(visual);
            ApplyMaterialToRenderers(visual, material);
            FitVisualToFootprint(root, visual.transform, targetDiameter);
            return true;
        }

        private static void ConfigureImportedIngredientMaterial(Material material, string textureRoot)
        {
            if (material == null)
            {
                return;
            }

            SetMaterialTexture(material, "_BaseMap", textureRoot + "/texture_diffuse.png");
            SetMaterialTexture(material, "_BumpMap", textureRoot + "/texture_normal.png");
            SetMaterialTexture(material, "_MetallicGlossMap", textureRoot + "/texture_pbr.png");
            SetMaterialTexture(material, "_OcclusionMap", textureRoot + "/texture_pbr.png");
            material.color = Color.white;
            material.SetFloat("_Smoothness", 0.38f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
        }

        private static void SetMaterialTexture(Material material, string propertyName, string path)
        {
            if (material == null || !material.HasProperty(propertyName))
            {
                return;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                material.SetTexture(propertyName, texture);
            }
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

        private static void RemoveLodGroups(GameObject root)
        {
            foreach (LODGroup lodGroup in root.GetComponentsInChildren<LODGroup>(true))
            {
                Object.DestroyImmediate(lodGroup);
            }
        }

        private static void FitVisualToFootprint(Transform root, Transform visual, float targetDiameter)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float largestHorizontal = Mathf.Max(bounds.size.x, bounds.size.z);
            if (largestHorizontal <= 0.0001f)
            {
                return;
            }

            float scale = targetDiameter / largestHorizontal;
            visual.localScale = Vector3.one * scale;

            renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 localCenter = root.InverseTransformPoint(bounds.center);
            Vector3 localMin = root.InverseTransformPoint(bounds.min);
            visual.localPosition -= new Vector3(localCenter.x, localMin.y, localCenter.z);
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static BoxCollider AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            BoxCollider collider = obj.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            return collider;
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
