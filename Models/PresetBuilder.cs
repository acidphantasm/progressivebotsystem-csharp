namespace _progressiveBotSystem.Models;

public class PresetItemView
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Category { get; set; }
    public string CurrentDropZone { get; set; }
    public double Weight { get; set; }
    public string botType { get; set; }
    public bool IsGenerationItem { get; set; } = false;
}