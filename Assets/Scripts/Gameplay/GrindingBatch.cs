using System;
using System.Collections.Generic;

namespace TheTasteReviver
{
    [Serializable]
    public class GrindingBatch
    {
        public int batchID;
        public List<IngredientData> ingredientsInBatch = new List<IngredientData>();
        public List<IngredientData> ingredientOrderInBatch = new List<IngredientData>();
        public List<RatioRequirement> ratioPatternInBatch = new List<RatioRequirement>();
        public ForceLevel forceLevel;
        public SpeedLevel speedLevel;
        public float grindDuration;
    }
}
