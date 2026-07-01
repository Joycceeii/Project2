using System;

namespace TheTasteReviver
{
    [Serializable]
    public class FeedbackTextData
    {
        public string forceTooLight = "Grinding force is too light.";
        public string forceTooHeavy = "Grinding force is too heavy.";
        public string forceCorrect = "Grinding force is correct.";
        public string speedTooSlow = "Grinding speed is too slow.";
        public string speedTooFast = "Grinding speed is too fast.";
        public string speedCorrect = "Grinding speed is correct.";
        public string durationTooShort = "Grinding duration is too short.";
        public string durationCorrect = "Grinding duration is correct.";
        public string durationTooLong = "Grinding duration is too long.";
        public string selectionCorrect = "Ingredient selection is correct.";
        public string selectionWrong = "Ingredient selection does not match the target.";
        public string orderCorrect = "Ingredient order is correct.";
        public string orderPartial = "Ingredient order is partially correct.";
        public string orderWrong = "Ingredient order does not match the target.";
        public string ratioCorrect = "Ratio pattern is correct.";
        public string ratioAmbiguous = "Ratio pattern is still ambiguous.";
        public string combinationCorrect = "Combination pattern is correct.";
        public string combinationWrong = "Combination pattern does not match the target.";
    }
}
