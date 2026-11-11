namespace DiceDrive.Turn
{
    public enum TurnPhase
    {
        NotStarted,
        TurnStart,
        AwaitingRoll,
        AwaitingCard,
        AwaitingPathChoice,
        AwaitingExitChoice,
        Drawing,
        Reward,
        Loading,
        LevelComplete,
        LevelRetry,
        RunOver
    }
}
