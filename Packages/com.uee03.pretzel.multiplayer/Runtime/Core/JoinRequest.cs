namespace Pretzel.Multiplayer
{
    public sealed class JoinRequest : SessionRequest
    {
        public JoinKind Kind { get; set; }

        /// <summary>Join code, used when <see cref="Kind"/> is <see cref="JoinKind.Code"/>.</summary>
        public string Code { get; set; }

        /// <summary>Session id, used when <see cref="Kind"/> is <see cref="JoinKind.SessionId"/>.</summary>
        public string SessionId { get; set; }
    }
}
