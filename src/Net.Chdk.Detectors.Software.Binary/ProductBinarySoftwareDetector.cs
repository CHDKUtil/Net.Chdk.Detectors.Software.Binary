using Net.Chdk.Model.Software;
using Net.Chdk.Providers.Software;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Net.Chdk.Detectors.Software.Binary
{
    public abstract class ProductBinarySoftwareDetector : IProductBinarySoftwareDetector
    {
        private static Version Version => new Version("1.0");

        private ISourceProvider SourceProvider { get; }

        protected ProductBinarySoftwareDetector(ISourceProvider sourceProvider)
        {
            SourceProvider = sourceProvider;

            bytes = new Lazy<byte[]>(GetBytes);
        }

        private readonly Lazy<byte[]> bytes;

        private byte[] GetBytes()
        {
            return Encoding.ASCII.GetBytes(String);
        }

        public byte[] Bytes => bytes.Value;

        public virtual SoftwareInfo GetSoftware(byte[] buffer, int index)
        {
            var strings = GetStrings(buffer, index, StringCount, SeparatorChar);
            if (strings == null)
                return null;

            var product = GetProduct(strings);
            if (product == null)
                return null;

            return new SoftwareInfo
            {
                Version = Version,
                Category = GetCategory(),
                Product = product,
                Camera = GetCamera(strings),
                Source = GetSource(strings, product),
                Build = GetBuild(strings),
                Compiler = GetCompiler(strings),
            };
        }

        private SoftwareCategoryInfo GetCategory()
        {
            return new SoftwareCategoryInfo
            {
                Name = CategoryName,
            };
        }

        private SoftwareProductInfo GetProduct(string[] strings)
        {
            Version version;
            string versionPrefix;
            if (!GetProductVersion(strings, out version, out versionPrefix))
                return null;

            return new SoftwareProductInfo
            {
                Name = ProductName,
                Version = version,
                VersionPrefix = versionPrefix,
                Language = GetLanguage(strings),
                Created = GetCreationDate(strings)
            };
        }

        private SoftwareSourceInfo GetSource(string[] strings, SoftwareProductInfo product)
        {
            var sourceName = GetSourceName(strings);
            var sources = SourceProvider.GetSources(product, sourceName);
            return sources.FirstOrDefault();
        }

        private static int SeekAfter(byte[] buffer, byte[] bytes)
        {
            for (var i = 0; i < buffer.Length - bytes.Length; i++)
                if (Enumerable.Range(0, bytes.Length).All(j => buffer[i + j] == bytes[j]))
                    return i + bytes.Length;
            return -1;
        }

        private static string[] GetStrings(byte[] buffer, int index, int length, char separator)
        {
            var strings = new string[length];
            for (var i = 0; i < length; i++)
                strings[i] = GetString(buffer, ref index, separator);
            return strings;
        }

        protected static string GetString(byte[] buffer, ref int index, char separator)
        {
            if (index >= buffer.Length)
                return null;

            int count;
            for (count = 0; index + count < buffer.Length && buffer[index + count] != separator; count++) ;
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

        public abstract string CategoryName { get; }

        public abstract string ProductName { get; }

        protected abstract string String { get; }

        protected abstract int StringCount { get; }

        protected virtual char SeparatorChar => '\0';

        protected virtual bool GetProductVersion(string[] strings, out Version version, out string versionPrefix)
        {
            version = GetProductVersion(strings);
            versionPrefix = null;
            return version != null;
        }

        protected abstract Version GetProductVersion(string[] strings);

        protected virtual CultureInfo GetLanguage(string[] strings) => null;

        protected virtual DateTime? GetCreationDate(string[] strings) => null;

        protected virtual SoftwareCameraInfo GetCamera(string[] strings)
        {
            var platform = GetPlatform(strings);
            var revision = GetRevision(strings);
            return GetCamera(platform, revision);
        }

        protected virtual SoftwareBuildInfo GetBuild(string[] strings) => null;

        protected virtual SoftwareCompilerInfo GetCompiler(string[] strings) => null;

        protected virtual string GetPlatform(string[] strings) => null;

        protected virtual string GetRevision(string[] strings) => null;

        protected virtual string GetSourceName(string[] strings) => ProductName;
    }
}
