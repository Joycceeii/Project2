using System;

namespace TheTasteReviver
{
    [Serializable]
    public class IngredientInstance
    {
        public IngredientData ingredient;
        public int amount = 1;

        public IngredientInstance(IngredientData ingredient, int amount)
        {
            this.ingredient = ingredient;
            this.amount = amount;
        }
    }
}
