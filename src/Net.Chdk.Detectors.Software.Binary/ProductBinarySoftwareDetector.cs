using Net.Chdk.Model.Category;
using Net.Chdk.Model.Software;
using Net.Chdk.Providers.Software;
using System;
using System.Globalization;
using System.Linq;

namespace Net.Chdk.Detectors.Software.Product
{
    public abstract class ProductBinarySoftwareDetector : ProductBinaryDetector, IProductBinarySoftwareDetector
    {
        private static Version Version => new Version("1.0");

        private ISourceProvider SourceProvider { get; }

        protected ProductBinarySoftwareDetector(ISourceProvider sourceProvider)
        {
            SourceProvider = sourceProvider;
        }

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

        private CategoryInfo GetCategory()
        {
            return new CategoryInfo
            {
                Name = CategoryName,
            };
        }

        private SoftwareProductInfo GetProduct(string[] strings)
        {
            Version version;
            string versionPrefix;
            string versionSuffix;
            if (!GetProductVersion(strings, out version, out versionPrefix, out versionSuffix))
                return null;

            return new SoftwareProductInfo
            {
                Name = ProductName,
                Version = version,
                VersionPrefix = versionPrefix,
                VersionSuffix = versionSuffix,
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

        protected virtual bool GetProductVersion(string[] strings, out Version version, out string versionPrefix, out string versionSuffix)
        {
            version = GetProductVersion(strings);
            versionPrefix = null;
            versionSuffix = null;
            return version != null;
        }

        protected virtual Version GetProductVersion(string[] strings) => null;

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
