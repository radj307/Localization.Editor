using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Localization.Editor.TestConfigs
{
    internal static class TestConfigHelper
    {
        public static readonly string[] ResourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        public static string GetResourceName(string fileName, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
            => ResourceNames.First(name => name.EndsWith(fileName, stringComparison));
        public static string GetResourceString(string resourceName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) throw new InvalidOperationException($"Resource \"{resourceName}\" was not found.");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
