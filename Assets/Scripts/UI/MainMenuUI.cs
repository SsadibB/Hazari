using Hazari.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hazari.UI
{
    /// <summary>
    /// First screen. Offline starts a bot match. Online and Multiplayer open their own panels.
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] GameObject home;
        [SerializeField] GameObject onlinePanel;
        [SerializeField] GameObject multiplayerPanel;
        [SerializeField] Text onlineStatus;
        [SerializeField] Text roomCodeLabel;
        [SerializeField] InputField roomCodeInput;
        [SerializeField] Text multiplayerStatus;

        public void PlayOffline()
        {
            SceneManager.LoadScene(SceneNames.Game);
        }

        public void ShowOnline()
        {
            SetPage(onlinePanel);
            SetText(onlineStatus, "Find a table with players online.");
        }

        public void ShowMultiplayer()
        {
            SetPage(multiplayerPanel);
            SetText(multiplayerStatus, "Host a private room, or enter a code to join.");
        }

        public void ShowHome()
        {
            SetPage(home);
        }

        public void FindOnlineMatch()
        {
            SetText(onlineStatus, "Looking for an open table...");
        }

        public void CreatePrivateRoom()
        {
            var code = RandomCode();
            if (roomCodeLabel != null)
                roomCodeLabel.text = "ROOM CODE: " + code;
            SetText(multiplayerStatus, "Room " + code + " is ready. Share the code, then start when the table is full.");
        }

        public void JoinPrivateRoom()
        {
            var code = roomCodeInput != null ? roomCodeInput.text.Trim().ToUpperInvariant() : string.Empty;
            if (code.Length < 4)
            {
                SetText(multiplayerStatus, "Enter the room code from the host.");
                return;
            }

            SetText(multiplayerStatus, "Joining room " + code + "...");
        }

        void SetPage(GameObject page)
        {
            if (home != null)
                home.SetActive(page == home);
            if (onlinePanel != null)
                onlinePanel.SetActive(page == onlinePanel);
            if (multiplayerPanel != null)
                multiplayerPanel.SetActive(page == multiplayerPanel);
        }

        static void SetText(Text label, string value)
        {
            if (label != null)
                label.text = value;
        }

        static string RandomCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var chars = new char[6];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = alphabet[Random.Range(0, alphabet.Length)];
            return new string(chars);
        }
    }
}
