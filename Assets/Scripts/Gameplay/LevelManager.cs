using System.Collections.Generic;
using UnityEngine;

namespace TheTasteReviver
{
    public class LevelManager : MonoBehaviour
    {
        public List<RecipeLevelData> levels = new List<RecipeLevelData>();
        public RecipeAttemptManager attemptManager;
        public UIManager uiManager;
        public HintManager hintManager;
        public LevelIngredientDisplayManager ingredientDisplayManager;

        public int CurrentLevelIndex { get; private set; }
        public RecipeLevelData CurrentLevel => levels.Count == 0 ? null : levels[Mathf.Clamp(CurrentLevelIndex, 0, levels.Count - 1)];

        private void Start()
        {
            int levelIndex = GameSceneReturnState.TryConsumePendingLevelIndex(out int pendingLevelIndex)
                ? pendingLevelIndex
                : CurrentLevelIndex;
            LoadLevel(levelIndex);
        }

        public void LoadLevel(int index)
        {
            if (levels.Count == 0)
            {
                return;
            }

            CurrentLevelIndex = Mathf.Clamp(index, 0, levels.Count - 1);
            attemptManager?.SetLevel(CurrentLevel);
            ingredientDisplayManager?.ShowLevelIngredients(CurrentLevel);
            hintManager?.ResetHints();
            uiManager?.ShowLevel(CurrentLevel);
        }

        public void NextLevel()
        {
            if (levels.Count == 0)
            {
                return;
            }

            LoadLevel((CurrentLevelIndex + 1) % levels.Count);
        }
    }
}
