namespace MorWalPizVideo.Operations
{
    static class Utils
    {
        public static string[] AskFor(params string[] args)
        {
            var results = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine($"Enter {args[i]}");
                results[i] = Console.ReadLine() ?? string.Empty;
            }
            return results;
        }
    }
}
