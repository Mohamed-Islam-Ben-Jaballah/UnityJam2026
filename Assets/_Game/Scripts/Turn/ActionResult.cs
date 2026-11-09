namespace DiceDrive.Turn
{
    public enum ActionStatus
    {
        Success,
        InvalidPhase,
        InvalidCard,
        NotPlayable,
        InvalidTarget,
        CannotReroll,
        DeckExhausted,
        RunOver,
        LevelComplete
    }

    public readonly struct ActionResult
    {
        public ActionStatus Status { get; }
        public string Message { get; }
        public bool Succeeded => Status == ActionStatus.Success;

        public ActionResult(ActionStatus status, string message = null)
        {
            Status = status;
            Message = message;
        }

        public static ActionResult Ok()
        {
            return new ActionResult(ActionStatus.Success);
        }

        public static ActionResult Fail(ActionStatus status, string message = null)
        {
            return new ActionResult(status, message);
        }
    }
}
