using System.Text.Json;

namespace CardAnalysis
{
    internal class AppSettings
    {
        public float  RotationStep        { get; set; } = 0.1f;
        public bool   OverlaySave         { get; set; } = true;
        public float  PixelWhiteThreshold { get; set; } = 0.80f;
        public float  LineWhiteThreshold  { get; set; } = 0.55f;
        public float  BorderTolerancePct  { get; set; } = 0.006f;
        public string RawImageFolder       { get; set; } = "";
        public string ProcessedImageFolder { get; set; } = "";

        // ── Persistence ───────────────────────────────────────────────────

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CardAnalysis", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* corrupted file — fall back to defaults */ }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* best-effort */ }
        }
    }
}
