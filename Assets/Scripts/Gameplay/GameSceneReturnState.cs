namespace TheTasteReviver
{
    public static class GameSceneReturnState
    {
        private static int pendingLevelIndex = -1;

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
    }
}
