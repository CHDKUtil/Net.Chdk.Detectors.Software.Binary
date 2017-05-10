using Net.Chdk.Model.Software;
using Net.Chdk.Providers.Software;
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

        private ISourceProvider SourceProvider { get; }

        protected InnerBinarySoftwareDetector(ISourceProvider sourceProvider)
        {
            SourceProvider = sourceProvider;

            bytes = new Lazy<IEnumerable<byte[]>>(GetBytes);
        }

        private readonly Lazy<IEnumerable<byte[]>> bytes;

        private IEnumerable<byte[]> GetBytes()
        {
            return Strings.Select(s => Encoding.ASCII.GetBytes(s));
        }

        public IEnumerable<byte[]> Bytes => bytes.Value;

        public virtual SoftwareInfo GetSoftware(byte[] buffer, int index)
        {
            var strings = GetStrings(buffer, index, StringCount);
            if (strings == null)
                return null;

            var product = GetProduct(strings);
            if (product == null)
                return null;

            var camera = GetCamera(strings);
            if (camera == null)
                return null;

            var source = GetSource(strings);
            if (source == null)
                return null;

            return new SoftwareInfo
            {
                Version = Version,
                Product = product,
                Camera = camera,
                Source = source,
            };
        }

        private SoftwareProductInfo GetProduct(string[] strings)
        {
            var version = GetProductVersion(strings);
            if (version == null)
                return null;

            var language = GetLanguage(strings);
            if (language == null)
                return null;

            var creationDate = GetCreationDate(strings);
            if (creationDate == null)
                return null;

            return new SoftwareProductInfo
            {
                Name = ProductName,
                Version = version,
                Language = language,
                Created = creationDate
            };
        }

        private SoftwareSourceInfo GetSource(string[] strings)
        {
            var sourceName = GetSourceName(strings);
            var channelName = GetSourceChannel(strings);
            return SourceProvider.GetSource(ProductName, sourceName, channelName);
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
                strings[i] = GetString(buffer, ref index);
            return strings;
        }

        protected static string GetString(byte[] buffer, ref int index)
        {
            if (index >= buffer.Length)
                return null;

            int count;
            for (count = 0; index + count < buffer.Length && buffer[index + count] != 0; count++) ;
            if (index + count == buffer.Length)
                return null;
            var str = Encoding.ASCII.GetString(buffer, index, count);
            index += count + 1;
            return str;
        }

        protected static Version GetVersion(string str)
        {
            if (str == null)
                return null;

            Version version;
            if (!Version.TryParse(str, out version))
                return null;

            return version;
        }

        protected static DateTime? GetCreationDate(string str)
        {
            if (str == null)
                return null;

            DateTime creationDate;
            if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out creationDate))
                return null;

            return creationDate;
        }

        protected static SoftwareCameraInfo GetCamera(string platform, string revision)
        {
            if (platform == null || revision == null)
                return null;

            return new SoftwareCameraInfo
            {
                Platform = platform,
                Revision = revision
            };
        }

        protected abstract string ProductName { get; }

        protected abstract string[] Strings { get; }

        protected abstract int StringCount { get; }

        protected abstract Version GetProductVersion(string[] strings);

        protected abstract CultureInfo GetLanguage(string[] strings);

        protected abstract DateTime? GetCreationDate(string[] strings);

        protected abstract SoftwareCameraInfo GetCamera(string[] strings);

        protected abstract string GetSourceName(string[] strings);

        protected abstract string GetSourceChannel(string[] strings);
    }
}
