using System;

class Program
{
    static void Main(string[] args)
    {
        var processor = new WordProcessor();
        try
        {
            processor.ProcessWordsParallel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}
