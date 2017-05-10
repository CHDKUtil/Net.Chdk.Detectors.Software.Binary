using Net.Chdk.Model.Software;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Net.Chdk.Detectors.Software.Binary
{
    public abstract class InnerBinarySoftwareDetector : IInnerBinarySoftwareDetector
    {
        private static Version Version => new Version("1.0");

        protected InnerBinarySoftwareDetector()
        {
            bytes = new Lazy<IEnumerable<byte[]>>(GetBytes);
        }

        private readonly Lazy<IEnumerable<byte[]>> bytes;

        private IEnumerable<byte[]> GetBytes()
        {
            return Strings.Select(s => Encoding.ASCII.GetBytes(s));
        }

        public IEnumerable<byte[]> Bytes => bytes.Value;

        public SoftwareInfo GetSoftware(byte[] buffer, int index)
        {
            var strings = GetStrings(buffer, index, StringCount);
            return new SoftwareInfo
            {
                Version = Version,
                Product = GetProduct(strings),
                Camera = GetCamera(strings),
            };
        }

        private SoftwareProductInfo GetProduct(string[] strings)
        {
            return new SoftwareProductInfo
            {
                Name = Name,
                Version = GetVersion(strings),
                Language = GetLanguage(strings),
                Created = GetCreationDate(strings)
            };
        }

        private static int SeekAfter(byte[] buffer, byte[] bytes)
        {
            for (var i = 0; i < buffer.Length - bytes.Length; i++)
                if (Enumerable.Range(0, bytes.Length).All(j => buffer[i + j] == bytes[j]))
                    return i + bytes.Length;
            return -1;
        }

        private static string[] GetStrings(byte[] buffer, int index, int length)
        {
            var strings = new string[length];
            for (var i = 0; i < length; i++)
            {
                int count;
                for (count = 0; buffer[index + count] != 0; count++) ;
                strings[i] = Encoding.ASCII.GetString(buffer, index, count);
                index += count + 1;
            }
            return strings;
        }

        protected static DateTime? GetCreationDate(string str)
        {
            return DateTime.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }

        protected abstract string Name { get; }

        protected abstract string[] Strings { get; }

        protected abstract int StringCount { get; }

        protected abstract Version GetVersion(string[] strings);

        protected abstract CultureInfo GetLanguage(string[] strings);

        protected abstract DateTime? GetCreationDate(string[] strings);

        protected abstract SoftwareCameraInfo GetCamera(string[] strings);
    }
}
