using System;
using System.Collections.Generic;

namespace TheTasteReviver
{
    [Serializable]
    public class HintSettings
    {
        public List<MechanicType> hintPriority = new List<MechanicType>
        {
            MechanicType.IngredientOrder,
            MechanicType.Ratio,
            MechanicType.Combination,
            MechanicType.Speed,
            MechanicType.Force
        };

        public int maxOrderHints = 5;
        public int maxRatioHints = 5;
        public int maxCombinationHints = 3;
        public int maxProcessHints = 2;
    }
}
