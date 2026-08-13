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
    public bool useRuntimeTenLevelData = true;
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
                if (this == null || Application.isPlaying)
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

            EnsureSingleEventSystem();

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
            LevelIngredientDisplayManager ingredientDisplay = gameObject.GetComponent<LevelIngredientDisplayManager>() ?? gameObject.AddComponent<LevelIngredientDisplayManager>();

            CreateCanvas(out ui, out force, out log, pestle);
            EnsureRuntimeData();

            attempt.uiManager = ui;
            attempt.forceController = force;
            attempt.pestleController = pestle;

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
            HintManager hint = gameObject.GetComponent<HintManager>() ?? gameObject.AddComponent<HintManager>();
            RecipeEvaluator evaluator = gameObject.GetComponent<RecipeEvaluator>() ?? gameObject.AddComponent<RecipeEvaluator>();
            RecipeAttemptManager attempt = gameObject.GetComponent<RecipeAttemptManager>() ?? gameObject.AddComponent<RecipeAttemptManager>();
            LevelManager levelManager = gameObject.GetComponent<LevelManager>() ?? gameObject.AddComponent<LevelManager>();
            LevelIngredientDisplayManager ingredientDisplay = gameObject.GetComponent<LevelIngredientDisplayManager>() ?? gameObject.AddComponent<LevelIngredientDisplayManager>();

            CreateCanvas(out ui, out force, out log, pestle);

            EnsureRuntimeData();

            attempt.uiManager = ui;
            attempt.forceController = force;
            attempt.pestleController = pestle;

            ingredientDisplay.mortarArea = FindFirstObjectByType<MortarArea>();
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

        public void RepairRuntimeReferences()
        {
            RecipeAttemptManager attempt = gameObject.GetComponent<RecipeAttemptManager>();
            UIManager ui = FindFirstObjectByType<UIManager>();
            ForceSliderController force = FindFirstObjectByType<ForceSliderController>();
            PestleController pestle = FindFirstObjectByType<PestleController>();
            LevelManager levelManager = gameObject.GetComponent<LevelManager>();
            LevelIngredientDisplayManager ingredientDisplay = gameObject.GetComponent<LevelIngredientDisplayManager>() ?? gameObject.AddComponent<LevelIngredientDisplayManager>();
            HintManager hint = gameObject.GetComponent<HintManager>();
            RecipeEvaluator evaluator = gameObject.GetComponent<RecipeEvaluator>();
            ExperimentLogManager log = FindFirstObjectByType<ExperimentLogManager>();
            MortarArea mortar = FindFirstObjectByType<MortarArea>();

            EnsureRuntimeData();

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
                ui.EnsureActionButtons();
                ui.EnsureIngredientTraitPanel();
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
            }
        }

        private void EnsureRuntimeData()
        {
            if (ingredients == null)
            {
                ingredients = new List<IngredientData>();
            }

            if (levels == null)
            {
                levels = new List<RecipeLevelData>();
            }

            if (!useRuntimeTenLevelData)
            {
                if (ingredients.Count == 0)
                {
                    ingredients = CreateRuntimeIngredients();
                }

                if (levels.Count == 0)
                {
                    levels = CreateRuntimeLevels(ingredients);
                }

                return;
            }

            SyncRuntimeIngredientMetadata();

            if (!HasExpectedRuntimeIngredients())
            {
                ingredients = CreateRuntimeIngredients();
            }

            levels = CreateRuntimeLevels(ingredients);
        }

        private void SyncRuntimeIngredientMetadata()
        {
            if (ingredients == null || ingredients.Count == 0)
            {
                return;
            }

            Dictionary<string, IngredientData> defaults = new Dictionary<string, IngredientData>();
            foreach (IngredientData ingredient in CreateRuntimeIngredients())
            {
                if (ingredient != null && !string.IsNullOrWhiteSpace(ingredient.ingredientID))
                {
                    defaults[ingredient.ingredientID] = ingredient;
                }
            }

            foreach (IngredientData ingredient in ingredients)
            {
                if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.ingredientID))
                {
                    continue;
                }

                if (!defaults.TryGetValue(ingredient.ingredientID, out IngredientData source))
                {
                    continue;
                }

                ingredient.ingredientNameCN = source.ingredientNameCN;
                ingredient.ingredientNameEN = source.ingredientNameEN;
                ingredient.aromaType = source.aromaType;
                ingredient.initialDescription = source.initialDescription;
                ingredient.ingredientColor = source.ingredientColor;
            }
        }

        private bool HasExpectedRuntimeIngredients()
        {
            return ingredients.Count >= 16
                && ingredients.Exists(x => x != null && x.ingredientID == "Rice")
                && ingredients.Exists(x => x != null && x.ingredientID == "TeaLeaf")
                && ingredients.Exists(x => x != null && x.ingredientID == "ScallionWhite");
        }

        private bool HasExpectedRuntimeLevels()
        {
            return levels.Count == 11
                && levels[0] != null
                && levels[0].levelID == "L01"
                && levels[0].levelName == "Gentle Grain Awakening"
                && levels[2] != null
                && levels[2].levelID == "L03"
                && levels[2].levelName == "Layered Dessert"
                && levels[4] != null
                && levels[4].levelID == "L05"
                && levels[4].levelName == "Clear Spice Split"
                && levels[10] != null
                && levels[10].levelID == "L11";
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
            return GameObject.Find("Placeholder Table") != null
                && GameObject.Find("Mortar Area") != null
                && GameObject.Find("Test Level Canvas") != null;
        }

        public static List<IngredientData> CreateRuntimeIngredients()
        {
            return new List<IngredientData>
            {
                CreateIngredient("Rice", "Rice", "Rice", "Soft grain", new Color(0.95f, 0.9f, 0.75f), "Rice is mild and clean. It needs steady grinding, but too much force can make it taste burnt."),
                CreateIngredient("TeaLeaf", "Tea Leaves", "Tea Leaves", "Tea", new Color(0.25f, 0.5f, 0.22f), "Tea leaves are bitter at first and sweet later. Slow grinding helps the sweetness come out."),
                CreateIngredient("BlackSesame", "Black Sesame", "Black Sesame", "Roasted nut", new Color(0.08f, 0.07f, 0.06f), "Black sesame has a strong roasted flavor. It can lead a recipe, but too much makes it bitter."),
                CreateIngredient("GlutinousRice", "Glutinous Rice", "Glutinous Rice", "Sticky grain", new Color(0.92f, 0.86f, 0.68f), "Glutinous rice makes the mixture smooth and thick. It should support stronger flavors."),
                CreateIngredient("Peanut", "Peanut", "Peanut", "Roasted nut", new Color(0.72f, 0.48f, 0.27f), "Peanut gives a warm roasted flavor. It works well with other nutty ingredients."),
                CreateIngredient("RedBean", "Red Bean", "Red Bean", "Sweet bean", new Color(0.55f, 0.12f, 0.11f), "Red bean is thick and gently sweet. It works well as a base flavor."),
                CreateIngredient("DriedTangerinePeel", "Dried Tangerine Peel", "Dried Tangerine Peel", "Citrus peel", new Color(0.95f, 0.45f, 0.12f), "Dried tangerine peel is bright and slightly bitter. It works best as a small accent."),
                CreateIngredient("RockSugar", "Rock Sugar", "Rock Sugar", "Clean sweetness", new Color(0.88f, 0.95f, 1f), "Rock sugar adds clean sweetness. It should round out the recipe instead of taking over."),
                CreateIngredient("SandGinger", "Sand Ginger", "Sand Ginger", "Root spice", new Color(0.78f, 0.58f, 0.35f), "Sand ginger is deep and spicy. It needs enough force to bring out its flavor."),
                CreateIngredient("CoarseSalt", "Coarse Salt", "Coarse Salt", "Salt", new Color(0.85f, 0.84f, 0.78f), "Coarse salt supports other flavors. Use it lightly so it does not become the main taste."),
                CreateIngredient("WhitePepper", "White Pepper", "White Pepper", "Sharp spice", new Color(0.8f, 0.77f, 0.68f), "White pepper is sharp and quick to spread. Too much grinding can make it harsh."),
                CreateIngredient("PeanutCrumb", "Peanut Crumb", "Peanut Crumb", "Roasted nut", new Color(0.68f, 0.46f, 0.25f), "Peanut crumb is warm and nutty. Strong spices can cover it easily."),
                CreateIngredient("LotusLeaf", "Lotus Leaf", "Lotus Leaf", "Green leaf", new Color(0.34f, 0.58f, 0.3f), "Lotus leaf is light and fresh. It can be covered easily by stronger ingredients."),
                CreateIngredient("YangjiangDouchi", "Yangjiang Douchi", "Yangjiang Douchi", "Fermented bean", new Color(0.18f, 0.14f, 0.1f), "Yangjiang douchi is savory and fermented. It gives the recipe a strong base."),
                CreateIngredient("Ginger", "Ginger", "Ginger", "Fresh spice", new Color(0.9f, 0.75f, 0.38f), "Ginger is fresh and sharp. It can brighten a recipe, but it can also become too strong."),
                CreateIngredient("ScallionWhite", "Scallion White", "Scallion White", "Fresh green", new Color(0.76f, 0.9f, 0.62f), "Scallion white is fresh and light. It works best near the end of a recipe."),
                CreateIngredient("Chili", "Chili", "Chili", "spicy", new Color(0.85f, 0.1f, 0.08f), "Direct hot spice. It is kept for compatibility and is not used by the default ten-level set.")
            };
        }

        public static List<RecipeLevelData> CreateRuntimeLevels(List<IngredientData> all)
        {
            IngredientData rice = Find(all, "Rice");
            IngredientData tea = Find(all, "TeaLeaf");
            IngredientData blackSesame = Find(all, "BlackSesame");
            IngredientData glutinousRice = Find(all, "GlutinousRice");
            IngredientData peanut = Find(all, "Peanut");
            IngredientData redBean = Find(all, "RedBean");
            IngredientData peel = Find(all, "DriedTangerinePeel");
            IngredientData rockSugar = Find(all, "RockSugar");
            IngredientData sandGinger = Find(all, "SandGinger");
            IngredientData coarseSalt = Find(all, "CoarseSalt");
            IngredientData whitePepper = Find(all, "WhitePepper");
            IngredientData lotusLeaf = Find(all, "LotusLeaf");
            IngredientData douchi = Find(all, "YangjiangDouchi");
            IngredientData ginger = Find(all, "Ginger");
            IngredientData scallion = Find(all, "ScallionWhite");

            return new List<RecipeLevelData>
            {
                Level("L01", "Gentle Grain Awakening", "Guangzhou", "Force Tutorial", "Use rice to learn grinding force. The goal is a soft rice flavor without a burnt taste.", new [] { rice }, new [] { rice }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(force:true, duration:true), 100,
                    minDuration:8f,
                    maxDuration:14f,
                    success:"The rice tastes soft and clean, with no burnt flavor.",
                    close:"Very close. Adjust the force or grinding time a little.",
                    wrong:"The rice does not taste right. Check the force and grinding time.",
                    clues:new [] { Clue("L01_Rice_Force", "Rice - Force Clue", "Rice works best with medium force. Too little force keeps it weak; too much can make it taste burnt.", MechanicType.Force, rice) }),

                Level("L02", "Slow Tea Return", "Chaozhou", "Speed Tutorial", "Use tea leaves to learn grinding speed. The goal is gentle tea bitterness followed by sweetness.", new [] { tea }, new [] { tea }, ForceLevel.Medium, SpeedLevel.Slow, Mechanics(speed:true, duration:true), 100,
                    minDuration:10f,
                    maxDuration:16f,
                    success:"The tea tastes balanced: light bitterness first, then sweetness.",
                    close:"Very close. Adjust the speed or grinding time a little.",
                    wrong:"The tea is too bitter. Check the speed and grinding time.",
                    clues:new [] { Clue("L02_Tea_Speed", "Tea Leaf - Speed Clue", "Tea leaves work best with slow grinding. Fast grinding makes the tea too bitter.", MechanicType.Speed, tea) }),

                Level("L03", "Layered Dessert", "Jiangmen", "Layer Timing", "Red bean, dried tangerine peel, and rock sugar each change the dessert at a different moment.", new [] { redBean, peel, rockSugar }, new [] { redBean, peel, rockSugar }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(order:true, duration:true), 100,
                    order:new [] { redBean, peel, rockSugar },
                    ratios:new [] { Ratio(redBean, RatioLevel.More), Ratio(rockSugar, RatioLevel.SlightlyMore), Ratio(peel, RatioLevel.Less) },
                    minDuration:18f,
                    maxDuration:28f,
                    success:"The dessert has a clear order: red bean first, citrus next, sweetness last.",
                    close:"Very close. Check the order or grinding time.",
                    wrong:"The dessert tastes out of order. Check when each ingredient is added.",
                    clues:new [] {
                        Clue("L03_RedBean_Order", "Red Bean - Order Clue", "Red bean works well early because it builds the base flavor.", MechanicType.IngredientOrder, redBean),
                        Clue("L03_Peel_Order", "Dried Tangerine Peel - Order Clue", "Dried tangerine peel works better after the base is set. Too early can make the recipe bitter.", MechanicType.IngredientOrder, peel),
                        Clue("L03_RockSugar_Order", "Rock Sugar - Order Clue", "Rock sugar works best at the end because it softens the final taste.", MechanicType.IngredientOrder, rockSugar)
                    }),

                Level("L04", "Joined Nut Crisp", "Foshan", "Shared Grind", "Peanut and black sesame are close in flavor. Try to find when they feel connected.", new [] { peanut, blackSesame }, new [] { peanut, blackSesame }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(combination:true, duration:true), 100,
                    ratios:new [] { Ratio(peanut, RatioLevel.More), Ratio(blackSesame, RatioLevel.Less) },
                    order:new [] { peanut, blackSesame },
                    combination:Combination(new [] { peanut, blackSesame }),
                    minDuration:15f,
                    maxDuration:24f,
                    success:"Peanut and black sesame blend into one warm nutty flavor.",
                    close:"Very close. Check the grouping or grinding time.",
                    wrong:"The two nutty flavors feel separate. Try a different grouping.",
                    clues:new [] {
                        Clue("L04_Peanut_Combination", "Peanut - Combination Clue", "Peanut works well with other nutty ingredients such as black sesame.", MechanicType.Combination, peanut, blackSesame),
                        Clue("L04_BlackSesame_Combination", "Black Sesame - Combination Clue", "Black sesame can blend with peanut, but too much handling can make it bitter.", MechanicType.Combination, blackSesame, peanut)
                    }),

                Level("L05", "Clear Spice Split", "Shantou", "Separate Grind", "White pepper and dried tangerine peel are both sharp. Find a way to keep their edges clear.", new [] { whitePepper, peel }, new [] { whitePepper, peel }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(combination:true, duration:true), 100,
                    ratios:new [] { Ratio(whitePepper, RatioLevel.More), Ratio(peel, RatioLevel.Less) },
                    order:new [] { whitePepper, peel },
                    combination:Combination(new [] { whitePepper }, new [] { peel }),
                    minDuration:14f,
                    maxDuration:22f,
                    success:"White pepper and dried tangerine peel stay clear because they were handled separately.",
                    close:"Very close. Check whether each ingredient has its own batch.",
                    wrong:"The sharp flavors are tangled together. Try using a separate batch.",
                    clues:new [] {
                        Clue("L05_WhitePepper_Separate", "White Pepper - Batch Clue", "White pepper is strong. It often works better away from other sharp ingredients.", MechanicType.Combination, whitePepper),
                        Clue("L05_Peel_Separate", "Dried Tangerine Peel - Batch Clue", "Dried tangerine peel can turn bitter when it is ground with another sharp ingredient.", MechanicType.Combination, peel)
                    }),

                Level("L06", "Soft Sesame Paste", "Shunde", "Subtle Balance", "A small change in amount can shift how the sesame paste feels.", new [] { blackSesame, glutinousRice }, new [] { blackSesame, glutinousRice }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(ratio:true, duration:true), 100,
                    ratios:new [] { Ratio(blackSesame, RatioLevel.Less), Ratio(glutinousRice, RatioLevel.VeryLess) },
                    order:new [] { blackSesame, glutinousRice },
                    combination:Combination(new [] { blackSesame }, new [] { glutinousRice }),
                    minDuration:14f,
                    maxDuration:22f,
                    success:"Black sesame is slightly stronger, and glutinous rice keeps the paste smooth.",
                    close:"Very close. Adjust the ratio or grinding time a little.",
                    wrong:"The sesame flavor is not clear enough. Check the ratio and grinding time.",
                    clues:new [] {
                        Clue("L06_BlackSesame_Ratio", "Black Sesame - Ratio Clue", "Use a little black sesame. It should be stronger than glutinous rice, but not heavy.", MechanicType.Ratio, blackSesame),
                        Clue("L06_GlutinousRice_Ratio", "Glutinous Rice - Ratio Clue", "Use the smallest amount of glutinous rice. It should only smooth the paste.", MechanicType.Ratio, glutinousRice)
                    }),

                Level("L07", "Sand Ginger Core", "Meizhou", "Two-Dimension Challenge", "Sand ginger should be the main flavor. Dried tangerine peel and coarse salt should support it.", new [] { sandGinger, coarseSalt, peel }, new [] { sandGinger, coarseSalt, peel }, ForceLevel.Heavy, SpeedLevel.Medium, Mechanics(force:true, ratio:true, duration:true), 90,
                    ratios:new [] { Ratio(sandGinger, RatioLevel.More), Ratio(peel, RatioLevel.SlightlyMore), Ratio(coarseSalt, RatioLevel.Less) },
                    order:new [] { sandGinger, peel, coarseSalt },
                    minDuration:20f,
                    maxDuration:32f,
                    success:"Sand ginger is clear, with a little citrus and salt behind it.",
                    close:"Very close. Check the force, ratio, or grinding time.",
                    wrong:"Salt or citrus is too strong, and sand ginger is not the main flavor.",
                    clues:new [] {
                        Clue("L07_SandGinger_Force", "Sand Ginger - Force Clue", "Sand ginger needs heavy force, but too much grinding can still make it harsh.", MechanicType.Force, sandGinger),
                        Clue("L07_CoarseSalt_Ratio", "Coarse Salt - Ratio Clue", "Coarse salt should stay in the background. Do not make it the main flavor.", MechanicType.Ratio, coarseSalt)
                    }),

                Level("L08", "Pepper Held in Balance", "Shantou", "Two-Dimension Challenge", "Speed is unlocked. Handle sharp spice carefully.", new [] { whitePepper, peanut, peel }, new [] { whitePepper, peanut, peel }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(speed:true, duration:true), 90,
                    ratios:new [] { Ratio(whitePepper, RatioLevel.More), Ratio(peanut, RatioLevel.SlightlyMore), Ratio(peel, RatioLevel.Less) },
                    order:new [] { whitePepper, peanut, peel },
                    combination:Combination(new [] { whitePepper }, new [] { peanut }, new [] { peel }),
                    minDuration:20f,
                    maxDuration:32f,
                    success:"White pepper is clear, peanut adds warmth, and citrus keeps it light.",
                    close:"Very close. Check the speed or grinding time.",
                    wrong:"White pepper is too harsh, or the support flavors are too weak.",
                    clues:new [] {
                        Clue("L08_WhitePepper_Speed", "White Pepper - Speed Clue", "White pepper works best at medium speed. Too fast makes it harsh; too slow makes it weak.", MechanicType.Speed, whitePepper)
                    }),

                Level("L09", "Wrapped Rice Layers", "Zhaoqing", "Three-Dimension Challenge", "Make a layered rice dumpling flavor with red bean, glutinous rice, black sesame, and lotus leaf.", new [] { glutinousRice, redBean, lotusLeaf, blackSesame }, new [] { glutinousRice, redBean, lotusLeaf, blackSesame }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(ratio:true, combination:true, order:true, duration:true), 90,
                    ratios:new [] { Ratio(redBean, RatioLevel.More), Ratio(glutinousRice, RatioLevel.SlightlyMore), Ratio(blackSesame, RatioLevel.Less), Ratio(lotusLeaf, RatioLevel.VeryLess) },
                    order:new [] { redBean, glutinousRice, blackSesame, lotusLeaf },
                    combination:Combination(new [] { glutinousRice, lotusLeaf }, new [] { redBean }, new [] { blackSesame }),
                    minDuration:25f,
                    maxDuration:40f,
                    success:"The dumpling flavor has clear layers: red bean, sticky rice, sesame, and lotus leaf.",
                    close:"Very close. Check the ratio, grouping, order, or time.",
                    wrong:"The layers are unclear. One flavor may be too strong or added at the wrong time.",
                    clues:new [] {
                        Clue("L09_LotusLeaf_Order", "Lotus Leaf - Order Clue", "Add lotus leaf near the end. If it is used too early, its fresh flavor gets covered.", MechanicType.IngredientOrder, lotusLeaf),
                        Clue("L09_LotusLeaf_Combination", "Lotus Leaf - Combination Clue", "Lotus leaf can be paired with glutinous rice, but black sesame is too strong for it.", MechanicType.Combination, lotusLeaf, glutinousRice)
                    }),

                Level("L10", "Savory Bean Current", "Yangjiang", "Four-Dimension Challenge", "Force, speed, ratio, and order are unlocked. Combination remains locked.", new [] { douchi, ginger, peel, coarseSalt }, new [] { douchi, ginger, peel, coarseSalt }, ForceLevel.Medium, SpeedLevel.Medium, Mechanics(force:true, speed:true, ratio:true, order:true, duration:true), 90,
                    ratios:new [] { Ratio(douchi, RatioLevel.More), Ratio(ginger, RatioLevel.SlightlyMore), Ratio(peel, RatioLevel.Less), Ratio(coarseSalt, RatioLevel.VeryLess) },
                    order:new [] { douchi, ginger, peel, coarseSalt },
                    minDuration:28f,
                    maxDuration:45f,
                    success:"Douchi is the main savory flavor, with ginger, citrus, and light salt behind it.",
                    close:"Very close. Check one process or structure setting.",
                    wrong:"Salt, ginger, or citrus is too strong, or the douchi flavor is unclear.",
                    clues:new [] {
                        Clue("L10_Douchi_Force", "Yangjiang Douchi - Force Clue", "Yangjiang douchi works best with medium force. Too little is weak; too much makes it muddy.", MechanicType.Force, douchi),
                        Clue("L10_Ginger_Speed", "Ginger - Speed Clue", "Ginger works best at medium speed. Too fast can make it too sharp.", MechanicType.Speed, ginger),
                        Clue("L10_Ginger_Order", "Ginger - Order Clue", "Ginger works well in the middle, after the base flavor is set.", MechanicType.IngredientOrder, ginger)
                    }),

                Level("L11", "Ginger-Scallion Finale", "Zhanjiang", "Final Free Challenge", "Make a balanced ginger-scallion sauce. Ginger flavors should lead, with scallion and salt kept light.", new [] { sandGinger, ginger, scallion, coarseSalt }, new [] { sandGinger, ginger, scallion, coarseSalt }, ForceLevel.Heavy, SpeedLevel.Medium, Mechanics(force:true, speed:true, ratio:true, combination:true, order:true, duration:true), 90,
                    ratios:new [] { Ratio(sandGinger, RatioLevel.More), Ratio(ginger, RatioLevel.SlightlyMore), Ratio(scallion, RatioLevel.Less), Ratio(coarseSalt, RatioLevel.VeryLess) },
                    order:new [] { coarseSalt, sandGinger, ginger, scallion },
                    combination:Combination(new [] { sandGinger, ginger }, new [] { scallion }, new [] { coarseSalt }),
                    minDuration:30f,
                    maxDuration:50f,
                    success:"The sauce is balanced: light salt, strong ginger, and fresh scallion at the end.",
                    close:"Very close. Check the final ratio, grouping, order, force, or speed.",
                    wrong:"The sauce is unbalanced. Ginger may be harsh, scallion may be lost, or salt may be too strong.",
                    clues:new [] {
                        Clue("L11_Scallion_Force", "Scallion White - Force Clue", "Use light force for scallion white. Too much force makes it watery and sharp.", MechanicType.Force, scallion),
                        Clue("L11_Scallion_Order", "Scallion White - Order Clue", "Add scallion white at the end so it stays fresh.", MechanicType.IngredientOrder, scallion),
                        Clue("L11_Scallion_Combination", "Scallion White - Combination Clue", "Keep scallion white separate from strong ginger so its fresh flavor stays clear.", MechanicType.Combination, scallion, ginger)
                    })
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

        public static LevelClueData Clue(string id, string title, string content, MechanicType dimension, params IngredientData[] relatedIngredients)
        {
            LevelClueData clue = new LevelClueData
            {
                clueId = id,
                title = title,
                content = content,
                relatedDimension = dimension
            };

            if (relatedIngredients != null)
            {
                clue.relatedIngredients.AddRange(relatedIngredients);
            }

            return clue;
        }

        public static RecipeLevelData Level(string id, string name, string city, string taste, string intro, IngredientData[] available, IngredientData[] required, ForceLevel force, SpeedLevel speed, EnabledMechanics mechanics, int passing, IngredientData[] order = null, RatioRequirement[] ratios = null, CombinationPattern combination = null, float minDuration = 3f, float maxDuration = 6f, string success = null, string close = null, string wrong = null, LevelClueData[] clues = null)
        {
            RecipeLevelData level = ScriptableObject.CreateInstance<RecipeLevelData>();
            level.levelID = id;
            level.levelName = name;
            level.cityName = city;
            level.targetTasteName = taste;
            level.levelIntro = intro;
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
            level.successFeedback = success;
            level.closeFeedback = close;
            level.wrongFeedback = wrong;
            if (clues != null)
            {
                level.unlockCluesOnComplete.AddRange(clues);
            }

            level.SyncDimensionsFromEnabledMechanics();
            level.BuildIngredientProfilesFromLevelData();
            return level;
        }

        private static IngredientData CreateIngredient(string id, string cn, string en, string aroma, Color color, string initialDescription)
        {
            IngredientData ingredient = ScriptableObject.CreateInstance<IngredientData>();
            ingredient.ingredientID = id;
            ingredient.ingredientNameCN = cn;
            ingredient.ingredientNameEN = en;
            ingredient.aromaType = aroma;
            ingredient.ingredientColor = color;
            ingredient.initialDescription = initialDescription;
            return ingredient;
        }

        private static IngredientData Find(List<IngredientData> source, string id)
        {
            return source.Find(x => x != null && x.ingredientID == id);
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
            ui.currentOrderLabel = CreateText(canvas.transform, "Current Order", new Vector2(300f, 68f), new Vector2(24f, -104f), topLeft, topLeft);
            ui.currentRatioLabel = CreateText(canvas.transform, "Current Ratio", new Vector2(300f, 68f), new Vector2(24f, -184f), topLeft, topLeft);
            ui.currentSpeedLabel = CreateText(canvas.transform, "Current Speed", new Vector2(300f, 42f), new Vector2(24f, -264f), topLeft, topLeft);
            ui.hintLabel = CreateText(canvas.transform, "Hint", new Vector2(380f, 160f), new Vector2(-24f, -24f), topRight, topRight, TextAnchor.UpperLeft);
            ui.EnsureIngredientTraitPanel();
            log.logText = null;

            Slider slider = CreateSlider(canvas.transform, "Force Slider", new Vector2(300f, 36f), new Vector2(24f, 56f), bottomLeft, bottomLeft);
            Text forceText = CreateText(canvas.transform, "Force Label", new Vector2(300f, 34f), new Vector2(24f, 20f), bottomLeft, bottomLeft);
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

    private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreatePanel(parent, label, size, position);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        Text text = CreateTextChild(buttonObject.transform, label, new Vector2(6f, 0f), new Vector2(-6f, 0f), TextAnchor.MiddleCenter);
        text.fontSize = 14;
        text.text = label;
        return button;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreatePanel(parent, label, size, position, anchorPoint, pivot);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        Text text = CreateTextChild(buttonObject.transform, label, new Vector2(6f, 0f), new Vector2(-6f, 0f), TextAnchor.MiddleCenter);
        text.fontSize = 14;
        text.text = label;
        return button;
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
