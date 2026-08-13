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
        private const string GeneratedRoot = "Assets/Data/GeneratedAssets";
        private const string IngredientsPath = GeneratedRoot + "/Ingredients";
        private const string LevelsPath = GeneratedRoot + "/Levels";
        private const string DesignDataPath = "Assets/Data/DesignTables/English";
        private const string IngredientCsvPath = DesignDataPath + "/ingredients_en.csv";
        private const string LevelCsvPath = DesignDataPath + "/levels_en.csv";
        private const string HintCsvPath = DesignDataPath + "/hints_en.csv";
        private const string LevelProfileFolderPath = DesignDataPath + "/LevelProfiles";

        [MenuItem("The Taste Reviver/Import English CSV Design Data")]
        public static void ImportEnglishCsvDesignData()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "GeneratedAssets");
            EnsureFolder(GeneratedRoot, "Ingredients");
            EnsureFolder(GeneratedRoot, "Levels");

            List<Dictionary<string, string>> ingredientRows = ReadCsvAsset(IngredientCsvPath);
            List<Dictionary<string, string>> levelRows = ReadCsvAsset(LevelCsvPath);
            List<Dictionary<string, string>> hintRows = ReadCsvAsset(HintCsvPath);

            Dictionary<string, IngredientData> ingredients = ImportIngredients(ingredientRows);
            TryGenerateIngredientPrefabs();
            List<RecipeLevelData> levels = ImportLevels(levelRows, hintRows, ingredients);
            AssignOpenSceneReferences(ingredients.Values.ToList(), levels);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Imported English CSV design data. Ingredients: " + ingredients.Count + ", levels: " + levels.Count + ".");
        }

        private static Dictionary<string, IngredientData> ImportIngredients(List<Dictionary<string, string>> rows)
        {
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
                Color existingColor = asset != null ? asset.ingredientColor : Color.white;
                Sprite existingIcon = asset != null ? asset.icon : null;
                GameObject existingPrefab = asset != null ? asset.prefab : null;
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<IngredientData>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.name = Path.GetFileNameWithoutExtension(path);
                asset.ingredientID = id;
                asset.ingredientNameCN = string.Empty;
                asset.ingredientNameEN = Get(row, "EnglishName");
                asset.aromaType = Get(row, "AromaType");
                asset.initialDescription = Get(row, "InitialDescription");
                asset.defaultRatioValue = DefaultRatioToInt(Get(row, "DefaultRatio"));
                asset.ingredientColor = existingColor;
                asset.icon = existingIcon;
                asset.prefab = existingPrefab;

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

                asset.name = Path.GetFileNameWithoutExtension(path);
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
                asset.unlockCluesOnComplete.Clear();
                asset.progressiveHintRules.Clear();
                asset.fallbackHintText = string.Empty;
                asset.BuildIngredientProfilesFromLevelData();

                List<Dictionary<string, string>> levelProfiles = ReadLevelProfileCsv(id);
                if (levelProfiles.Count > 0)
                {
                    asset.ingredientProfiles.Clear();
                    ApplyIngredientProfiles(asset, levelProfiles, ingredients);
                }

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

        private static List<Dictionary<string, string>> ReadLevelProfileCsv(string levelID)
        {
            if (string.IsNullOrWhiteSpace(levelID))
            {
                return new List<Dictionary<string, string>>();
            }

            string path = LevelProfileFolderPath + "/" + Sanitize(levelID) + "_profiles_en.csv";
            return ReadOptionalCsvAsset(path);
        }

        private static void ApplyIngredientProfiles(RecipeLevelData level, List<Dictionary<string, string>> rows, Dictionary<string, IngredientData> ingredients)
        {
            foreach (Dictionary<string, string> row in rows)
            {
                string ingredientId = Get(row, "IngredientID");
                if (!ingredients.TryGetValue(ingredientId, out IngredientData ingredient))
                {
                    Debug.LogWarning("Unknown profile ingredient ID in CSV: " + ingredientId);
                    continue;
                }

                LevelIngredientProfile profile = level.ingredientProfiles.FirstOrDefault(x => x != null && x.ingredient == ingredient);
                if (profile == null)
                {
                    profile = new LevelIngredientProfile { ingredient = ingredient };
                    level.ingredientProfiles.Add(profile);
                }

                string tag = Get(row, "ProfileTag");
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    profile.profileTag = tag;
                }

                string trait = Get(row, "TraitDescription");
                if (!string.IsNullOrWhiteSpace(trait))
                {
                    profile.levelTraitDescription = trait;
                }

                MechanicType mechanic = ParseEnum(Get(row, "Mechanic"), MechanicType.IngredientSelection);
                EnableProfileMechanic(profile, mechanic);
                ApplyProfileTarget(profile, row, mechanic);

                string hint = Get(row, "HintText");
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    profile.responseHintRules.Add(new IngredientResponseHintRule
                    {
                        ruleId = Get(row, "RuleID"),
                        priority = ParseInt(Get(row, "Priority"), 0),
                        mechanic = mechanic,
                        matchForce = ParseBool(Get(row, "MatchForce")),
                        forceValue = ParseEnum(Get(row, "ForceValue"), profile.targetForceLevel),
                        matchSpeed = ParseBool(Get(row, "MatchSpeed")),
                        speedValue = ParseEnum(Get(row, "SpeedValue"), profile.targetSpeedLevel),
                        matchRatio = ParseBool(Get(row, "MatchRatio")),
                        ratioValue = ParseEnum(Get(row, "RatioValue"), profile.targetRatioLevel),
                        matchCombination = ParseBool(Get(row, "MatchCombination")),
                        combinationKey = Get(row, "CombinationKey"),
                        hintText = hint
                    });
                }
            }
        }

        private static void EnableProfileMechanic(LevelIngredientProfile profile, MechanicType mechanic)
        {
            if (profile.checkedMechanics == null)
            {
                profile.checkedMechanics = new EnabledMechanics();
            }

            switch (mechanic)
            {
                case MechanicType.IngredientSelection:
                    profile.checkedMechanics.enableIngredientSelection = true;
                    break;
                case MechanicType.IngredientOrder:
                    profile.checkedMechanics.enableIngredientOrder = true;
                    break;
                case MechanicType.Ratio:
                    profile.checkedMechanics.enableRatio = true;
                    break;
                case MechanicType.Combination:
                    profile.checkedMechanics.enableCombination = true;
                    break;
                case MechanicType.Force:
                    profile.checkedMechanics.enableForce = true;
                    break;
                case MechanicType.Speed:
                    profile.checkedMechanics.enableSpeed = true;
                    break;
                case MechanicType.GrindDuration:
                    profile.checkedMechanics.enableGrindDuration = true;
                    break;
            }
        }

        private static void ApplyProfileTarget(LevelIngredientProfile profile, Dictionary<string, string> row, MechanicType mechanic)
        {
            switch (mechanic)
            {
                case MechanicType.Force:
                    profile.targetForceLevel = ParseEnum(Get(row, "TargetValue"), profile.targetForceLevel);
                    break;
                case MechanicType.Speed:
                    profile.targetSpeedLevel = ParseEnum(Get(row, "TargetValue"), profile.targetSpeedLevel);
                    break;
                case MechanicType.Ratio:
                    profile.targetRatioLevel = ParseEnum(Get(row, "TargetValue"), profile.targetRatioLevel);
                    break;
                case MechanicType.IngredientOrder:
                    profile.targetOrderIndex = ParseInt(Get(row, "TargetValue"), profile.targetOrderIndex);
                    break;
                case MechanicType.Combination:
                    profile.targetCombinationKey = Get(row, "TargetValue");
                    break;
                case MechanicType.GrindDuration:
                    ApplyDurationTarget(profile, Get(row, "TargetValue"));
                    break;
            }
        }

        private static void ApplyDurationTarget(LevelIngredientProfile profile, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string[] parts = value.Split('-');
            if (parts.Length != 2)
            {
                return;
            }

            profile.minGrindDuration = ParseFloat(parts[0], profile.minGrindDuration);
            profile.maxGrindDuration = ParseFloat(parts[1], profile.maxGrindDuration);
        }

        private static void AssignOpenSceneReferences(List<IngredientData> ingredients, List<RecipeLevelData> levels)
        {
            TestLevelInitializer initializer = UnityEngine.Object.FindFirstObjectByType<TestLevelInitializer>();
            if (initializer != null)
            {
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

            List<List<string>> rows = ParseCsv(ReadAllTextShared(fullPath));
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

        private static string ReadAllTextShared(string fullPath)
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static List<Dictionary<string, string>> ReadOptionalCsvAsset(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            return File.Exists(fullPath) ? ReadCsvAsset(assetPath) : new List<Dictionary<string, string>>();
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

        private static bool ParseBool(string value)
        {
            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value == "1";
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

        private static void TryGenerateIngredientPrefabs()
        {
            Type generatorType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("TheTasteReviver.EditorTools.IngredientPrefabGenerator"))
                .FirstOrDefault(type => type != null);
            if (generatorType == null)
            {
                return;
            }

            generatorType.GetMethod("GenerateAndAssignIngredientPrefabs")?.Invoke(null, null);
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
