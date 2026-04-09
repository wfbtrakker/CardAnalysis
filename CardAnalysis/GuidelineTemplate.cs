using System.Text.Json;

namespace CardAnalysis
{
    /// <summary>
    /// Stores guideline positions as fractions of image dimensions (0.0 – 1.0)
    /// so templates work correctly across images of different sizes.
    /// </summary>
    public class GuidelineTemplate
    {
        public string Name         { get; set; } = "Template";
        public double ZoomFactor   { get; set; } = 1.0;
        public double LeftOuter    { get; set; }
        public double LeftInner    { get; set; }
        public double RightInner   { get; set; }
        public double RightOuter   { get; set; }
        public double TopOuter     { get; set; }
        public double TopInner     { get; set; }
        public double BottomInner  { get; set; }
        public double BottomOuter  { get; set; }

        // ── File helpers ──────────────────────────────────────────────────

        public static string DefaultFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CardAnalysis", "templates");

        public static GuidelineTemplate? Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GuidelineTemplate>(json);
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
