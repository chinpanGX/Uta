namespace Uta.Client.Application.Player
{
    public sealed class PlayerSignUpResult
    {
        private PlayerSignUpResult(bool succeeded, bool created, string errorText)
        {
            Succeeded = succeeded;
            Created = created;
            ErrorText = errorText;
        }

        public bool Succeeded { get; }

        /// <summary>今回作ったか（2 回目以降は false）。</summary>
        public bool Created { get; }
        public string ErrorText { get; }

        public static PlayerSignUpResult Success(bool created)
        {
            return new PlayerSignUpResult(true, created, null);
        }

        public static PlayerSignUpResult Failure(string errorText)
        {
            return new PlayerSignUpResult(false, false, errorText);
        }
    }
}
