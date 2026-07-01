using System.Collections.Generic;
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
    public bool buildOnStart = true;
    public bool buildInEditMode = true;
    private static Font cachedUIFont;
    private static readonly Dictionary<string, Material> cachedPlaceholderMaterials = new Dictionary<string, Material>();

        private void OnEnable()
        {
            if (!Application.isPlaying && Camera.main != null)
            {
                ConfigureTopDownCamera(Camera.main);
            }

            RepairRuntimeReferences();
            TryBuildInEditMode();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || !buildInEditMode)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
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
                ConfigureTopDownCamera(camera);
            }
            else
            {
                ConfigureTopDownCamera(Camera.main);
            }

            EnsureCameraRaycaster(Camera.main);

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            CreateLight();
            CreateTable();

            MortarArea mortar = CreateMortar();
            PestleController pestle = CreatePestle(mortar);
            ForceSliderController force = null;
            UIManager ui = null;
            ExperimentLogManager log = null;
            HintManager hint = gameObject.GetComponent<HintManager>() ?? gameObject.AddComponent<HintManager>();
            RecipeEvaluator evaluator = gameObject.GetComponent<RecipeEvaluator>() ?? gameObject.AddComponent<RecipeEvaluator>();
            RecipeAttemptManager attempt = gameObject.GetComponent<RecipeAttemptManager>() ?? gameObject.AddComponent<RecipeAttemptManager>();
            LevelManager levelManager = gameObject.GetComponent<LevelManager>() ?? gameObject.AddComponent<LevelManager>();

            CreateCanvas(out ui, out force, out log, pestle);
            if (ingredients.Count == 0)
            {
                ingredients = CreateRuntimeIngredients();
            }

            if (levels.Count == 0)
            {
                levels = CreateRuntimeLevels(ingredients);
            }

            CreateIngredientPlates(ingredients, mortar, attempt);

            attempt.uiManager = ui;
            attempt.forceController = force;
            attempt.pestleController = pestle;

            ui.attemptManager = attempt;
            ui.evaluator = evaluator;
            ui.hintManager = hint;
            ui.logManager = log;
            ui.levelManager = levelManager;

            levelManager.levels = levels;
            levelManager.attemptManager = attempt;
            levelManager.uiManager = ui;
            levelManager.hintManager = hint;
            levelManager.LoadLevel(0);
        }

        public void RebuildUI()
        {
            DestroyGeneratedObject("Test Level Canvas");

            PestleController pestle = FindObjectOfType<PestleController>();
            ForceSliderController force = null;
            UIManager ui = null;
            ExperimentLogManager log = null;
            HintManager hint = gameObject.GetComponent<HintManager>() ?? gameObject.AddComponent<HintManager>();
            RecipeEvaluator evaluator = gameObject.GetComponent<RecipeEvaluator>() ?? gameObject.AddComponent<RecipeEvaluator>();
            RecipeAttemptManager attempt = gameObject.GetComponent<RecipeAttemptManager>() ?? gameObject.AddComponent<RecipeAttemptManager>();
            LevelManager levelManager = gameObject.GetComponent<LevelManager>() ?? gameObject.AddComponent<LevelManager>();

            CreateCanvas(out ui, out force, out log, pestle);

            if (levels.Count == 0)
            {
                if (ingredients.Count == 0)
                {
                    ingredients = CreateRuntimeIngredients();
                }

                levels = CreateRuntimeLevels(ingredients);
            }

            attempt.uiManager = ui;
            attempt.forceController = force;
            attempt.pestleController = pestle;

            ui.attemptManager = attempt;
            ui.evaluator = evaluator;
            ui.hintManager = hint;
            ui.logManager = log;
            ui.levelManager = levelManager;

            levelManager.levels = levels;
            levelManager.attemptManager = attempt;
            levelManager.uiManager = ui;
            levelManager.hintManager = hint;
            levelManager.LoadLevel(0);
        }

        public void RepairRuntimeReferences()
        {
            RecipeAttemptManager attempt = gameObject.GetComponent<RecipeAttemptManager>();
            UIManager ui = FindObjectOfType<UIManager>();
            ForceSliderController force = FindObjectOfType<ForceSliderController>();
            PestleController pestle = FindObjectOfType<PestleController>();
            LevelManager levelManager = gameObject.GetComponent<LevelManager>();
            HintManager hint = gameObject.GetComponent<HintManager>();
            RecipeEvaluator evaluator = gameObject.GetComponent<RecipeEvaluator>();
            ExperimentLogManager log = FindObjectOfType<ExperimentLogManager>();

            if (force != null && force.forceSlider == null)
            {
                force.Bind(force.GetComponent<Slider>(), force.forceLabel);
            }

            if (attempt != null)
            {
                attempt.uiManager = ui;
                attempt.forceController = force;
                attempt.pestleController = pestle;
            }

            if (ui != null)
            {
                ui.EnsureRatioSelectionPanel();
                ui.EnsureExperimentLogButton();
                ui.attemptManager = attempt;
                ui.evaluator = evaluator;
                ui.hintManager = hint;
                ui.logManager = log;
                ui.levelManager = levelManager;
            }

            if (levelManager != null)
            {
                levelManager.attemptManager = attempt;
                levelManager.uiManager = ui;
                levelManager.hintManager = hint;
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
            camera.transform.position = new Vector3(0f, 8.5f, 0f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 3.3f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.74f, 0.68f);
        }

        private static void EnsureCameraRaycaster(Camera camera)
        {
            if (camera != null && camera.GetComponent<PhysicsRaycaster>() == null)
            {
                camera.gameObject.AddComponent<PhysicsRaycaster>();
            }
        }

        private static bool HasBuiltSceneObjects()
        {
            return GameObject.Find("Placeholder Table") != null
                && GameObject.Find("Mortar Area") != null
                && GameObject.Find("Test Level Canvas") != null;
        }

        public static List<IngredientData> CreateRuntimeIngredients()
        {
            return new List<IngredientData>
            {
                CreateIngredient("Rice", "Rice", "Rice", "clean grain", new Color(0.95f, 0.9f, 0.75f)),
                CreateIngredient("Chili", "Chili", "Chili", "spicy", new Color(0.85f, 0.1f, 0.08f)),
                CreateIngredient("Peanut", "Peanut", "Peanut", "nutty", new Color(0.72f, 0.48f, 0.27f)),
                CreateIngredient("DriedTangerinePeel", "Dried Tangerine Peel", "Dried Tangerine Peel", "citrus", new Color(0.95f, 0.45f, 0.12f))
            };
        }

        public static List<RecipeLevelData> CreateRuntimeLevels(List<IngredientData> all)
        {
            IngredientData rice = Find(all, "Rice");
            IngredientData chili = Find(all, "Chili");
            IngredientData peanut = Find(all, "Peanut");
            IngredientData peel = Find(all, "DriedTangerinePeel");

            return new List<RecipeLevelData>
            {
                Level("L01", "Free Mix Test", "Memory Kitchen", "Free Experiment", new [] { rice, chili, peanut, peel }, new IngredientData[0], ForceLevel.Medium, SpeedLevel.Medium, Mechanics(force:true), 80),
                Level("L02", "Rice Aroma Test Two", "Guangzhou", "Rice Aroma Two", new [] { rice }, new [] { rice }, ForceLevel.Medium, SpeedLevel.Slow, Mechanics(selection:true, speed:true), 80),
                Level("L03", "Stable Rice Aroma", "Guangzhou", "Stable Rice Aroma", new [] { rice }, new [] { rice }, ForceLevel.Medium, SpeedLevel.Slow, Mechanics(selection:true, force:true, speed:true), 80),
                Level("L04", "Peanut Chili Aroma", "Foshan", "Peanut Chili Aroma", new [] { peanut, chili }, new [] { peanut, chili }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(selection:true, order:true, force:true, speed:true), 80, order:new [] { peanut, chili }),
                Level("L05", "Tangerine Peel Rice Aroma", "Chaoshan", "Tangerine Peel Rice Aroma", new [] { rice, peel, peanut }, new [] { rice, peel, peanut }, ForceLevel.Light, SpeedLevel.Slow, Mechanics(selection:true, order:true, ratio:true, force:true, speed:true), 80, order:new [] { rice, peel, peanut }, ratios:new [] { Ratio(rice, RatioLevel.More), Ratio(peel, RatioLevel.SlightlyMore), Ratio(peanut, RatioLevel.Less) }),
                Level("L06", "Free Restoration Test", "Memory Kitchen", "Combined Free Mix", new [] { rice, peanut, chili, peel }, new [] { rice, peanut, chili, peel }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(selection:true, order:true, ratio:true, combination:true, force:true, speed:true, duration:true), 80, order:new [] { rice, peanut, peel, chili }, ratios:new [] { Ratio(rice, RatioLevel.More), Ratio(peanut, RatioLevel.SlightlyMore), Ratio(peel, RatioLevel.Less), Ratio(chili, RatioLevel.VeryLess) }, combination:Combination(new [] { rice, peanut }, new [] { peel }, new [] { chili }), minDuration:3f, maxDuration:6f)
            };
        }

        public static EnabledMechanics Mechanics(bool selection = false, bool order = false, bool ratio = false, bool combination = false, bool force = false, bool speed = false, bool duration = false)
        {
            return new EnabledMechanics
            {
                enableIngredientSelection = selection,
                enableIngredientOrder = order,
                enableRatio = ratio,
                enableCombination = combination,
                enableForce = force,
                enableSpeed = speed,
                enableGrindDuration = duration
            };
        }

        public static RatioRequirement Ratio(IngredientData ingredient, RatioLevel level)
        {
            return new RatioRequirement { ingredient = ingredient, ratioLevel = level };
        }

        public static CombinationPattern Combination(params IngredientData[][] groups)
        {
            CombinationPattern pattern = new CombinationPattern();
            foreach (IngredientData[] groupIngredients in groups)
            {
                CombinationGroup group = new CombinationGroup();
                group.ingredients.AddRange(groupIngredients);
                pattern.groups.Add(group);
            }

            return pattern;
        }

        public static RecipeLevelData Level(string id, string name, string city, string taste, IngredientData[] available, IngredientData[] required, ForceLevel force, SpeedLevel speed, EnabledMechanics mechanics, int passing, IngredientData[] order = null, RatioRequirement[] ratios = null, CombinationPattern combination = null, float minDuration = 3f, float maxDuration = 6f)
        {
            RecipeLevelData level = ScriptableObject.CreateInstance<RecipeLevelData>();
            level.levelID = id;
            level.levelName = name;
            level.cityName = city;
            level.targetTasteName = taste;
            level.availableIngredients.AddRange(available);
            level.requiredIngredients.AddRange(required);
            level.maxIngredientCount = Mathf.Clamp(available.Length, 1, 4);
            level.correctIngredientOrder.AddRange(order ?? required);
            if (ratios != null) level.correctRatioPattern.AddRange(ratios);
            level.correctCombinationPattern = combination ?? Combination(required);
            level.targetForceLevel = force;
            level.targetSpeedLevel = speed;
            level.enabledMechanics = mechanics;
            level.passingScore = passing;
            level.minGrindDuration = minDuration;
            level.maxGrindDuration = maxDuration;
            return level;
        }

        private static IngredientData CreateIngredient(string id, string cn, string en, string aroma, Color color)
        {
            IngredientData ingredient = ScriptableObject.CreateInstance<IngredientData>();
            ingredient.ingredientID = id;
            ingredient.ingredientNameCN = cn;
            ingredient.ingredientNameEN = en;
            ingredient.aromaType = aroma;
            ingredient.ingredientColor = color;
            return ingredient;
        }

        private static IngredientData Find(List<IngredientData> source, string id)
        {
            return source.Find(x => x != null && x.ingredientID == id);
        }

        private static void CreateLight()
        {
            if (FindObjectOfType<Light>() != null) return;
            Light light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateTable()
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Placeholder Table";
            table.transform.position = new Vector3(0f, -0.1f, 0f);
            table.transform.localScale = new Vector3(8f, 0.2f, 5f);
            ApplyColor(table, new Color(0.45f, 0.35f, 0.25f));
        }

        private static MortarArea CreateMortar()
        {
            GameObject mortar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mortar.name = "Mortar Area";
            mortar.transform.position = new Vector3(0f, 0.35f, 0.5f);
            mortar.transform.localScale = new Vector3(1.7f, 0.35f, 1.7f);
            ApplyColor(mortar, new Color(0.45f, 0.45f, 0.48f));
            return mortar.AddComponent<MortarArea>();
        }

        private static PestleController CreatePestle(MortarArea mortar)
        {
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
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            EnsureCameraRaycaster(Camera.main);
            ConfigureWorldSpaceCanvas(canvasObject);
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
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
            ui.currentIngredientsLabel = CreateText(canvas.transform, "Current Ingredients", new Vector2(300f, 76f), new Vector2(24f, -94f), topLeft, topLeft);
            ui.currentOrderLabel = CreateText(canvas.transform, "Current Order", new Vector2(300f, 68f), new Vector2(24f, -182f), topLeft, topLeft);
            ui.currentRatioLabel = CreateText(canvas.transform, "Current Ratio", new Vector2(300f, 68f), new Vector2(24f, -262f), topLeft, topLeft);
            ui.currentSpeedLabel = CreateText(canvas.transform, "Current Speed", new Vector2(300f, 42f), new Vector2(24f, -342f), topLeft, topLeft);
            ui.feedbackLabel = CreateText(canvas.transform, "Feedback", new Vector2(380f, 132f), new Vector2(-24f, -24f), topRight, topRight, TextAnchor.UpperLeft);
            ui.hintLabel = CreateText(canvas.transform, "Hint", new Vector2(380f, 64f), new Vector2(-24f, -168f), topRight, topRight, TextAnchor.UpperLeft);
            log.logText = null;

            Slider slider = CreateSlider(canvas.transform, "Force Slider", new Vector2(300f, 36f), new Vector2(24f, 56f), bottomLeft, bottomLeft);
            Text forceText = CreateText(canvas.transform, "Force Label", new Vector2(300f, 34f), new Vector2(24f, 20f), bottomLeft, bottomLeft);
            force = slider.gameObject.AddComponent<ForceSliderController>();
            force.Bind(slider, forceText);

            pestle.speedLabel = ui.currentSpeedLabel;

            Vector2 bottomRight = new Vector2(1f, 0f);
            ui.EnsureExperimentLogButton();
            CreateButton(canvas.transform, "Evaluate", new Vector2(132f, 36f), new Vector2(-24f, 144f), bottomRight, bottomRight, ui.EvaluateCurrentAttempt);
            CreateButton(canvas.transform, "Reset Attempt", new Vector2(132f, 36f), new Vector2(-24f, 104f), bottomRight, bottomRight, ui.ResetAttempt);
            CreateButton(canvas.transform, "Next Level", new Vector2(132f, 36f), new Vector2(-24f, 24f), bottomRight, bottomRight, ui.NextLevel);
        }

    private static void ConfigureWorldSpaceCanvas(GameObject canvasObject)
    {
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1366f, 768f);
        canvasObject.transform.position = new Vector3(0f, 1.35f, 0f);
        canvasObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.008f;
    }

    private static Text CreateText(Transform parent, string name, Vector2 size, Vector2 position, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        GameObject panel = CreatePanel(parent, name + " Panel", size, position);
        Text text = CreateTextChild(panel.transform, name, Vector2.zero, Vector2.zero, anchor);
        text.fontSize = 15;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = name;
        return text;
    }

    private static Text CreateText(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        GameObject panel = CreatePanel(parent, name + " Panel", size, position, anchorPoint, pivot);
        Text text = CreateTextChild(panel.transform, name, Vector2.zero, Vector2.zero, anchor);
        text.fontSize = 15;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = name;
        return text;
    }

    private static Text CreateTextChild(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name + " Text");
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = textObject.AddComponent<Text>();
        text.font = GetDefaultUIFont();
        text.color = Color.black;
        text.alignment = anchor;
        return text;
    }

    private static Font GetDefaultUIFont()
    {
        if (cachedUIFont != null)
        {
            return cachedUIFont;
        }

        try
        {
            cachedUIFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (System.ArgumentException)
        {
            cachedUIFont = null;
        }

        if (cachedUIFont != null)
        {
            return cachedUIFont;
        }

        cachedUIFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial", "Helvetica" }, 14);
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
            image.color = new Color(1f, 1f, 1f, 0.82f);
            return panel;
        }

    private static void CreateButton(Transform parent, string label, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreatePanel(parent, label, size, position);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        Text text = CreateTextChild(buttonObject.transform, label, new Vector2(6f, 0f), new Vector2(-6f, 0f), TextAnchor.MiddleCenter);
        text.fontSize = 14;
        text.text = label;
    }

    private static void CreateButton(Transform parent, string label, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreatePanel(parent, label, size, position, anchorPoint, pivot);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        Text text = CreateTextChild(buttonObject.transform, label, new Vector2(6f, 0f), new Vector2(-6f, 0f), TextAnchor.MiddleCenter);
        text.fontSize = 14;
        text.text = label;
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
            fill.AddComponent<Image>().color = new Color(0.35f, 0.65f, 0.95f);

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
            handleRect.sizeDelta = new Vector2(22f, 32f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

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
