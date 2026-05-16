using System;

class Program
{
    static void Main(string[] args)
    {
        var processor = new WordProcessor();
        try
        {
            processor.ProcessWords();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}
