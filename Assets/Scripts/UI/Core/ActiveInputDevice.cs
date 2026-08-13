using System;
using UnityEngine.InputSystem;
// Call() is an extension on the observable that onAnyButtonPress returns.
using UnityEngine.InputSystem.Utilities;

namespace Game.UI
{
    /// <summary>Which family of device the player last actually used.</summary>
    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad
    }

    /// <summary>
    /// Tracks the device the player last pressed something on, so the UI can follow them.
    ///
    /// The naive test — <c>Gamepad.current != null</c> — is wrong: a controller plugged in and left
    /// on the desk would make the game show controller prompts while the player is typing. What
    /// matters is not what is connected but what was last touched, so this listens for actual button
    /// presses and remembers where they came from. Switching is instant and needs no setting.
    ///
    /// Deliberately ignores mouse movement and stick drift: only real presses count, otherwise a
    /// slightly worn stick would flip the UI back and forth on its own.
    /// </summary>
    public static class ActiveInputDevice
    {
        private static InputDeviceKind current = InputDeviceKind.KeyboardMouse;
        private static IDisposable subscription;

        /// <summary>Raised when the player switches between keyboard/mouse and a gamepad.</summary>
        public static event Action<InputDeviceKind> Changed;

        public static InputDeviceKind Current => current;
        public static bool UsingGamepad => current == InputDeviceKind.Gamepad;

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialise()
        {
            subscription?.Dispose();

            // Start on whatever is plugged in; the first press corrects it if that guess is wrong.
            current = Gamepad.current != null ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse;

            subscription = InputSystem.onAnyButtonPress.Call(control =>
            {
                InputDeviceKind kind = control.device is Gamepad
                    ? InputDeviceKind.Gamepad
                    : InputDeviceKind.KeyboardMouse;

                if (kind == current) return;
                current = kind;
                Changed?.Invoke(kind);
            });
        }
    }
}
