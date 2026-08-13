using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Opens a <see cref="UIScreen"/> as soon as the scene starts.
    ///
    /// The design doc asks for each UI feature to live in its own scene so we don't fight over the
    /// same file in git. Those test scenes have no menu to open the screen for them, hence this.
    /// </summary>
    public class UIScreenAutoOpen : MonoBehaviour
    {
        [SerializeField] private UIScreen screen;

        private void Start()
        {
            if (screen == null) screen = GetComponent<UIScreen>();
            if (screen != null) screen.Open();
        }
    }
}
