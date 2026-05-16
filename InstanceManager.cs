using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class InstanceManager
{
    private readonly Settings settings;

    public InstanceManager(Settings settings)
    {
        this.settings = settings;
    }

    public int GetRunningInstancesCount()
    {
        string processName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        return Process.GetProcessesByName(processName).Length;
    }

    public bool IsExcessInstance()
    {
        int runningCount = GetRunningInstancesCount();
        return runningCount > settings.MaxInstances;
    }

    public void RestartWithReducedInstances()
    {
        Console.WriteLine("Перезапуск с уменьшением количества экземпляров...");
        KillExcessInstances();

        int optimalCount = Math.Max(1, settings.MaxInstances - 1);
        for (int i = 0; i < optimalCount; i++)
        {
            StartNewInstance();
        }
    }

    private void KillExcessInstances()
    {
        string processName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        var processes = Process.GetProcessesByName(processName)
            .OrderByDescending(p => p.Id)
            .Skip(settings.MaxInstances)
            .ToArray();

        foreach (var process in processes)
        {
            try
            {
                process.Kill();
                Console.WriteLine($"Завершён экземпляр с PID: {process.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при завершении процесса {process.Id}: {ex.Message}");
            }
        }
    }

    private void StartNewInstance()
    {
        try
        {
            Process.Start(Environment.GetCommandLineArgs()[0]);
            Console.WriteLine("Запущен новый экземпляр программы");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при запуске нового экземпляра: {ex.Message}");
        }
    }
}
