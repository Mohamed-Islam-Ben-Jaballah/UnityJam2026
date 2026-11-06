namespace DiceDrive.Turn
{
    public enum TurnPhase
    {
        NotStarted,
        TurnStart,
        AwaitingRoll,
        AwaitingCard,
        Drawing,
        LevelComplete,
        LevelRetry,
        RunOver
    }
}
