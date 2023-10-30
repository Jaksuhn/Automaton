using System;

namespace Automaton.Helpers
{
    internal static class AddressHelper
    {
        public static unsafe T ReadField<T>(void* address, int offset) where T : unmanaged
        {
            return *(T*)((IntPtr)address + offset);
        }

        public static unsafe void WriteField<T>(void* address, int offset, T value) where T : unmanaged
        {
            *(T*)((IntPtr)address + offset) = value;
        }
    }
}
