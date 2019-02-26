using System;

namespace olproxy
{
    class ConsoleSpinner
    {
        private static readonly char[] spinChars = new[] { '/', '-', '\\', '|' };
        private int idx;
        public DateTime LastUpdateTime;
        public bool Active;

        public void Spin()
        {
            Console.Write(new[] { spinChars[idx], '\r' });
            idx = (idx + 1) % spinChars.Length;
            LastUpdateTime = DateTime.UtcNow;
            Active = true;
        }

        public void Clear()
        {
            Console.Write(" \r");
            Active = false;
        }
    }
}
