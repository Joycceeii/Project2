#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheTasteReviver.EditorTools
{
    public static class LevelDesignCsvImporter
    {
        private const string GeneratedRoot = "Assets/TheTasteReviver/Generated";
        private const string IngredientsPath = GeneratedRoot + "/Ingredients";
        private const string LevelsPath = GeneratedRoot + "/Levels";
        private const string DesignDataPath = "Assets/TheTasteReviver/DesignData/English";
        private const string IngredientCsvPath = DesignDataPath + "/ingredients_en.csv";
        private const string LevelCsvPath = DesignDataPath + "/levels_en.csv";
        private const string HintCsvPath = DesignDataPath + "/hints_en.csv";

        [MenuItem("The Taste Reviver/Import English CSV Design Data")]
        public static void ImportEnglishCsvDesignData()
        {
            EnsureFolder("Assets", "TheTasteReviver");
            EnsureFolder("Assets/TheTasteReviver", "Generated");
            EnsureFolder(GeneratedRoot, "Ingredients");
            EnsureFolder(GeneratedRoot, "Levels");

            List<Dictionary<string, string>> ingredientRows = ReadCsvAsset(IngredientCsvPath);
            List<Dictionary<string, string>> levelRows = ReadCsvAsset(LevelCsvPath);
            List<Dictionary<string, string>> hintRows = ReadCsvAsset(HintCsvPath);

            Dictionary<string, IngredientData> ingredients = ImportIngredients(ingredientRows);
            List<RecipeLevelData> levels = ImportLevels(levelRows, hintRows, ingredients);
            AssignOpenSceneReferences(ingredients.Values.ToList(), levels);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Imported English CSV design data. Ingredients: " + ingredients.Count + ", levels: " + levels.Count + ".");
        }

        private static Dictionary<string, IngredientData> ImportIngredients(List<Dictionary<string, string>> rows)
        {
            Dictionary<string, IngredientData> runtimeDefaults = TestLevelInitializer.CreateRuntimeIngredients()
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ingredientID))
                .ToDictionary(x => x.ingredientID, x => x);

            Dictionary<string, IngredientData> created = new Dictionary<string, IngredientData>();
            foreach (Dictionary<string, string> row in rows)
            {
                string id = Get(row, "IngredientID");
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                string path = IngredientsPath + "/" + Sanitize(id) + ".asset";
                IngredientData asset = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<IngredientData>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                runtimeDefaults.TryGetValue(id, out IngredientData defaults);
                asset.name = id;
                asset.ingredientID = id;
                asset.ingredientNameCN = string.Empty;
                asset.ingredientNameEN = Get(row, "EnglishName");
                asset.aromaType = Get(row, "AromaType");
                asset.initialDescription = Get(row, "InitialDescription");
                asset.defaultRatioValue = DefaultRatioToInt(Get(row, "DefaultRatio"));
                if (defaults != null)
                {
                    asset.ingredientColor = defaults.ingredientColor;
                    asset.icon = defaults.icon;
                    asset.prefab = defaults.prefab;
                }

                EditorUtility.SetDirty(asset);
                created[id] = asset;
            }

            return created;
        }

        private static List<RecipeLevelData> ImportLevels(List<Dictionary<string, string>> rows, List<Dictionary<string, string>> hintRows, Dictionary<string, IngredientData> ingredients)
        {
            Dictionary<string, List<Dictionary<string, string>>> hintsByLevel = hintRows
                .Where(row => !string.IsNullOrWhiteSpace(Get(row, "LevelID")))
                .GroupBy(row => Get(row, "LevelID"))
                .ToDictionary(group => group.Key, group => group.ToList());

            List<RecipeLevelData> created = new List<RecipeLevelData>();
            foreach (Dictionary<string, string> row in rows)
            {
                string id = Get(row, "LevelID");
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                string levelName = Get(row, "LevelName");
                string path = LevelsPath + "/" + Sanitize(id + "_" + levelName) + ".asset";
                RecipeLevelData asset = AssetDatabase.LoadAssetAtPath<RecipeLevelData>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<RecipeLevelData>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.name = id;
                asset.levelID = id;
                asset.cityName = Get(row, "City");
                asset.levelName = levelName;
                asset.targetTasteName = Get(row, "TargetTasteName");
                asset.levelIntro = Get(row, "LevelIntro");
                asset.availableIngredients = ResolveIngredients(Get(row, "AvailableIngredients"), ingredients);
                asset.requiredIngredients = ResolveIngredients(Get(row, "RequiredIngredients"), ingredients);
                asset.forbiddenIngredients.Clear();
                asset.maxIngredientCount = Mathf.Clamp(asset.availableIngredients.Count, 1, 4);
                asset.correctIngredientOrder = ResolveIngredients(Get(row, "CorrectIngredientOrder"), ingredients);
                if (asset.correctIngredientOrder.Count == 0)
                {
                    asset.correctIngredientOrder.AddRange(asset.requiredIngredients);
                }

                asset.correctRatioPattern = ParseRatioPattern(Get(row, "CorrectRatioPattern"), ingredients);
                asset.correctCombinationPattern = ParseCombinationPattern(Get(row, "CorrectCombinationPattern"), ingredients);
                asset.targetForceLevel = ParseEnum(Get(row, "TargetForceLevel"), ForceLevel.Medium);
                asset.targetSpeedLevel = ParseEnum(Get(row, "TargetSpeedLevel"), SpeedLevel.Medium);
                asset.minGrindDuration = ParseFloat(Get(row, "MinGrindDuration"), 3f);
                asset.maxGrindDuration = ParseFloat(Get(row, "MaxGrindDuration"), 6f);
                asset.passingScore = ParseInt(Get(row, "PassingScore"), 90);
                asset.successFeedback = Get(row, "SuccessFeedback");
                asset.closeFeedback = Get(row, "CloseFeedback");
                asset.wrongFeedback = Get(row, "WrongFeedback");
                asset.enabledMechanics = ParseMechanics(Get(row, "EnabledMechanics"));
                asset.SyncDimensionsFromEnabledMechanics();
                asset.hintSettings.hintPriority = BuildHintPriority(asset.enabledMechanics);
                asset.processFeedbackRules.Clear();
                asset.unlockCluesOnComplete.Clear();
                asset.progressiveHintRules.Clear();
                asset.fallbackHintText = string.Empty;

                if (hintsByLevel.TryGetValue(id, out List<Dictionary<string, string>> levelHints))
                {
                    ApplyHints(asset, levelHints, ingredients);
                }

                EditorUtility.SetDirty(asset);
                created.Add(asset);
            }

            return created.OrderBy(x => x.levelID).ToList();
        }

        private static void ApplyHints(RecipeLevelData level, List<Dictionary<string, string>> hints, Dictionary<string, IngredientData> ingredients)
        {
            foreach (Dictionary<string, string> row in hints)
            {
                string type = Get(row, "HintType");
                string text = Get(row, "HintText");
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (type == "Fallback")
                {
                    level.fallbackHintText = text;
                    continue;
                }

                if (type == "ProcessCheck")
                {
                    ProcessFeedbackRule rule = new ProcessFeedbackRule
                    {
                        ruleId = Get(row, "RuleID"),
                        priority = ParseInt(Get(row, "Priority"), 0),
                        hintText = text
                    };
                    rule.requiredCorrect.AddRange(ParseMechanicList(Get(row, "RequiredCorrect")));
                    rule.requiredIncorrect.AddRange(ParseMechanicList(Get(row, "RequiredIncorrect")));
                    level.processFeedbackRules.Add(rule);
                    continue;
                }

                if (type == "ProgressiveHint")
                {
                    level.progressiveHintRules.Add(new ProgressiveHintRule
                    {
                        ruleId = Get(row, "RuleID"),
                        priority = ParseInt(Get(row, "Priority"), 0),
                        mechanic = ParseEnum(Get(row, "Mechanic"), MechanicType.IngredientSelection),
                        hintText = text
                    });
                    continue;
                }

                if (type == "PermanentClue")
                {
                    LevelClueData clue = new LevelClueData
                    {
                        clueId = Get(row, "RuleID"),
                        title = Get(row, "ClueTitle"),
                        content = text,
                        relatedDimension = ParseEnum(Get(row, "Mechanic"), MechanicType.IngredientSelection)
                    };
                    clue.relatedIngredients.AddRange(ResolveIngredients(Get(row, "RelatedIngredients"), ingredients));
                    level.unlockCluesOnComplete.Add(clue);
                }
            }
        }

        private static void AssignOpenSceneReferences(List<IngredientData> ingredients, List<RecipeLevelData> levels)
        {
            TestLevelInitializer initializer = UnityEngine.Object.FindFirstObjectByType<TestLevelInitializer>();
            if (initializer != null)
            {
                initializer.useRuntimeTenLevelData = false;
                initializer.ingredients = ingredients;
                initializer.levels = levels;
                EditorUtility.SetDirty(initializer);
                EditorSceneManager.MarkSceneDirty(initializer.gameObject.scene);
            }

            LevelManager levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.levels = levels;
                EditorUtility.SetDirty(levelManager);
                EditorSceneManager.MarkSceneDirty(levelManager.gameObject.scene);
            }
        }

        private static EnabledMechanics ParseMechanics(string value)
        {
            HashSet<MechanicType> mechanics = new HashSet<MechanicType>(ParseMechanicList(value));
            return new EnabledMechanics
            {
                enableIngredientSelection = mechanics.Contains(MechanicType.IngredientSelection),
                enableIngredientOrder = mechanics.Contains(MechanicType.IngredientOrder),
                enableRatio = mechanics.Contains(MechanicType.Ratio),
                enableCombination = mechanics.Contains(MechanicType.Combination),
                enableForce = mechanics.Contains(MechanicType.Force),
                enableSpeed = mechanics.Contains(MechanicType.Speed),
                enableGrindDuration = mechanics.Contains(MechanicType.GrindDuration)
            };
        }

        private static List<MechanicType> BuildHintPriority(EnabledMechanics mechanics)
        {
            MechanicType[] order =
            {
                MechanicType.IngredientOrder,
                MechanicType.Ratio,
                MechanicType.Combination,
                MechanicType.Speed,
                MechanicType.Force
            };

            return order.Where(mechanics.IsEnabled).ToList();
        }

        private static List<RatioRequirement> ParseRatioPattern(string value, Dictionary<string, IngredientData> ingredients)
        {
            List<RatioRequirement> ratios = new List<RatioRequirement>();
            foreach (string part in SplitList(value, ';'))
            {
                string[] pieces = part.Split('=');
                if (pieces.Length != 2)
                {
                    continue;
                }

                if (ingredients.TryGetValue(pieces[0].Trim(), out IngredientData ingredient))
                {
                    ratios.Add(new RatioRequirement
                    {
                        ingredient = ingredient,
                        ratioLevel = ParseEnum(pieces[1].Trim(), RatioLevel.None)
                    });
                }
            }

            return ratios;
        }

        private static CombinationPattern ParseCombinationPattern(string value, Dictionary<string, IngredientData> ingredients)
        {
            CombinationPattern pattern = new CombinationPattern();
            foreach (string groupText in SplitList(value, '|'))
            {
                CombinationGroup group = new CombinationGroup();
                group.ingredients.AddRange(ResolveIngredients(groupText.Replace("+", ","), ingredients));
                if (group.ingredients.Count > 0)
                {
                    pattern.groups.Add(group);
                }
            }

            return pattern;
        }

        private static List<IngredientData> ResolveIngredients(string value, Dictionary<string, IngredientData> ingredients)
        {
            List<IngredientData> result = new List<IngredientData>();
            foreach (string id in SplitList(value, ','))
            {
                if (ingredients.TryGetValue(id.Trim(), out IngredientData ingredient))
                {
                    result.Add(ingredient);
                }
                else if (!string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning("Unknown ingredient ID in CSV: " + id);
                }
            }

            return result;
        }

        private static List<MechanicType> ParseMechanicList(string value)
        {
            List<MechanicType> result = new List<MechanicType>();
            foreach (string part in SplitList(value, ','))
            {
                if (Enum.TryParse(part.Trim(), out MechanicType mechanic))
                {
                    result.Add(mechanic);
                }
            }

            return result;
        }

        private static List<Dictionary<string, string>> ReadCsvAsset(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Missing CSV file: " + assetPath, fullPath);
            }

            List<List<string>> rows = ParseCsv(File.ReadAllText(fullPath, Encoding.UTF8));
            if (rows.Count == 0)
            {
                return new List<Dictionary<string, string>>();
            }

            List<string> headers = rows[0];
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            for (int i = 1; i < rows.Count; i++)
            {
                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int j = 0; j < headers.Count; j++)
                {
                    row[headers[j]] = j < rows[i].Count ? rows[i][j] : string.Empty;
                }

                result.Add(row);
            }

            return result;
        }

        private static List<List<string>> ParseCsv(string text)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else if (c != '\r')
                {
                    field.Append(c);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows.Where(x => x.Any(cell => !string.IsNullOrWhiteSpace(cell))).ToList();
        }

        private static List<string> SplitList(string value, char separator)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            return value.Split(separator)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static string Get(Dictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out string value) ? value.Trim() : string.Empty;
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
        }

        private static int DefaultRatioToInt(string value)
        {
            switch (value)
            {
                case "High": return 2;
                case "Low": return 0;
                default: return 1;
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

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}
#endif
