namespace Hazari.Players
{
    public enum ConnectionState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Reconnecting = 3
    }

    public enum ReadyState
    {
        NotReady = 0,
        Ready = 1
    }

    /// <summary>
    /// Identity and match status for one seat. Cards are ids on <see cref="PlayerHand"/>, not scene objects.
    /// </summary>
    public sealed class PlayerData
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarId { get; set; } = string.Empty;
        public int Score { get; set; }
        public ConnectionState Connection { get; set; } = ConnectionState.Disconnected;
        public ReadyState Ready { get; set; } = ReadyState.NotReady;
        public bool IsTurn { get; set; }
        public bool IsLocalHuman { get; set; }
        public bool IsAi { get; set; }
        public PlayerHand Hand { get; } = new PlayerHand();
    }
}
