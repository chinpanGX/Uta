namespace Uta.Client.Application.Player
{
    public sealed class PlayerSignInResult
    {
        private PlayerSignInResult(PlayerSignInOutcome outcome, PlayerSnapshot snapshot, string errorText)
        {
            Outcome = outcome;
            Snapshot = snapshot;
            ErrorText = errorText;
        }

        public PlayerSignInOutcome Outcome { get; }
        public PlayerSnapshot Snapshot { get; }
        public string ErrorText { get; }

        public static PlayerSignInResult Success(PlayerSnapshot snapshot)
        {
            return new PlayerSignInResult(PlayerSignInOutcome.Succeeded, snapshot, null);
        }

        public static PlayerSignInResult NotCreated()
        {
            return new PlayerSignInResult(PlayerSignInOutcome.PlayerNotCreated, null, null);
        }

        public static PlayerSignInResult Failure(string errorText)
        {
            return new PlayerSignInResult(PlayerSignInOutcome.Failed, null, errorText);
        }
    }
}
