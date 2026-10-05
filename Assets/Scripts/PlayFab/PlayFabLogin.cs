using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Hazari.Backend
{
    /// <summary>
    /// Account login. Does not run again on every scene load.
    /// </summary>
    public sealed class PlayFabLogin : MonoBehaviour
    {
        public bool IsLoggedIn => PlayFabClientAPI.IsClientLoggedIn();

        public void LoginWithCustomId(string customId, Action<bool> onComplete)
        {
            if (IsLoggedIn)
            {
                if (onComplete != null)
                    onComplete(true);
                return;
            }

            if (string.IsNullOrEmpty(customId))
            {
                if (onComplete != null)
                    onComplete(false);
                return;
            }

            var request = new LoginWithCustomIDRequest
            {
                CustomId = customId,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(request, _ =>
            {
                if (onComplete != null)
                    onComplete(true);
            }, error =>
            {
                Debug.LogWarning("PlayFab login failed: " + error.GenerateErrorReport());
                if (onComplete != null)
                    onComplete(false);
            });
        }
    }
}
