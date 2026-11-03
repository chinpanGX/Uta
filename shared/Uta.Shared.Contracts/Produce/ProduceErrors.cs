using VolTech.Server.Abstractions;

namespace Uta.Shared.Contracts.Produce
{
    public static class ProduceErrors
    {
        public static readonly ErrorCode StaleOperation = new ErrorCode("uta", "PRODUCE_STALE_OPERATION");
        public static readonly ErrorCode UnexpectedPhase = new ErrorCode("uta", "PRODUCE_UNEXPECTED_PHASE");
        public static readonly ErrorCode InvalidArgument = new ErrorCode("uta", "PRODUCE_INVALID_ARGUMENT");
        public static readonly ErrorCode InvalidFormation = new ErrorCode("uta", "PRODUCE_INVALID_FORMATION");
        public static readonly ErrorCode NotInProgress = new ErrorCode("uta", "PRODUCE_NOT_IN_PROGRESS");
        public static readonly ErrorCode SessionDiscarded = new ErrorCode("uta", "PRODUCE_SESSION_DISCARDED");
        public static readonly ErrorCode AlreadyInProgress = new ErrorCode("uta", "PRODUCE_ALREADY_IN_PROGRESS");
    }
}
