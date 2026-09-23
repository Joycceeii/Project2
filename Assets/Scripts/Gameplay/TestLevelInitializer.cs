using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheTasteReviver
{
[ExecuteAlways]
public class TestLevelInitializer : MonoBehaviour
{
    public List<IngredientData> ingredients = new List<IngredientData>();
    public List<RecipeLevelData> levels = new List<RecipeLevelData>();
    public bool buildOnStart = false;
    public bool buildInEditMode = true;

    [Header("Layout Control")]
    [Tooltip("Off lets you move Camera and GrindingTable directly in the Scene view without this script overwriting them.")]
    public bool driveSceneLayoutFromInspector = false;

    [Header("Camera Layout")]
    public Vector3 gameplayCameraPosition = new Vector3(0f, 6.45f, -4.85f);
    public Vector3 gameplayCameraEulerAngles = new Vector3(55f, 0f, 0f);
    public float gameplayCameraOrthographicSize = 3.85f;
    public Color gameplayCameraBackgroundColor = new Color(0.78f, 0.74f, 0.68f);

    [Header("Table Layout")]
    public float grindingTableScale = 1.55f;
    public float grindingTabletopY = 0.35f;
    public Vector3 grindingTablePositionOffset = Vector3.zero;

    private static Font cachedUIFont;
    private static readonly Dictionary<string, Material> cachedPlaceholderMaterials = new Dictionary<string, Material>();
#if UNITY_EDITOR
    private bool inspectorLayoutQueued;
#endif

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("TheTasteReviver.TmpMigration", false))
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }
#endif
            RepairRuntimeReferences();
            TryBuildInEditMode();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            QueueInspectorLayoutRefresh();
        }

        private void QueueInspectorLayoutRefresh()
        {
            if (inspectorLayoutQueued)
            {
                return;
            }

            inspectorLayoutQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                inspectorLayoutQueued = false;
                if (this == null)
                {
                    return;
                }

                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                ApplyInspectorLayout();
                if (!buildInEditMode || HasBuiltSceneObjects())
                {
                    return;
                }

                TryBuildInEditMode();
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            };
        }
#endif

        private void Start()
        {
            RepairRuntimeReferences();

            if (buildOnStart)
            {
                BuildScene();
            }
        }

        public void BuildScene()
        {
            if (Camera.main == null)
            {
                Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                ConfigureSceneCamera(camera);
            }
            else if (!HasBuiltSceneObjects())
            {
                ConfigureSceneCamera(Camera.main);
            }

            EnsureCameraRaycaster(Camera.main);

            EnsureSingleEventSystem();

            DestroyLegacyGeneratedObjects();
            CreateLight();
            CreateTable();

            MortarArea mortar = CreateMortar();
            PestleController pestle = CreatePestle(mortar);
            ForceSliderController force = null;
            UIManager ui = null;
            ExperimentLogManager log = null;
            HintManager hint = GetOrAddComponent<HintManager>(gameObject);
            RecipeEvaluator evaluator = GetOrAddComponent<RecipeEvaluator>(gameObject);
            RecipeAttemptManager attempt = GetOrAddComponent<RecipeAttemptManager>(gameObject);
            LevelManager levelManager = GetOrAddComponent<LevelManager>(gameObject);
            LevelIngredientDisplayManager ingredientDisplay = GetOrAddComponent<LevelIngredientDisplayManager>(gameObject);

            CreateCanvas(out ui, out force, out log, pestle);
            EnsureAssignedData();
            ExperimentLogManager.SetIngredientCatalog(ingredients);
            ExperimentLogManager.SetLevelCatalog(levels);

            attempt.uiManager = ui;
            attempt.forceController = force;
            attempt.pestleController = pestle;
            attempt.ingredientDisplayManager = ingredientDisplay;

            ingredientDisplay.mortarArea = mortar;
            ingredientDisplay.attemptManager = attempt;

            ui.attemptManager = attempt;
            ui.evaluator = evaluator;
            ui.hintManager = hint;
            ui.logManager = log;
            ui.levelManager = levelManager;

            levelManager.levels = levels;
            levelManager.attemptManager = attempt;
            levelManager.uiManager = ui;
            levelManager.hintManager = hint;
            levelManager.ingredientDisplayManager = ingredientDisplay;
            levelManager.LoadLevel(0);
        }

        public void RebuildUI()
        {
            DestroyGeneratedObject("Test Level Canvas");

            PestleController pestle = FindFirstObjectByType<PestleController>();
            ForceSliderController force = null;
            UIManager ui = null;
            ExperimentLogManager log = null;
            HintManager hint = GetOrAddComponent<HintManager>(gameObject);
            RecipeEvaluator evaluator = GetOrAddComponent<RecipeEvaluator>(gameObject);
            RecipeAttemptManager attempt = GetOrAddComponent<RecipeAttemptManager>(gameObject);
            LevelManager levelManager = GetOrAddComponent<LevelManager>(gameObject);
            LevelIngredientDisplayManager ingredientDisplay = GetOrAddComponent<LevelIngredientDisplayManager>(gameObject);

            CreateCanvas(out ui, out force, out log, pestle);

            EnsureAssignedData();
            ExperimentLogManager.SetIngredientCatalog(ingredients);
            ExperimentLogManager.SetLevelCatalog(levels);

            attempt.uiManager = ui;
            attempt.forceController = force;
            attempt.pestleController = pestle;
            attempt.ingredientDisplayManager = ingredientDisplay;

            MortarArea mortar = ResolvePrimaryMortarArea(ingredientDisplay, pestle);
            if (pestle != null)
            {
                pestle.mortarArea = mortar;
            }

            ingredientDisplay.mortarArea = mortar;
            ingredientDisplay.attemptManager = attempt;

            ui.attemptManager = attempt;
            ui.evaluator = evaluator;
            ui.hintManager = hint;
            ui.logManager = log;
            ui.levelManager = levelManager;

            levelManager.levels = levels;
            levelManager.attemptManager = attempt;
            levelManager.uiManager = ui;
            levelManager.hintManager = hint;
            levelManager.ingredientDisplayManager = ingredientDisplay;
            levelManager.LoadLevel(0);
            ApplyMortarToDraggableIngredients(mortar, attempt);
        }

        public void RepairRuntimeReferences()
        {
            RecipeAttemptManager attempt = gameObject.GetComponent<RecipeAttemptManager>();
            UIManager ui = FindFirstObjectByType<UIManager>();
            ForceSliderController force = FindFirstObjectByType<ForceSliderController>();
            PestleController pestle = FindFirstObjectByType<PestleController>();
            LevelManager levelManager = gameObject.GetComponent<LevelManager>();
            LevelIngredientDisplayManager ingredientDisplay = GetOrAddComponent<LevelIngredientDisplayManager>(gameObject);
            HintManager hint = gameObject.GetComponent<HintManager>();
            RecipeEvaluator evaluator = gameObject.GetComponent<RecipeEvaluator>();
            ExperimentLogManager log = FindFirstObjectByType<ExperimentLogManager>();
            MortarArea mortar = ResolvePrimaryMortarArea(ingredientDisplay, pestle);

            ApplyInspectorLayout();
            EnsureAssignedData();
            ExperimentLogManager.SetIngredientCatalog(ingredients);
            ExperimentLogManager.SetLevelCatalog(levels);

            if (force != null && force.forceSlider == null)
            {
                force.Bind(force.GetComponent<Slider>(), force.forceLabel);
            }

            if (force != null)
            {
                force.uiManager = ui;
            }

            if (pestle != null)
            {
                pestle.uiManager = ui;
                pestle.mortarArea = mortar;
            }

            if (attempt != null)
            {
                attempt.uiManager = ui;
                attempt.forceController = force;
                attempt.pestleController = pestle;
                attempt.ingredientDisplayManager = ingredientDisplay;
            }

            if (ui != null)
            {
                Canvas canvas = ui.GetComponent<Canvas>();
                if (canvas != null)
                {
                    ConfigureGameplayCanvas(canvas);
                }

                ui.EnsureRatioSelectionPanel();
                ui.EnsureExperimentLogButton();
                ui.EnsureActionButtons();
                ui.EnsureIngredientTraitPanel();
                if (ui.driveHudLayoutFromInspector)
                {
                    ui.NormalizeHudLayout();
                }
                ui.attemptManager = attempt;
                ui.evaluator = evaluator;
                ui.hintManager = hint;
                ui.logManager = log;
                ui.levelManager = levelManager;
            }

            if (levelManager != null)
            {
                levelManager.levels = levels;
                levelManager.attemptManager = attempt;
                levelManager.uiManager = ui;
                levelManager.hintManager = hint;
                levelManager.ingredientDisplayManager = ingredientDisplay;
            }

            if (ingredientDisplay != null)
            {
                ingredientDisplay.mortarArea = mortar;
                ingredientDisplay.attemptManager = attempt;
                if (levelManager != null && levelManager.CurrentLevel != null)
                {
                    ingredientDisplay.ShowLevelIngredients(levelManager.CurrentLevel);
                }
            }

            ApplyMortarToDraggableIngredients(mortar, attempt);
        }

        private void EnsureAssignedData()
        {
            if (ingredients == null)
            {
                ingredients = new List<IngredientData>();
            }

            if (levels == null)
            {
                levels = new List<RecipeLevelData>();
            }

            if (ingredients.Count == 0)
            {
                Debug.LogError("No ingredient assets assigned. Import design data into Assets/Data/GeneratedAssets and assign those assets in the scene.");
            }

            if (levels.Count == 0)
            {
                Debug.LogError("No level assets assigned. Import design data into Assets/Data/GeneratedAssets and assign those assets in the scene.");
            }
        }

        private static void DestroyGeneratedObject(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing == null)
            {
                return;
            }

#if UNITY_EDITOR
            UnityEngine.SceneManagement.Scene scene = existing.scene;
#endif

            if (Application.isPlaying)
            {
                Destroy(existing);
            }
            else
            {
                DestroyImmediate(existing);
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
#endif
            }
        }

        private void TryBuildInEditMode()
        {
            if (Application.isPlaying || !buildInEditMode || HasBuiltSceneObjects())
            {
                return;
            }

            buildOnStart = false;
            BuildScene();
        }

        public static void ConfigureTopDownCamera(Camera camera)
        {
            ConfigureDefaultCamera(camera);
        }

        public static void ConfigureDefaultCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(0f, 6.45f, -4.85f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 3.85f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.74f, 0.68f);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(camera);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            }
#endif
        }

        private void ApplyInspectorLayout()
        {
            if (!driveSceneLayoutFromInspector)
            {
                return;
            }

            ConfigureSceneCamera(Camera.main);
            ConfigureGrindingTable();
        }

        private void ConfigureSceneCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.transform.position = gameplayCameraPosition;
            camera.transform.rotation = Quaternion.Euler(gameplayCameraEulerAngles);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(0.1f, gameplayCameraOrthographicSize);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = gameplayCameraBackgroundColor;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(camera);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            }
#endif
        }

        private void ConfigureGrindingTable()
        {
            GameObject table = GameObject.Find("GrindingTable");
            if (table == null)
            {
                return;
            }

            float tableScale = Mathf.Max(0.1f, grindingTableScale);
            float tabletopY = grindingTabletopY;
            table.transform.localScale = Vector3.one * tableScale;

            Transform anchor = table.transform.Find("GrindingBowlAnchor");
            float rootY = table.transform.position.y;
            if (anchor != null)
            {
                rootY = tabletopY - anchor.localPosition.y * tableScale;
            }

            table.transform.position = new Vector3(0f, rootY, 0f) + grindingTablePositionOffset;
            table.transform.rotation = Quaternion.identity;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(table);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(table.scene);
            }
#endif
        }

        private static void EnsureCameraRaycaster(Camera camera)
        {
            if (camera != null && camera.GetComponent<PhysicsRaycaster>() == null)
            {
                camera.gameObject.AddComponent<PhysicsRaycaster>();
            }
        }

        private static void EnsureSingleEventSystem()
        {
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (eventSystems.Length == 0)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                return;
            }

            EventSystem keep = EventSystem.current != null ? EventSystem.current : eventSystems[0];
            foreach (EventSystem eventSystem in eventSystems)
            {
                if (eventSystem == keep)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(eventSystem.gameObject);
                }
                else
                {
                    DestroyImmediate(eventSystem.gameObject);
                }
            }
        }

        private static bool HasBuiltSceneObjects()
        {
            return HasTableObject()
                && GameObject.Find("Mortar Area") != null
                && GameObject.Find("Test Level Canvas") != null;
        }

        private static bool HasTableObject()
        {
            return GameObject.Find("GrindingTable") != null || GameObject.Find("Placeholder Table") != null;
        }

        private static void DestroyLegacyGeneratedObjects()
        {
            if (GameObject.Find("GrindingTable") != null)
            {
                DestroyGeneratedObject("Placeholder Table");
            }

            if (GameObject.Find("GrindingPestle") != null)
            {
                DestroyGeneratedObject("Pestle");
            }
        }

        private static void CreateLight()
        {
            if (FindFirstObjectByType<Light>() != null) return;
            Light light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateTable()
        {
            if (HasTableObject())
            {
                return;
            }

            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Placeholder Table";
            table.transform.position = new Vector3(0f, -0.1f, 0f);
            table.transform.localScale = new Vector3(8f, 0.2f, 5f);
            ApplyColor(table, new Color(0.45f, 0.35f, 0.25f));
        }

        private static MortarArea CreateMortar()
        {
            MortarArea existing = ResolvePrimaryMortarArea(null, FindFirstObjectByType<PestleController>());
            if (existing != null)
            {
                return existing;
            }

            GameObject mortar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mortar.name = "Mortar Area";
            mortar.transform.position = new Vector3(0f, 0.35f, 0.5f);
            mortar.transform.localScale = new Vector3(1.7f, 0.35f, 1.7f);
            ApplyColor(mortar, new Color(0.45f, 0.45f, 0.48f));
            return mortar.AddComponent<MortarArea>();
        }

        private static MortarArea ResolvePrimaryMortarArea(LevelIngredientDisplayManager ingredientDisplay, PestleController pestle)
        {
            if (ingredientDisplay != null && ingredientDisplay.mortarArea != null && ingredientDisplay.mortarArea.gameObject.activeInHierarchy)
            {
                return ingredientDisplay.mortarArea;
            }

            if (pestle != null && pestle.mortarArea != null && pestle.mortarArea.gameObject.activeInHierarchy)
            {
                return pestle.mortarArea;
            }

            return FindObjectsByType<MortarArea>(FindObjectsSortMode.None)
                .FirstOrDefault(area => area != null && area.gameObject.activeInHierarchy);
        }

        private static void ApplyMortarToDraggableIngredients(MortarArea mortar, RecipeAttemptManager attempt)
        {
            if (mortar == null)
            {
                return;
            }

            foreach (DraggableIngredient ingredient in FindObjectsByType<DraggableIngredient>(FindObjectsSortMode.None))
            {
                if (ingredient == null)
                {
                    continue;
                }

                ingredient.mortarArea = mortar;
                ingredient.mortarDropHeight = 0.2f;
                ingredient.mortarGroundVisualLift = 0.04f;
                ingredient.groundVisualLift = 0.1f;
                ingredient.homeGroundVisualLift = 0.24f;
                ingredient.groundVisualScale = 0.8f;
                ingredient.groundVisualFootprint = 0.7f;
                ingredient.groundVisualMaxHeight = 0.14f;
                if (attempt != null)
                {
                    ingredient.attemptManager = attempt;
                }
            }
        }

        private static PestleController CreatePestle(MortarArea mortar)
        {
            PestleController existing = FindFirstObjectByType<PestleController>();
            if (existing != null)
            {
                existing.mortarArea = mortar;
                return existing;
            }

            GameObject pestle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pestle.name = "Pestle";
            pestle.transform.position = new Vector3(0.3f, 1.1f, 0.5f);
            pestle.transform.rotation = Quaternion.Euler(0f, 0f, 35f);
            pestle.transform.localScale = new Vector3(0.18f, 1.1f, 0.18f);
            ApplyColor(pestle, new Color(0.72f, 0.72f, 0.76f));
            PestleController controller = pestle.AddComponent<PestleController>();
            controller.mortarArea = mortar;
            return controller;
        }

        private static void CreateIngredientPlates(List<IngredientData> data, MortarArea mortar, RecipeAttemptManager attempt)
        {
            for (int i = 0; i < data.Count; i++)
            {
                IngredientData ingredient = data[i];
                string displayName = ingredient != null ? ingredient.DisplayName : "Unknown";
                GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plate.name = displayName + " Plate";
                plate.transform.position = new Vector3(-3f + i * 2f, 0.15f, -1.6f);
                plate.transform.localScale = new Vector3(0.75f, 0.08f, 0.75f);
                ApplyColor(plate, Color.white);

                GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                item.name = displayName + " Ingredient";
                item.transform.position = plate.transform.position + Vector3.up * 0.45f;
                item.transform.localScale = Vector3.one * 0.38f;
                ApplyColor(item, ingredient.ingredientColor);
                DraggableIngredient drag = item.AddComponent<DraggableIngredient>();
                drag.ingredientData = ingredient;
                drag.mortarArea = mortar;
                drag.attemptManager = attempt;
                drag.mortarDropHeight = 0.2f;
                drag.mortarGroundVisualLift = 0.04f;
                drag.groundVisualLift = 0.1f;
                drag.homeGroundVisualLift = 0.24f;
                drag.groundVisualScale = 0.8f;
                drag.groundVisualFootprint = 0.7f;
                drag.groundVisualMaxHeight = 0.14f;
            }
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                renderer.material.color = color;
                return;
            }

            renderer.sharedMaterial = GetPlaceholderMaterial(color);
        }

        private static Material GetPlaceholderMaterial(Color color)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color);
            if (cachedPlaceholderMaterials.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = "Placeholder_" + key;
            material.color = color;
            cachedPlaceholderMaterials[key] = material;
            return material;
        }

        private static void CreateCanvas(out UIManager ui, out ForceSliderController force, out ExperimentLogManager log, PestleController pestle)
        {
            GameObject canvasObject = new GameObject("Test Level Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            EnsureCameraRaycaster(Camera.main);
            ConfigureGameplayCanvas(canvas);
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            ui = canvasObject.AddComponent<UIManager>();
            log = canvasObject.AddComponent<ExperimentLogManager>();
            ui.EnsureRatioSelectionPanel();

            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 topRight = new Vector2(1f, 1f);
            Vector2 bottomLeft = new Vector2(0f, 0f);

            ui.levelLabel = CreateText(canvas.transform, "Level", new Vector2(280f, 58f), new Vector2(24f, -24f), topLeft, topLeft);
            ui.currentOrderLabel = CreateText(canvas.transform, "Current Order", new Vector2(300f, 68f), new Vector2(24f, -104f), topLeft, topLeft);
            ui.currentRatioLabel = CreateText(canvas.transform, "Current Ratio", new Vector2(300f, 68f), new Vector2(24f, -184f), topLeft, topLeft);
            ui.currentSpeedLabel = CreateText(canvas.transform, "Current Speed", new Vector2(300f, 42f), new Vector2(24f, -264f), topLeft, topLeft);
            ui.hintLabel = CreateText(canvas.transform, "Hint", new Vector2(380f, 160f), new Vector2(-24f, -24f), topRight, topRight, TextAnchor.UpperLeft);
            ui.EnsureIngredientTraitPanel();
            log.logText = null;

            Slider slider = CreateSlider(canvas.transform, "Force Slider", new Vector2(300f, 36f), new Vector2(24f, 56f), bottomLeft, bottomLeft);
            TMP_Text forceText = CreateText(canvas.transform, "Force Label", new Vector2(300f, 34f), new Vector2(24f, 20f), bottomLeft, bottomLeft);
            force = slider.gameObject.AddComponent<ForceSliderController>();
            force.Bind(slider, forceText);
            force.uiManager = ui;

            pestle.speedLabel = ui.currentSpeedLabel;
            pestle.uiManager = ui;

            Vector2 bottomRight = new Vector2(1f, 0f);
            ui.EnsureExperimentLogButton();
            ui.evaluateButton = CreateButton(canvas.transform, "Evaluate", new Vector2(132f, 36f), new Vector2(-24f, 144f), bottomRight, bottomRight, ui.EvaluateCurrentAttempt);
            ui.resetAttemptButton = CreateButton(canvas.transform, "Reset Attempt", new Vector2(132f, 36f), new Vector2(-24f, 104f), bottomRight, bottomRight, ui.ResetAttempt);
            ui.newBatchButton = CreateButton(canvas.transform, "New Batch", new Vector2(132f, 36f), new Vector2(-24f, 64f), bottomRight, bottomRight, ui.StartNewBatch);
            ui.nextLevelButton = CreateButton(canvas.transform, "Next Level", new Vector2(132f, 36f), new Vector2(-24f, 24f), bottomRight, bottomRight, ui.NextLevel);
            ui.EnsureActionButtons();
            ui.EnsureIngredientTraitPanel();
            if (ui.driveHudLayoutFromInspector)
            {
                ui.NormalizeHudLayout();
            }
        }

        private static void ConfigureGameplayCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.planeDistance = 100f;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            Transform canvasTransform = canvas.transform;
            canvasTransform.localPosition = Vector3.zero;
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.localScale = Vector3.one;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.pivot = new Vector2(0.5f, 0.5f);
                canvasRect.anchoredPosition = Vector2.zero;
                canvasRect.sizeDelta = Vector2.zero;
            }
        }

        private static void ConfigureWorldSpaceCanvas(GameObject canvasObject)
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                ConfigureGameplayCanvas(canvas);
            }
        }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 size, Vector2 position, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        GameObject panel = CreatePanel(parent, name + " Panel", size, position);
        TMP_Text text = CreateTextChild(panel.transform, name, GetTextInsetMin(anchor), GetTextInsetMax(anchor), anchor);
        text.fontSize = 20;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.text = name;
        return text;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        GameObject panel = CreatePanel(parent, name + " Panel", size, position, anchorPoint, pivot);
        TMP_Text text = CreateTextChild(panel.transform, name, GetTextInsetMin(anchor), GetTextInsetMax(anchor), anchor);
        text.fontSize = 20;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.text = name;
        return text;
    }

    private static Vector2 GetTextInsetMin(TextAnchor anchor)
    {
        bool leftAligned = anchor == TextAnchor.UpperLeft || anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.LowerLeft;
        float horizontalInset = leftAligned ? 52f : 60f;
        float bottomInset = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.MiddleCenter ? 18f : 28f;
        return new Vector2(horizontalInset, bottomInset);
    }

    private static Vector2 GetTextInsetMax(TextAnchor anchor)
    {
        Vector2 min = GetTextInsetMin(anchor);
        bool upperAligned = anchor == TextAnchor.UpperLeft || anchor == TextAnchor.UpperCenter || anchor == TextAnchor.UpperRight;
        float topInset = upperAligned ? 42f : 24f;
        return new Vector2(-min.x, -topInset);
    }

    private static TMP_Text CreateTextChild(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name + " Text");
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = ThemeFontProvider.GetTmpFont(14);
        text.color = Color.black;
        text.alignment = TmpTextUtility.ToAlignment(anchor);
        return text;
    }

    private static Font GetDefaultUIFont()
    {
        if (cachedUIFont != null)
        {
            return cachedUIFont;
        }

        cachedUIFont = ThemeFontProvider.GetFont(14);
        return cachedUIFont;
    }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 position)
        {
            return CreatePanel(parent, name, size, position, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = panel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(image, 0.82f);
            return panel;
        }

    private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreatePanel(parent, label, size, position);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        TMP_Text text = CreateTextChild(buttonObject.transform, label, new Vector2(6f, 0f), new Vector2(-6f, 0f), TextAnchor.MiddleCenter);
        text.fontSize = 22;
        text.fontStyle = FontStyles.Bold;
        text.text = label;
        return button;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreatePanel(parent, label, size, position, anchorPoint, pivot);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        TMP_Text text = CreateTextChild(buttonObject.transform, label, new Vector2(6f, 0f), new Vector2(-6f, 0f), TextAnchor.MiddleCenter);
        text.fontSize = 22;
        text.fontStyle = FontStyles.Bold;
        text.text = label;
        return button;
    }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 size, Vector2 position)
        {
            return CreateSlider(parent, name, size, position, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot)
        {
            GameObject root = CreatePanel(parent, name, size, position, anchorPoint, pivot);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.86f, 0.86f, 0.86f, 0.95f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(8f, 0f);
            fillAreaRect.offsetMax = new Vector2(-8f, 0f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.72f, 0.52f, 0.32f, 0.82f);

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(root.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(34f, 56f);
            Image handleImage = handle.AddComponent<Image>();
            PanelBackgroundStyle.Apply(handleImage);

            Slider slider = root.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            return slider;
        }
    }
}
