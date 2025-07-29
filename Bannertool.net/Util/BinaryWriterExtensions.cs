using System.IO;
using System.Text;

namespace Bannertool.Net.Util
{
    public static class BinaryWriterExtensions
    {
        // Writes a UTF-16 LE string and pads it to the specified byte length
        public static void WriteUnicodeString(this BinaryWriter writer, string value, int totalBytes)
        {
            var bytes = Encoding.Unicode.GetBytes(value);
            writer.Write(bytes);

            // Pad with null bytes
            int padding = totalBytes - bytes.Length;
            if (padding > 0)
            {
                writer.Write(new byte[padding]);
            }
        }

        // Writes a reference structure (magic, offset, size) common in 3DS formats
        public static void WriteReference(this BinaryWriter writer, uint magic, uint offset, uint size)
        {
            writer.Write(magic);
            writer.Write(offset);
            writer.Write(size);
        }
    }
}