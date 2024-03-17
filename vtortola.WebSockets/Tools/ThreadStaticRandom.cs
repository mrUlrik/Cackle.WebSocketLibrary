﻿/*
	Copyright (c) 2017 Denis Zykov
	License: https://opensource.org/licenses/MIT
*/

namespace vtortola.WebSockets.Tools
{
    public sealed class ThreadStaticRandom
    {
        [ThreadStatic]
        private static Random instance;

        public static Random Instance
        {
            get
            {
                if (instance == null)
                {
                    var seed = unchecked((int)(17 * (DateTime.UtcNow.Ticks % int.MaxValue)));
                    instance = new Random(seed);
                }
                return instance;
            }
        }

        public static int Next()
        {
            return Instance.Next();
        }
        public static int NextNotZero()
        {
            var next = Instance.Next();
            while (next == 0)
            {
                next = Instance.Next();
            }

            return next;
        }
        public static double NextDouble()
        {
            return Instance.NextDouble();
        }
        public static void NextBytes(ArraySegment<byte> arraySegment)
        {
            for (var i = 0; i < arraySegment.Count; i += 4)
            {
                var next = NextNotZero();

                arraySegment.Array[arraySegment.Offset + i] = (byte)(next >> 24);
                if (i + 1 < arraySegment.Count)
                    arraySegment.Array[arraySegment.Offset + i + 1] = (byte)(next >> 16);
                if (i + 2 < arraySegment.Count)
                    arraySegment.Array[arraySegment.Offset + i + 2] = (byte)(next >> 8);
                if (i + 3 < arraySegment.Count)
                    arraySegment.Array[arraySegment.Offset + i + 3] = (byte)next;
            }
        }
    }
}