using UnityEngine;

namespace TheTasteReviver
{
    [CreateAssetMenu(menuName = "The Taste Reviver/Ingredient Data", fileName = "IngredientData")]
    public class IngredientData : ScriptableObject
    {
        public string ingredientID;
        public string ingredientNameCN;
        public string ingredientNameEN;
        public string aromaType;
        [TextArea] public string initialDescription;
        public Color ingredientColor = Color.white;
        public Sprite icon;
        public GameObject prefab;
        public int defaultRatioValue = 1;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ingredientNameEN))
                {
                    return ingredientNameEN;
                }

                return string.IsNullOrWhiteSpace(ingredientID) ? name : ingredientID;
            }
        }
    }
}
