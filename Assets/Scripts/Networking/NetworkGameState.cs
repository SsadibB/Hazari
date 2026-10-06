using System;
using Fusion;
using Hazari.Rules;
using UnityEngine;

namespace Hazari.Networking
{
    /// <summary>
    /// Authoritative Fusion game state for multiplayer matches.
    /// Controls round points, total points, winner, current round, 1000-point check, and match completion.
    /// Clients mirror this authoritative state; local client score calculation is never trusted.
    /// </summary>
    public class NetworkGameState : NetworkBehaviour
    {
        [Networked] public int CurrentRound { get; set; }
        [Networked, Capacity(4)] public NetworkArray<int> TotalScores => default;
        [Networked, Capacity(4)] public NetworkArray<int> RoundScores => default;
        [Networked] public int MatchWinnerSeat { get; set; } = -1;
        [Networked] public NetworkBool IsMatchComplete { get; set; }
        [Networked] public NetworkBool IsRoundComplete { get; set; }

        public event Action<int[]> OnTotalScoresChanged;
        public event Action<int[]> OnRoundScoresChanged;
        public event Action<int, int> OnMatchFinished; // winnerSeat, winningScore

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                ResetMatchAuthority();
            }
        }

        /// <summary>
        /// Resets all match data for a fresh game. Authoritative: server/host only.
        /// </summary>
        public void ResetMatchAuthority()
        {
            if (!HasStateAuthority)
                return;

            CurrentRound = 1;
            MatchWinnerSeat = -1;
            IsMatchComplete = false;
            IsRoundComplete = false;

            for (var i = 0; i < HazariRules.PlayerCount; i++)
            {
                TotalScores.Set(i, 0);
                RoundScores.Set(i, 0);
            }
        }

        /// <summary>
        /// Authoritatively records round points, updates cumulative totals, and checks for 1000 points.
        /// </summary>
        public void ApplyRoundScoresAuthority(int[] points)
        {
            if (!HasStateAuthority || points == null || points.Length != HazariRules.PlayerCount)
                return;

            var highestTotal = 0;
            var leaderSeat = 0;

            for (var i = 0; i < HazariRules.PlayerCount; i++)
            {
                RoundScores.Set(i, points[i]);
                var newTotal = TotalScores[i] + points[i];
                TotalScores.Set(i, newTotal);

                if (newTotal > highestTotal)
                {
                    highestTotal = newTotal;
                    leaderSeat = i;
                }
            }

            IsRoundComplete = true;
            OnRoundScoresChanged?.Invoke(CopyRoundScores());
            OnTotalScoresChanged?.Invoke(CopyTotalScores());

            // Check if any player has reached 1000 total points
            if (highestTotal >= HazariRules.WinningScore)
            {
                MatchWinnerSeat = leaderSeat;
                IsMatchComplete = true;
                OnMatchFinished?.Invoke(leaderSeat, highestTotal);
                Debug.Log($"[NetworkGameState] Match completed authoritatively. Winner: Player {leaderSeat} with {highestTotal} points.");
            }
            else
            {
                Debug.Log($"[NetworkGameState] Round {CurrentRound} completed. Highest score: {highestTotal}/{HazariRules.WinningScore}. Next round ready.");
            }
        }

        /// <summary>
        /// Advances to the next round if the match is not complete.
        /// </summary>
        public bool AdvanceToNextRoundAuthority()
        {
            if (!HasStateAuthority || IsMatchComplete)
                return false;

            CurrentRound++;
            IsRoundComplete = false;
            for (var i = 0; i < HazariRules.PlayerCount; i++)
            {
                RoundScores.Set(i, 0);
            }

            return true;
        }

        public int GetTotalScore(int seat)
        {
            if (seat < 0 || seat >= HazariRules.PlayerCount)
                return 0;
            return TotalScores[seat];
        }

        public int GetRoundScore(int seat)
        {
            if (seat < 0 || seat >= HazariRules.PlayerCount)
                return 0;
            return RoundScores[seat];
        }

        public int[] CopyTotalScores()
        {
            var arr = new int[HazariRules.PlayerCount];
            for (var i = 0; i < HazariRules.PlayerCount; i++)
                arr[i] = TotalScores[i];
            return arr;
        }

        public int[] CopyRoundScores()
        {
            var arr = new int[HazariRules.PlayerCount];
            for (var i = 0; i < HazariRules.PlayerCount; i++)
                arr[i] = RoundScores[i];
            return arr;
        }
    }
}
