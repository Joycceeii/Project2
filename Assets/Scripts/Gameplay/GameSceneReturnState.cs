namespace TheTasteReviver
{
    public static class GameSceneReturnState
    {
        private static int pendingLevelIndex = -1;
        private static bool skipNextOpeningLevelTitle;

        public static void SetPendingLevelIndex(int index)
        {
            pendingLevelIndex = index;
        }

        public static bool TryConsumePendingLevelIndex(out int index)
        {
            index = pendingLevelIndex;
            pendingLevelIndex = -1;
            return index >= 0;
        }

        public static void MarkReturningFromExperimentLog()
        {
            skipNextOpeningLevelTitle = true;
        }

        public static bool ConsumeSkipOpeningLevelTitle()
        {
            bool shouldSkip = skipNextOpeningLevelTitle;
            skipNextOpeningLevelTitle = false;
            return shouldSkip;
        }
    }
}
