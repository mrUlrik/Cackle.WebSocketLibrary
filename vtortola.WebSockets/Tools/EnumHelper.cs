/*
	Copyright (c) 2017 Denis Zykov
	License: https://opensource.org/licenses/MIT
*/

using System.Linq.Expressions;
using System.Reflection;

namespace vtortola.WebSockets.Tools
{
    internal static class EnumHelper
    {
        public static readonly bool PlatformSupportEnumInterchange;

        static EnumHelper()
        {
            PlatformSupportEnumInterchange = false;
        }

        public static byte FromOrToUInt8(byte value) => value;
        public static sbyte FromOrToInt8(sbyte value) => value;
        public static short FromOrToInt16(short value) => value;
        public static int FromOrToInt32(int value) => value;
        public static long FromOrToInt64(long value) => value;
        public static ushort FromOrToUInt16(ushort value) => value;
        public static uint FromOrToUInt32(uint value) => value;
        public static ulong FromOrToUInt64(ulong value) => value;
    }

    public static class EnumHelper<EnumT>
    {
        private static readonly SortedDictionary<EnumT, string> NamesByNumber;

        public static readonly Delegate ToNumber;
        public static readonly Delegate FromNumber;
        private static readonly Comparer<EnumT> Comparer;

        static EnumHelper()
        {
            var enumType = typeof(EnumT);
            if (enumType.GetTypeInfo().IsEnum == false)
                throw new InvalidOperationException("TKnownHeader should be enum type.");

            var underlyingType = Enum.GetUnderlyingType(enumType);

            if (EnumHelper.PlatformSupportEnumInterchange)
            {
            }
            else if (ReflectionHelper.IsDynamicCompilationSupported)
            {
                var valueParameter = Expression.Parameter(underlyingType, "value");
                var enumParameter = Expression.Parameter(enumType, "value");
                var xParameter = Expression.Parameter(enumType, "value");
                var yParameter = Expression.Parameter(enumType, "value");

                FromNumber = Expression.Lambda(Expression.ConvertChecked(valueParameter, enumType), valueParameter).Compile();
                ToNumber = Expression.Lambda(Expression.ConvertChecked(enumParameter, underlyingType), enumParameter).Compile();
                Comparer = Comparer<EnumT>.Create(Expression.Lambda<Comparison<EnumT>>(
                    Expression.Call
                    (
                        Expression.ConvertChecked(xParameter, underlyingType),
                        nameof(int.CompareTo),
                        Type.EmptyTypes,
                        Expression.ConvertChecked(yParameter, underlyingType)
                    ),
                    xParameter,
                    yParameter
                ).Compile());
            }
            else
            {
                switch (ReflectionHelper.GetTypeCode(underlyingType))
                {
                    case TypeCode.SByte:
                        ToNumber = new Func<EnumT, sbyte>(v => Convert.ToSByte(v));
                        FromNumber = new Func<sbyte, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, sbyte>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, sbyte>)ToNumber).Invoke(y)));
                        break;
                    case TypeCode.Byte:
                        ToNumber = new Func<EnumT, byte>(v => Convert.ToByte(v));
                        FromNumber = new Func<byte, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, byte>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, byte>)ToNumber).Invoke(y)));
                        break;
                    case TypeCode.Int16:
                        ToNumber = new Func<EnumT, short>(v => Convert.ToInt16(v));
                        FromNumber = new Func<short, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, short>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, short>)ToNumber).Invoke(y)));
                        break;
                    case TypeCode.UInt16:
                        ToNumber = new Func<EnumT, ushort>(v => Convert.ToUInt16(v));
                        FromNumber = new Func<ushort, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, ushort>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, ushort>)ToNumber).Invoke(y)));
                        break;
                    case TypeCode.Int32:
                        ToNumber = new Func<EnumT, int>(v => Convert.ToInt32(v));
                        FromNumber = new Func<int, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, int>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, int>)ToNumber).Invoke(y)));
                        break;
                    case TypeCode.UInt32:
                        ToNumber = new Func<EnumT, uint>(v => Convert.ToUInt32(v));
                        FromNumber = new Func<uint, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, uint>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, uint>)ToNumber).Invoke(y)));
                        break;
                    case TypeCode.Int64:
                        ToNumber = new Func<EnumT, long>(v => Convert.ToInt64(v));
                        FromNumber = new Func<long, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, long>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, long>)ToNumber).Invoke(y)));
                        break;
                    case TypeCode.UInt64:
                        ToNumber = new Func<EnumT, ulong>(v => Convert.ToUInt64(v));
                        FromNumber = new Func<ulong, EnumT>(v => (EnumT)Enum.ToObject(typeof(EnumT), v));
                        Comparer = Comparer<EnumT>.Create((x, y) => ((Func<EnumT, ulong>)ToNumber).Invoke(x).CompareTo(((Func<EnumT, ulong>)ToNumber).Invoke(y)));
                        break;
                    default: throw new ArgumentOutOfRangeException($"Unexpected underlying type '{underlyingType}' of enum '{enumType}'.");
                }
            }

            NamesByNumber = new SortedDictionary<EnumT, string>(Comparer);
            foreach (EnumT value in Enum.GetValues(typeof(EnumT)))
                NamesByNumber[value] = value.ToString();
        }

        public static string ToName(EnumT value)
        {
            var name = default(string);
            if (NamesByNumber.TryGetValue(value, out name))
                return name;
            return value.ToString();
        }

        public static bool IsDefined(EnumT value)
        {
            return NamesByNumber.ContainsKey(value);
        }
    }
}
