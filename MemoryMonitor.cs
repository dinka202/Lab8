using System;
using System.Diagnostics;
using System.Threading;

public class MemoryMonitor
{
    private readonly Settings settings;
    private readonly PerformanceCounter memoryCounter;
    private long previousMemory;
    private DateTime lastCheckTime;

    public MemoryMonitor(Settings settings)
    {
        this.settings = settings;
        memoryCounter = new PerformanceCounter("Process", "Private Bytes", Process.GetCurrentProcess().ProcessName);
        previousMemory = GetCurrentMemoryUsage();
        lastCheckTime = DateTime.Now;
    }

    private long GetCurrentMemoryUsage()
    {
        float memoryUsageFloat = memoryCounter.NextValue();
        return (long)memoryUsageFloat;
    }

    public bool ShouldRestart()
    {
        long currentMemory = GetCurrentMemoryUsage();
        DateTime now = DateTime.Now;

        if (currentMemory > settings.MemoryThresholdMB * 1024 * 1024)
        {
            Console.WriteLine($"Порог памяти превышен: {currentMemory / 1024 / 1024} МБ > {settings.MemoryThresholdMB} МБ");
            return true;
        }

        TimeSpan timeDiff = now - lastCheckTime;
        if (timeDiff.TotalSeconds > 0)
        {
            double memoryGrowthRate = (currentMemory - previousMemory) / timeDiff.TotalSeconds;
            long growthInWindow = (long)(memoryGrowthRate * settings.RapidGrowthTimeWindowSeconds);

            if (growthInWindow > settings.RapidGrowthThresholdMB * 1024 * 1024)
            {
                Console.WriteLine($"Быстрый рост памяти: {growthInWindow / 1024 / 1024} МБ за {settings.RapidGrowthTimeWindowSeconds} сек > {settings.RapidGrowthThresholdMB} МБ");
                return true;
            }
        }

        previousMemory = currentMemory;
        lastCheckTime = now;
        return false;
    }
}
