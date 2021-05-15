using System;

namespace vtortola.WebSockets.Rfc6455.Header
{
    public static class EndianBitConverter
    {
        public static void UInt32CopyBytesLe(uint value, byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            for (var i = 0; i < 4; i++)
            {
                buffer[offset + i] = (byte)value;
                value >>= 8;
            }
        }

        public static void UInt64CopyBytesLe(ulong value, byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            for (var i = 0; i < 8; i++)
            {
                buffer[offset + i] = (byte)value;
                value >>= 8;
            }
        }

        public static void UInt16CopyBytesBe(ushort value, byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            for (var i = offset + 1; i >= offset; i--)
            {
                buffer[i] = (byte)value;
                value >>= 8;
            }
        }

        public static void UInt64CopyBytesBe(ulong value, byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            for (var i = offset + 7; i >= offset; i--)
            {
                buffer[i] = (byte)value;
                value >>= 8;
            }
        }

        public static ushort ToUInt16Be(byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            long value = 0;
            for (var i = 0; i < sizeof(ushort); i++)
            {
                value = unchecked((value << 8) | buffer[offset + i]);
            }
            return (ushort)value;
        }

        public static ulong ToUInt64Be(byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            long value = 0;
            for (var i = 0; i < sizeof(ulong); i++)
            {
                value = unchecked((value << 8) | buffer[offset + i]);
            }
            return (ulong)value;
        }

        public static uint ToUInt32Le(byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            long value = 0;
            for (var i = 0; i < sizeof(uint); i++)
            {
                value = unchecked((value << 8) | buffer[offset + sizeof(uint) - 1 - i]);
            }
            return (uint)value;
        }

        public static ulong ToUInt64Le(byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            long value = 0;
            for (var i = 0; i < sizeof(ulong); i++)
            {
                value = unchecked((value << 8) | buffer[offset + sizeof(ulong) - 1 - i]);
            }
            return (ulong)value;
        }
    }
}
