using System;
using System.Collections.Generic;

namespace Game.UI
{
    public enum Language
    {
        English,
        Vietnamese
    }

    /// <summary>
    /// Every player-facing string in the menus, in both languages.
    ///
    /// Deliberately a plain table in code rather than a file format or a package: the whole game has
    /// about a hundred strings, all of them in these menus, and a dictionary is something anyone on
    /// the team can edit without learning a tool. If the game ever grows dialogue, move to
    /// Unity Localization then — not before.
    ///
    /// Usage: <c>Localization.Get("settings.camera_shake")</c>. A missing key returns the key itself
    /// rather than throwing, so a typo shows up on screen instead of taking the menu down.
    /// </summary>
    public static class Localization
    {
        public static Language Current { get; private set; } = Language.English;

        /// <summary>Raised after the language changes so open screens can rebuild their labels.</summary>
        public static event Action OnLanguageChanged;

        public static readonly string[] LanguageNames = { "English", "Tiếng Việt" };

        public static void SetLanguage(Language language)
        {
            if (Current == language) return;
            Current = language;
            OnLanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            if (strings.TryGetValue(key, out string[] variants))
                return variants[(int)Current];
            return key;
        }

        /// <summary>For rows whose options are a fixed list (Low/Medium/High, ON/OFF...).</summary>
        public static string[] GetArray(string key)
        {
            if (arrays.TryGetValue(key, out string[][] variants))
                return variants[(int)Current];
            return new[] { key };
        }

        // Index 0 is English, index 1 is Vietnamese — same order as the Language enum.
        private static readonly Dictionary<string, string[]> strings = new Dictionary<string, string[]>
        {
            // ---- screens and navigation
            { "menu.play", new[] { "Play", "Chơi" } },
            { "menu.options", new[] { "Options", "Tùy chỉnh" } },
            { "menu.quit_desktop", new[] { "Quit To Desktop", "Thoát game" } },
            { "menu.resume", new[] { "Resume", "Tiếp tục" } },
            { "menu.quit_main", new[] { "Quit To Main", "Về màn hình chính" } },
            { "menu.back", new[] { "Back", "Quay lại" } },
            { "menu.reset_defaults", new[] { "Reset Defaults", "Khôi phục mặc định" } },

            // ---- tabs
            { "tab.game", new[] { "Game", "Trò chơi" } },
            { "tab.video", new[] { "Video", "Hình ảnh" } },
            { "tab.graphics", new[] { "Graphics", "Đồ họa" } },
            { "tab.audio", new[] { "Audio", "Âm thanh" } },
            { "tab.controls", new[] { "Controls", "Phím" } },

            // ---- game tab
            { "game.language", new[] { "Language", "Ngôn ngữ" } },
            { "game.invert_horizontal", new[] { "Camera Control Horizon", "Đảo chiều ngang" } },
            { "game.invert_vertical", new[] { "Camera Control Vertical", "Đảo chiều dọc" } },
            { "game.sensitivity_horizontal", new[] { "Camera Sensitivity Horizon", "Độ nhạy ngang" } },
            { "game.sensitivity_vertical", new[] { "Camera Sensitivity Vertical", "Độ nhạy dọc" } },
            { "game.acceleration", new[] { "Camera Acceleration", "Độ mượt camera" } },
            { "game.shake", new[] { "Camera Shake", "Rung camera" } },

            // ---- video tab
            { "video.display_mode", new[] { "Display Mode", "Chế độ hiển thị" } },
            { "video.resolution", new[] { "Resolution", "Độ phân giải" } },
            { "video.fov", new[] { "FOV", "Góc nhìn" } },
            { "video.frame_rate", new[] { "Frame Rate Limit", "Giới hạn FPS" } },
            { "video.vsync", new[] { "VSync", "Đồng bộ dọc" } },
            { "video.motion_blur", new[] { "Motion Blur", "Nhòe chuyển động" } },
            { "video.brightness", new[] { "Brightness", "Độ sáng" } },

            // ---- graphics tab
            { "graphics.preset", new[] { "Preset", "Cấu hình sẵn" } },
            { "graphics.render_scale", new[] { "Resolution Scale", "Tỉ lệ dựng hình" } },
            { "graphics.lighting", new[] { "Lighting Quality", "Chất lượng ánh sáng" } },
            { "graphics.reflection", new[] { "Reflection", "Phản chiếu" } },
            { "graphics.anti_aliasing", new[] { "Anti Aliasing", "Khử răng cưa" } },
            { "graphics.post", new[] { "Post Processing Quality", "Chất lượng hậu kỳ" } },

            // ---- audio tab
            { "audio.master", new[] { "Master Volume", "Âm lượng chung" } },
            { "audio.music", new[] { "Music Volume", "Nhạc nền" } },
            { "audio.sfx", new[] { "Sound Volume", "Hiệu ứng" } },

            // ---- controls tab
            { "binding.MoveForward", new[] { "Forward", "Đi tới" } },
            { "binding.MoveBack", new[] { "Back", "Đi lùi" } },
            { "binding.MoveLeft", new[] { "Left", "Sang trái" } },
            { "binding.MoveRight", new[] { "Right", "Sang phải" } },
            { "binding.MoveForwardAlt", new[] { "Forward (alt)", "Đi tới (phụ)" } },
            { "binding.MoveBackAlt", new[] { "Back (alt)", "Đi lùi (phụ)" } },
            { "binding.MoveLeftAlt", new[] { "Left (alt)", "Sang trái (phụ)" } },
            { "binding.MoveRightAlt", new[] { "Right (alt)", "Sang phải (phụ)" } },
            { "binding.Jump", new[] { "Jump", "Nhảy" } },
            { "binding.Sprint", new[] { "Sprint", "Chạy" } },
            { "binding.Interact", new[] { "Interact", "Tương tác" } },

            // ---- rebind feedback
            { "rebind.in_use", new[] { "KEY ALREADY IN USE", "PHÍM ĐÃ ĐƯỢC DÙNG" } },
            { "rebind.reserved", new[] { "KEY RESERVED", "PHÍM BỊ GIỮ CHỖ" } },
            { "rebind.not_rebindable", new[] { "NOT REBINDABLE", "KHÔNG ĐỔI ĐƯỢC" } },
            { "rebind.not_found", new[] { "BINDING NOT FOUND", "KHÔNG TÌM THẤY PHÍM" } },
            { "rebind.unbound", new[] { "UNBOUND", "TRỐNG" } },
            { "rebind.waiting", new[] { "...", "..." } },
            { "rebind.cancelled", new[] { "", "" } },
            {
                "rebind.prompt",
                new[] { "Press a key…  Esc to cancel", "Bấm một phím…  Esc để huỷ" }
            },
            {
                "rebind.prompt_gamepad",
                new[] { "Press a button…  B to cancel", "Bấm một nút…  B để huỷ" }
            },
            { "controls.keyboard", new[] { "Keyboard", "Bàn phím" } },
            { "controls.gamepad", new[] { "Gamepad", "Tay cầm" } },
            { "rebind.restored", new[] { "DEFAULTS RESTORED", "ĐÃ KHÔI PHỤC MẶC ĐỊNH" } },
            { "rebind.stick", new[] { "L-Stick / D-Pad", "Cần trái / D-Pad" } },
            {
                "rebind.stick_locked",
                new[]
                {
                    "Movement uses the whole stick — nothing to rebind",
                    "Di chuyển dùng cả cần analog — không có gì để đổi"
                }
            }
        };

        private static readonly Dictionary<string, string[][]> arrays = new Dictionary<string, string[][]>
        {
            {
                "opt.display_mode", new[]
                {
                    new[] { "Fullscreen", "Borderless", "Windowed" },
                    new[] { "Toàn màn hình", "Không viền", "Cửa sổ" }
                }
            },
            {
                "opt.quality", new[]
                {
                    new[] { "Low", "Medium", "High" },
                    new[] { "Thấp", "Trung bình", "Cao" }
                }
            },
            {
                "opt.preset", new[]
                {
                    new[] { "Low", "Medium", "High", "Custom" },
                    new[] { "Thấp", "Trung bình", "Cao", "Tùy chỉnh" }
                }
            },
            {
                "opt.on_off", new[]
                {
                    new[] { "OFF", "ON" },
                    new[] { "TẮT", "BẬT" }
                }
            },
            {
                "opt.invert", new[]
                {
                    new[] { "Normal", "Inverted" },
                    new[] { "Bình thường", "Đảo ngược" }
                }
            },
            {
                "opt.vsync", new[]
                {
                    new[] { "OFF", "ON", "Half" },
                    new[] { "TẮT", "BẬT", "Một nửa" }
                }
            },
            {
                "opt.frame_rate", new[]
                {
                    new[] { "30", "60", "120", "144", "240", "Unlimited" },
                    new[] { "30", "60", "120", "144", "240", "Không giới hạn" }
                }
            }
        };
    }
}
