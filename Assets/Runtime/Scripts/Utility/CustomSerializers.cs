using Unity.Netcode;

namespace TinyTanks.Utility
{
    public static class CustomSerializers
    {
        public static void WriteValueSafe(this FastBufferWriter writer, ulong? value)
        {
            writer.WriteValueSafe(value.HasValue);
            writer.WriteValueSafe(value ?? 0);
        }
        
        public static void ReadValueSafe(this FastBufferReader reader, out ulong? result)
        {
            reader.ReadValueSafe(out bool hasValue);
            reader.ReadValueSafe(out ulong value);
            result = hasValue ? value : null;
        }
    }
}