using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Data;

public class WordProcessor
{
    private IWebDriver driver;
    private Settings settings;
    private HashSet<string> existingWords;
    private WebDriverWait wait;
    private static int timeoutSeconds = 30;

    public WordProcessor()
    {
        LoadSettings();
        existingWords = LoadExistingWords();
        InitializeEdgeDriver();
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
    }

    private void InitializeEdgeDriver()
    {
        var options = new EdgeOptions();
        driver = new EdgeDriver(options);
    }

    private void LoadSettings()
    {
        var settingsJson = File.ReadAllText("Settings.json");
        settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
    }

    private HashSet<string> LoadExistingWords()
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(settings.WordsFilePath))
        {
            foreach (var line in File.ReadLines(settings.WordsFilePath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    words.Add(line.Trim().ToLower());
            }
        }
        return words;
    }

    public void ProcessWords()
    {
        try
        {
            driver.Navigate().GoToUrl("https://www.bukvarix.com/");
            LogStep("Переход на сайт выполнен");

            var words = File.ReadLines(settings.WordsFilePath).ToList();
            for (int i = settings.LastProcessedWordIndex; i < words.Count; i++)
            {
                var word = words[i].Trim();
                if (string.IsNullOrWhiteSpace(word)) continue;

                ProcessSingleWord(word);
                settings.LastProcessedWordIndex = i + 1;
                SaveSettings();
            }
        }
        finally
        {
            driver.Quit();
        }
    }

    private void ProcessSingleWord(string word)
    {
        Console.WriteLine($"Обработка слова: {word}");
        LogStep($"Начинаем обработку слова '{word}'");

        // Шаг 1: Ввод слова в поисковое поле
        LogStep("Ожидаем появления поля ввода...");
        var searchInput = wait.Until(
            d =>
            {
                var element = d.FindElement(By.Name("q"));
            LogStep("Поле ввода найдено");
                return element;
            }
        );
        searchInput.Clear();
        searchInput.SendKeys(word);
        LogStep("Слово введено в поисковое поле");
        LogStep("Ожидаем появления кнопки «Найти»...");

        IWebElement findButton = wait.Until(
        ExpectedConditions.ElementToBeClickable(
        By.CssSelector("div.search-form-submit-index input[type='submit']")
        )
        );
        LogStep("Кнопка «Найти» найдена и готова к клику");

        if (findButton.GetAttribute("value") != "Найти")
        {
            throw new Exception("Некорректное значение кнопки 'Найти'");
        }

        findButton.Click();
        LogStep("Кнопка «Найти» нажата");
        LogStep("Ожидаем загрузки результатов поиска...");
        WaitForPageLoad();
        LogStep("Результаты поиска загружены");
        LogStep("Ожидаем появления кнопки скачивания CSV...");
        var downloadButton = wait.Until(
            d =>
            {
                var element = d.FindElement(By.XPath("//a[contains(text(), 'Скачать (файл .csv)')]"));
                LogStep("Кнопка скачивания CSV найдена");
                return element;
            }
        );
        downloadButton.Click();
        LogStep("Кнопка скачивания CSV нажата");

        Thread.Sleep(5000);
        LogStep("Ожидание завершения скачивания (5 сек)");

        ParseAndAddWordsFromCsv();
    }


    private void ParseAndAddWordsFromCsv()
    {
        var csvFiles = Directory.GetFiles(settings.CsvDownloadPath, "*.csv")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToArray();

        if (!Directory.Exists(settings.CsvDownloadPath))
        {
            Console.WriteLine($"Директория не найдена: {settings.CsvDownloadPath}");
            return;
        }

        var latestCsvPath = csvFiles.FirstOrDefault();

        if (latestCsvPath == null)
        {
            Console.WriteLine("CSV-файлы не найдены.");
            return;
        }

        if (string.IsNullOrEmpty(latestCsvPath))
        {
            Console.WriteLine("Путь к файлу пустой.");
            return;
        }

        if (!File.Exists(latestCsvPath))
        {
            Console.WriteLine($"Файл не найден по пути: {latestCsvPath}");
            return;
        }

        Console.WriteLine($"Обрабатывается файл: {latestCsvPath}");
        Console.WriteLine($"Дата модификации: {File.GetLastWriteTime(latestCsvPath)}");

        try
        {
            using (var fileStream = File.OpenRead(latestCsvPath))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Read();
                        csv.ReadHeader();

                        while (csv.Read())
                        {
                            var newWord = csv.GetField<string>(0).Trim().ToLower();
                            AddWordIfNotExists(newWord);
                        }
                    }
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Нет прав на чтение файла: {latestCsvPath}. Ошибка: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка ввода-вывода при работе с файлом: {latestCsvPath}. Ошибка: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Непредвиденная ошибка при обработке файла: {latestCsvPath}. Ошибка: {ex.Message}");
        }
    }

    private void AddWordIfNotExists(string word)
    {
        if (!existingWords.Contains(word))
        {
            existingWords.Add(word);
            File.AppendAllLines(settings.WordsFilePath, new[] { word });
        }
    }

    private void WaitForPageLoad()
    {
        WebDriverWait pageLoadWait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        pageLoadWait.Until(d =>
        {
            var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString();
            bool isLoaded = state.Equals("complete", StringComparison.OrdinalIgnoreCase);
            if (!isLoaded) LogStep("Документ ещё не загружен, ждём...");
            return isLoaded;
        });
        LogStep("Страница полностью загружена (document.readyState = 'complete')");
    }

    private void SaveSettings()
    {
        var settingsJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText("Settings.json", settingsJson);
    }

    private void LogStep(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
