#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheTasteReviver.Editor
{
    /// <summary>
    /// One-time scene migration used to turn the monolithic gameplay Canvas into
    /// independently authored panel prefabs. Safe to run again after the migration.
    /// </summary>
    public static class GameplayUiPrefabMigration
    {
        private const string ScenePath = "Assets/Scenes/TasteRestorerTestLevel.unity";
        private const string PrefabFolder = "Assets/Prefabs/UI";

        [MenuItem("Tools/The Taste Reviver/Migrate Gameplay UI To Prefabs")]
        public static void Migrate()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabFolder);

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            UIManager manager = Object.FindFirstObjectByType<UIManager>();
            if (manager == null)
            {
                throw new MissingReferenceException("TasteRestorerTestLevel does not contain a UIManager.");
            }

            Transform canvas = manager.transform;
            GameplayHudView hud = BuildHud(canvas, manager);
            RatioSelectionPanelView ratio = BuildRatio(manager);
            IngredientTraitsPanelView traits = BuildTraits(canvas, manager);
            IngredientTooltipView tooltip = BuildTooltip(manager);
            OpeningLevelTitlePanelView title = BuildOpeningTitle(manager);
            MortarReactionPanelView reaction = BuildMortarReaction(canvas, manager);
            StoryIntroPanelView story = BuildStoryIntro(canvas, manager);

            manager.ConfigurePrefabViews(hud, ratio, traits, tooltip, title, reaction, story);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Gameplay UI migration complete. Panel prefabs are in " + PrefabFolder + ".");
        }

        public static void RunBatchMigration()
        {
            Migrate();
        }

        private static GameplayHudView BuildHud(Transform canvas, UIManager manager)
        {
            Transform existing = canvas.Find("Gameplay HUD");
            GameObject root = existing != null ? existing.gameObject : CreateStretchRoot(canvas, "Gameplay HUD");
            GameplayHudView view = GetOrAdd<GameplayHudView>(root);

            if (existing == null)
            {
                HashSet<GameObject> elements = new HashSet<GameObject>();
                AddPanel(elements, manager.levelLabel);
                AddPanel(elements, manager.currentOrderLabel);
                AddPanel(elements, manager.currentRatioLabel);
                AddPanel(elements, manager.currentSpeedLabel);
                AddPanel(elements, manager.hintLabel);
                Add(elements, manager.experimentLogButton);
                Add(elements, manager.evaluateButton);
                Add(elements, manager.resetAttemptButton);
                Add(elements, manager.newBatchButton);
                Add(elements, manager.nextLevelButton);
                Add(elements, Find(canvas, "Force Slider"));
                Add(elements, Find(canvas, "Force Label Panel"));

                foreach (GameObject element in elements.Where(item => item != null))
                {
                    element.transform.SetParent(root.transform, false);
                }
            }

            view.Configure(
                manager.levelLabel,
                manager.currentOrderLabel,
                manager.currentRatioLabel,
                manager.currentSpeedLabel,
                manager.hintLabel,
                manager.experimentLogButton,
                manager.evaluateButton,
                manager.resetAttemptButton,
                manager.newBatchButton,
                manager.nextLevelButton);
            return SaveAndConnect(root, "GameplayHud.prefab").GetComponent<GameplayHudView>();
        }

        private static RatioSelectionPanelView BuildRatio(UIManager manager)
        {
            GameObject root = manager.ratioSelectionPanel;
            RatioSelectionPanelView view = GetOrAdd<RatioSelectionPanelView>(root);
            view.Configure(manager.ratioSelectionTitle, manager.ratioSelectionButtons);
            return SaveAndConnect(root, "RatioSelectionPanel.prefab").GetComponent<RatioSelectionPanelView>();
        }

        private static IngredientTraitsPanelView BuildTraits(Transform canvas, UIManager manager)
        {
            Transform existing = canvas.Find("Ingredient Traits UI");
            GameObject root = existing != null ? existing.gameObject : CreateStretchRoot(canvas, "Ingredient Traits UI");
            GameObject backdrop = Find(canvas, "Ingredient Traits Backdrop");
            GameObject panel = Find(canvas, "Ingredient Traits Expanded Panel");
            GameObject toggleObject = manager.ingredientTraitToggleButton != null
                ? manager.ingredientTraitToggleButton.gameObject
                : Find(canvas, "Ingredient Traits Toggle");

            if (existing == null)
            {
                Reparent(backdrop, root.transform);
                Reparent(panel, root.transform);
                Reparent(toggleObject, root.transform);
            }

            Button backdropButton = backdrop != null ? backdrop.GetComponent<Button>() : null;
            ScrollRect scrollRect = panel != null ? panel.GetComponent<ScrollRect>() : null;
            RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;
            IngredientTraitsPanelView view = GetOrAdd<IngredientTraitsPanelView>(root);
            view.Configure(
                backdrop,
                panel,
                manager.ingredientTraitLabel,
                manager.ingredientTraitToggleButton,
                backdropButton,
                scrollRect,
                viewport);
            return SaveAndConnect(root, "IngredientTraitsPanel.prefab").GetComponent<IngredientTraitsPanelView>();
        }

        private static IngredientTooltipView BuildTooltip(UIManager manager)
        {
            GameObject root = manager.ingredientTooltipPanel;
            IngredientTooltipView view = GetOrAdd<IngredientTooltipView>(root);
            view.Configure(manager.ingredientTooltipLabel, manager.ingredientTooltipCursorOffset);
            return SaveAndConnect(root, "IngredientTooltipPanel.prefab").GetComponent<IngredientTooltipView>();
        }

        private static OpeningLevelTitlePanelView BuildOpeningTitle(UIManager manager)
        {
            GameObject root = manager.openingLevelTitleLabel.transform.parent.gameObject;
            OpeningLevelTitlePanelView view = GetOrAdd<OpeningLevelTitlePanelView>(root);
            view.Configure(manager.openingLevelTitleLabel);
            return SaveAndConnect(root, "OpeningLevelTitlePanel.prefab").GetComponent<OpeningLevelTitlePanelView>();
        }

        private static MortarReactionPanelView BuildMortarReaction(Transform canvas, UIManager manager)
        {
            GameObject root = Find(canvas, "Mortar Reaction");
            bool createdRoot = root == null;
            root ??= CreateRectObject(canvas, "Mortar Reaction");
            RectTransform rect = root.GetComponent<RectTransform>();
            if (createdRoot)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = manager.mortarReactionSize;
                rect.anchoredPosition = manager.mortarReactionPosition;
            }

            Image background = root.GetComponent<Image>();
            bool createdBackground = background == null;
            background ??= root.AddComponent<Image>();
            if (createdRoot || createdBackground)
            {
                Texture2D texture = Resources.Load<Texture2D>("UI/MortarReactionBackground");
                if (texture != null)
                {
                    background.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                    background.type = Image.Type.Sliced;
                }
            }
            background.raycastTarget = false;

            CanvasGroup group = GetOrAdd<CanvasGroup>(root);
            group.blocksRaycasts = false;
            group.interactable = false;
            TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
            bool createdLabel = label == null;
            label ??= CreateText(root.transform, "Mortar Reaction Text", 23);
            if (createdLabel)
            {
                Stretch(label.rectTransform, new Vector2(94f, 60f), new Vector2(-94f, -60f));
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16f;
                label.fontSizeMax = 23f;
                label.richText = true;
            }
            label.raycastTarget = false;

            MortarReactionPanelView view = GetOrAdd<MortarReactionPanelView>(root);
            view.Configure(label, group, manager.mortarReactionHoldSeconds);
            if (createdRoot)
            {
                root.SetActive(false);
            }
            return SaveAndConnect(root, "MortarReactionPanel.prefab").GetComponent<MortarReactionPanelView>();
        }

        private static StoryIntroPanelView BuildStoryIntro(Transform canvas, UIManager manager)
        {
            GameObject root = Find(canvas, "Story Intro Panel");
            bool createdRoot = root == null;
            root ??= CreateStretchRoot(canvas, "Story Intro Panel");
            Image backdrop = root.GetComponent<Image>();
            bool createdBackdrop = backdrop == null;
            backdrop ??= root.AddComponent<Image>();
            if (createdRoot || createdBackdrop)
            {
                backdrop.color = manager.storyIntroBackdropColor;
            }
            CanvasGroup group = GetOrAdd<CanvasGroup>(root);
            group.blocksRaycasts = true;
            group.interactable = true;

            GameObject frame = Find(root.transform, "Story Image Frame");
            bool createdFrame = frame == null;
            frame ??= CreateRectObject(root.transform, "Story Image Frame");
            if (createdFrame)
            {
                SetCentered(frame.GetComponent<RectTransform>(), manager.storyIntroFrameSize, manager.storyIntroFramePosition);
            }
            Image frameImage = frame.GetComponent<Image>();
            bool createdFrameImage = frameImage == null;
            frameImage ??= frame.AddComponent<Image>();
            if (createdFrame || createdFrameImage)
            {
                PanelBackgroundStyle.Apply(frameImage, PanelBackgroundKind.PaleCream, 0.98f);
            }

            GameObject imageObject = Find(frame.transform, "Story Image");
            bool createdImageObject = imageObject == null;
            imageObject ??= CreateRectObject(frame.transform, "Story Image");
            if (createdImageObject)
            {
                SetCentered(imageObject.GetComponent<RectTransform>(), manager.storyIntroImageSize, manager.storyIntroImagePosition);
            }
            Image image = imageObject.GetComponent<Image>();
            bool createdImage = image == null;
            image ??= imageObject.AddComponent<Image>();
            if (createdImageObject || createdImage)
            {
                image.preserveAspect = true;
            }
            image.raycastTarget = false;

            TMP_Text body = FindText(frame.transform, "Story Intro Text");
            bool createdBody = body == null;
            body ??= CreateText(frame.transform, "Story Intro Text", 22);
            if (createdBody)
            {
                SetCentered(body.rectTransform, manager.storyIntroTextSize, manager.storyIntroTextPosition);
                body.alignment = TextAlignmentOptions.Top;
                body.lineSpacing = 7f;
                body.overflowMode = TextOverflowModes.Ellipsis;
            }

            GameObject buttonObject = Find(root.transform, "Story Continue Button");
            bool createdButtonObject = buttonObject == null;
            buttonObject ??= CreateRectObject(root.transform, "Story Continue Button");
            if (createdButtonObject)
            {
                SetCentered(buttonObject.GetComponent<RectTransform>(), manager.storyIntroButtonSize, manager.storyIntroButtonPosition);
            }
            Image buttonImage = buttonObject.GetComponent<Image>();
            bool createdButtonImage = buttonImage == null;
            buttonImage ??= buttonObject.AddComponent<Image>();
            if (createdButtonObject || createdButtonImage)
            {
                PanelBackgroundStyle.Apply(buttonImage, PanelBackgroundKind.OchreYellow, 0.98f);
            }
            Button button = GetOrAdd<Button>(buttonObject);
            button.targetGraphic = buttonImage;
            TMP_Text buttonLabel = buttonObject.GetComponentInChildren<TMP_Text>(true);
            bool createdButtonLabel = buttonLabel == null;
            buttonLabel ??= CreateText(buttonObject.transform, "Story Continue Button Text", 20);
            if (createdButtonLabel)
            {
                Stretch(buttonLabel.rectTransform, new Vector2(34f, 10f), new Vector2(-34f, -12f));
                buttonLabel.alignment = TextAlignmentOptions.Center;
                buttonLabel.fontStyle = FontStyles.Bold;
                buttonLabel.text = "Continue";
            }

            StoryIntroPanelView view = GetOrAdd<StoryIntroPanelView>(root);
            view.Configure(backdrop, image, body, button, buttonLabel);
            if (createdRoot)
            {
                root.SetActive(false);
            }
            return SaveAndConnect(root, "StoryIntroPanel.prefab").GetComponent<StoryIntroPanelView>();
        }

        private static GameObject SaveAndConnect(GameObject root, string fileName)
        {
            string path = PrefabFolder + "/" + fileName;
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);

            // SaveAsPrefabAssetAndConnect returns the persistent prefab asset root.
            // UIManager must reference the connected scene instance instead; otherwise
            // runtime presentation code attempts to modify asset Transforms directly.
            return root;
        }

        private static GameObject CreateStretchRoot(Transform parent, string name)
        {
            GameObject root = CreateRectObject(parent, name);
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            return root;
        }

        private static GameObject CreateRectObject(Transform parent, string name)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            return result;
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize)
        {
            GameObject textObject = CreateRectObject(parent, name);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = ThemeFontProvider.GetTmpFont(Mathf.RoundToInt(fontSize));
            text.fontSize = fontSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Reparent(GameObject child, Transform parent)
        {
            if (child != null && child.transform.parent != parent)
            {
                child.transform.SetParent(parent, false);
            }
        }

        private static void Add(HashSet<GameObject> objects, Component component)
        {
            Add(objects, component != null ? component.gameObject : null);
        }

        private static void Add(HashSet<GameObject> objects, GameObject item)
        {
            if (item != null)
            {
                objects.Add(item);
            }
        }

        private static void AddPanel(HashSet<GameObject> objects, TMP_Text text)
        {
            if (text != null)
            {
                Add(objects, text.transform.parent != null ? text.transform.parent.gameObject : text.gameObject);
            }
        }

        private static GameObject Find(Transform root, string name)
        {
            Transform match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            return match != null ? match.gameObject : null;
        }

        private static TMP_Text FindText(Transform root, string name)
        {
            GameObject match = Find(root, name);
            return match != null ? match.GetComponent<TMP_Text>() : null;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                string name = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
