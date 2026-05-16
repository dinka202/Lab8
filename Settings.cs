public class Settings
{
    public int LastProcessedWordIndex { get; set; } = 0;
    public string WordsFilePath { get; set; } = "words.txt";
    public string CsvDownloadPath { get; set; } = "downloads/";
    public long MemoryThresholdMB { get; set; } = 500;
    public int RapidGrowthTimeWindowSeconds { get; set; } = 30;
    public long RapidGrowthThresholdMB { get; set; } = 100;
    public int MaxInstances { get; set; } = 5;
}

