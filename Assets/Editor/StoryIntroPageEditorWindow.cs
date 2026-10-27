#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TheTasteReviver.EditorTools
{
    public sealed class StoryIntroPageEditorWindow : EditorWindow
    {
        private RecipeLevelData level;
        private int pageIndex;
        private StoryIntroPanelView storyIntroView;
        private Vector2 scrollPosition;

        [MenuItem("The Taste Reviver/Story Page Visual Editor")]
        public static void Open()
        {
            StoryIntroPageEditorWindow window = GetWindow<StoryIntroPageEditorWindow>("Story Page Editor");
            window.minSize = new Vector2(360f, 260f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "选择关卡和故事页后，点击 Select Text，在 Scene 视图中直接拖动文字框。调好后点击 Save Scene Layout To Page。",
                MessageType.Info);

            level = (RecipeLevelData)EditorGUILayout.ObjectField(
                "Level Asset",
                level,
                typeof(RecipeLevelData),
                false);

            storyIntroView = FindStoryIntroView();
            EditorGUILayout.LabelField(
                "Scene Story Intro",
                storyIntroView != null ? "Found" : "Not found",
                EditorStyles.boldLabel);

            if (level == null || level.storyIntroPages == null || level.storyIntroPages.Count == 0)
            {
                EditorGUILayout.HelpBox("请先指定一个包含 Story Intro Pages 的关卡 asset。", MessageType.Warning);
                return;
            }

            string[] pageNames = new string[level.storyIntroPages.Count];
            for (int i = 0; i < pageNames.Length; i++)
            {
                StoryIntroPageData page = level.storyIntroPages[i];
                pageNames[i] = page == null ? "<Missing Page>" : page.pageKey;
            }

            pageIndex = Mathf.Clamp(pageIndex, 0, pageNames.Length - 1);
            pageIndex = EditorGUILayout.Popup("Story Page", pageIndex, pageNames);
            StoryIntroPageData selectedPage = level.storyIntroPages[pageIndex];
            if (selectedPage == null)
            {
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.LabelField("Selected Page", selectedPage.pageKey, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Override", selectedPage.overrideTextPresentation ? "Enabled" : "Disabled");

            if (GUILayout.Button("Show Page Preview In Scene"))
            {
                ShowPagePreview(selectedPage);
            }

            using (new EditorGUI.DisabledScope(storyIntroView == null || storyIntroView.EditorStoryText == null))
            {
                if (GUILayout.Button("Select Text In Scene"))
                {
                    SelectStoryText();
                }

                if (GUILayout.Button("Load Page Layout To Scene"))
                {
                    LoadPageLayout(selectedPage);
                }

                if (GUILayout.Button("Save Scene Layout To Page"))
                {
                    SaveSceneLayout(selectedPage);
                }
            }

            EditorGUILayout.HelpBox(
                "Save 会自动勾选 Override Text Presentation，并保存位置、大小、旋转、字号、颜色和对齐方式。",
                MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private StoryIntroPanelView FindStoryIntroView()
        {
            return Object.FindFirstObjectByType<StoryIntroPanelView>(FindObjectsInactive.Include);
        }

        private void SelectStoryText()
        {
            TMP_Text text = storyIntroView != null ? storyIntroView.EditorStoryText : null;
            if (text == null)
            {
                return;
            }

            Selection.activeGameObject = text.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private void ShowPagePreview(StoryIntroPageData page)
        {
            if (storyIntroView == null || page == null)
            {
                return;
            }

            Texture2D texture = Resources.Load<Texture2D>(page.imageResourcePath);
            if (texture == null)
            {
                EditorUtility.DisplayDialog(
                    "Story Page Editor",
                    "找不到图片资源：" + page.imageResourcePath,
                    "OK");
                return;
            }

            storyIntroView.EditorPreview(texture, page.storyText);
            if (page.overrideTextPresentation)
            {
                LoadPageLayout(page);
            }

            EditorUtility.SetDirty(storyIntroView.gameObject);
            SceneView.RepaintAll();
        }

        private void LoadPageLayout(StoryIntroPageData page)
        {
            TMP_Text text = storyIntroView != null ? storyIntroView.EditorStoryText : null;
            if (text == null || page == null || !page.overrideTextPresentation)
            {
                EditorUtility.DisplayDialog(
                    "Story Page Editor",
                    "这个页面还没有启用 Override Text Presentation。请先手动调好 Scene 里的文字，再点击 Save Scene Layout To Page。",
                    "OK");
                return;
            }

            Undo.RecordObject(text.rectTransform, "Load Story Page Text Layout");
            storyIntroView.ApplyTextPresentation(
                page.textSize,
                page.textPosition,
                page.textRotation,
                page.textFontSize,
                page.textColor,
                page.textAlignment);
            EditorUtility.SetDirty(text.rectTransform);
            SceneView.RepaintAll();
        }

        private void SaveSceneLayout(StoryIntroPageData page)
        {
            TMP_Text text = storyIntroView != null ? storyIntroView.EditorStoryText : null;
            if (text == null || page == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            Undo.RecordObject(level, "Save Story Page Text Layout");
            page.overrideTextPresentation = true;
            page.textSize = rect.sizeDelta;
            page.textPosition = rect.anchoredPosition;
            page.textRotation = rect.localEulerAngles.z > 180f
                ? rect.localEulerAngles.z - 360f
                : rect.localEulerAngles.z;
            page.textFontSize = text.fontSize;
            page.textColor = text.color;
            page.textAlignment = text.alignment;
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }
}
#endif
