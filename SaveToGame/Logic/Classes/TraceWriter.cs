namespace SaveToGameWpf.Logic.Classes
{
    internal static class TraceWriter
    {
        public static bool Trace { get; set; }

        public static void WriteLine(string info)
        {
            if (Trace)
            {
                System.Diagnostics.Trace.WriteLine(info);
            }
        }

        public static void WriteLine<T>(T info)
        {
            WriteLine(info.ToString());
        }
    }
}
