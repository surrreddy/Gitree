using System;
using System.IO;

internal static class Program
{
    /// <summary>
    /// Exit codes:
    /// 0 = .gitignore found
    /// 2 = .gitignore missing (expected precondition)
    /// 1 = unexpected error
    /// </summary>
    private static int Main(string[] args)
    {
        try
        {
            string cwd = Directory.GetCurrentDirectory();
            string gi  = Path.Combine(cwd, ".gitignore");

            if (File.Exists(gi))
            {
                Console.WriteLine($"✅ .gitignore found in: {cwd}");
                return 0;
            }

            Console.WriteLine($"❌ .gitignore NOT found in: {cwd}");
            Console.WriteLine("This tool requires a .gitignore at the project root.");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
