using System.Collections.Immutable;

namespace org.g14.TypedConfig.Generator;

public partial class TypedConfigTypesGenerator
{
    /// <summary>
    /// Represents current location and context data when generating code.
    /// </summary>
    private record struct LocationContext
    {
        public string Namespace { get; private set; }

        public bool IsExcludedFromBoolConventionCheck
        {
            get
            {
                var @this = this;
                return _excludedSections.Any(x => @this._configSectionPath.StartsWith(x));
            }
        }

        private readonly ImmutableHashSet<string> _excludedSections;

        private string _configSectionPath;

        // constructor intentionally private
        private LocationContext(string @namespace, string configSectionPath, ImmutableHashSet<string> excludedSections)
        {
            Namespace = @namespace;
            _configSectionPath = configSectionPath;
            _excludedSections = excludedSections;
        }

        /// <summary>
        /// Create a nested location of the object/namespace/section of config
        /// </summary>
        /// <param name="childName">name of the nested property/section/...</param>
        public LocationContext Child(string childName)
        {
            return this with
            {
                Namespace = $"{Namespace}.{childName}",
                _configSectionPath = _configSectionPath.Length == 0 ? childName : $"{_configSectionPath}:{childName}",
            };
        }

        /// <summary>
        /// Create a new instance for the root of appsettings
        /// </summary>
        public static LocationContext Init(string namespaceBase, ImmutableHashSet<string> excludedSections)
        {
            return new(namespaceBase, string.Empty, excludedSections);
        }
    }
}