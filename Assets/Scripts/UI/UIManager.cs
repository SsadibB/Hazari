using Hazari.Utilities;

namespace Hazari.UI
{
    public enum UiPanel
    {
        None = 0,
        MainMenu = 1,
        Mode = 2,
        Lobby = 3,
        PrivateRoom = 4,
        Loading = 5,
        Game = 6,
        Result = 7,
        Profile = 8,
        Settings = 9,
        HowToPlay = 10
    }

    /// <summary>
    /// Shows and hides panels. Buttons call managers. This type does not contain networking or Hazari rules.
    /// </summary>
    public sealed class UIManager : PersistentSingleton<UIManager>
    {
        public UiPanel ActivePanel { get; private set; } = UiPanel.None;

        public void Show(UiPanel panel)
        {
            ActivePanel = panel;
        }
    }
}
