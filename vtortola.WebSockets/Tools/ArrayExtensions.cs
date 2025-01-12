﻿using JetBrains.Annotations;

namespace vtortola.WebSockets.Tools
{
    internal static class ArrayExtensions
    {
        public static ResultT[] ConvertAll<SourceT, ResultT>([NotNull] this SourceT[] sourceArray, [NotNull, InstantHandle] Func<SourceT, ResultT> conversion)
        {
            if (sourceArray == null) throw new ArgumentNullException(nameof(sourceArray));
            if (conversion == null) throw new ArgumentNullException(nameof(conversion));

            var resultArray = new ResultT[sourceArray.Length];
            for (int i = 0; i < sourceArray.Length; i++)
                resultArray[i] = conversion(sourceArray[i]);
            return resultArray;
        }
    }
}