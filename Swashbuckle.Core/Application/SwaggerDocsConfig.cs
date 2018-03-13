using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.XPath;
using Newtonsoft.Json;
using Swashbuckle.Swagger;
using Swashbuckle.Swagger.Annotations;
using Swashbuckle.Swagger.FromUriParams;
using Swashbuckle.Swagger.XmlComments;

namespace Swashbuckle.Application
{
    public class SwaggerDocsConfig
    {
		private IDictionary<string, Info> _swaggerDocs;
		private Func<string, Info, ApiDescription, bool> _docInclusionPredicate;
        private IEnumerable<string> _schemes;
        private IDictionary<string, SecuritySchemeBuilder> _securitySchemeBuilders;
        private bool _prettyPrint;
        private bool _ignoreObsoleteActions;
        private Func<ApiDescription, string> _groupingKeySelector;
        private IComparer<string> _groupingKeyComparer;
        private readonly IDictionary<Type, Func<Schema>> _customSchemaMappings;
        private readonly IList<Func<ISchemaFilter>> _schemaFilters;
        private readonly IList<Func<IModelFilter>> _modelFilters;
        private Func<Type, string> _schemaIdSelector;
        private bool _ignoreObsoleteProperties;
        private bool _describeAllEnumsAsStrings;
        private bool _describeStringEnumsInCamelCase;
        private readonly IList<Func<IOperationFilter>> _operationFilters;
        private readonly IList<Func<IDocumentFilter>> _documentFilters;
        private readonly IList<Func<XPathDocument>> _xmlDocFactories;
        private Func<IEnumerable<ApiDescription>, ApiDescription> _conflictingActionsResolver;
        private Func<HttpRequestMessage, string> _rootUrlResolver;

        private Func<ISwaggerProvider, ISwaggerProvider> _customProviderFactory;

        public SwaggerDocsConfig()
        {
			_swaggerDocs = new Dictionary<string, Info>();
            _securitySchemeBuilders = new Dictionary<string, SecuritySchemeBuilder>();
            _prettyPrint = false;
            _ignoreObsoleteActions = false;
            _customSchemaMappings = new Dictionary<Type, Func<Schema>>();
            _schemaFilters = new List<Func<ISchemaFilter>>();
            _modelFilters = new List<Func<IModelFilter>>();
            _ignoreObsoleteProperties = false;
            _describeAllEnumsAsStrings = false;
            _describeStringEnumsInCamelCase = false;
            _operationFilters = new List<Func<IOperationFilter>>();
            _documentFilters = new List<Func<IDocumentFilter>>();
            _xmlDocFactories = new List<Func<XPathDocument>>();
            _rootUrlResolver = DefaultRootUrlResolver;

            SchemaFilter<ApplySwaggerSchemaFilterAttributes>();

            OperationFilter<HandleFromUriParams>();
            OperationFilter<ApplySwaggerOperationAttributes>();
            OperationFilter<ApplySwaggerResponseAttributes>();
            OperationFilter<ApplySwaggerOperationFilterAttributes>();
        }

		/// <summary>
		/// Define a document to be created by the Swagger generator
		/// </summary>
		/// <param name="version">Version for the document</param>
		/// <param name="title">Title for the document</param>
		/// <param name="configure">An optional method for configuring the Info instance for the document</param>
		public void SwaggerDoc(string version, string title, Action<InfoBuilder> configure = null)
		{
			var infoBuilder = new InfoBuilder()
				.Version(version)
				.Title(title);
			configure?.Invoke(infoBuilder);
			_swaggerDocs.Add(version, infoBuilder.Build());
		}

		/// <summary>
		/// Define a document to be created by the Swagger generator
		/// </summary>
		/// <param name="name">A URI-friendly name that uniquely identifies the document</param>
		/// <param name="info">Global metadata to be included in the Swagger output</param>
		public void SwaggerDoc(string name, Info info)
		{
			_swaggerDocs.Add(name, info);
		}

		/// <summary>
		/// Define a document to be created by the Swagger generator
		/// </summary>
		/// <param name="name">A URI-friendly name that uniquely identifies the document</param>
		/// <param name="configure">A method for configuring the Info instance for the document</param>
		public void SwaggerDoc(string name, Action<InfoBuilder> configure = null)
		{
			var infoBuilder = new InfoBuilder();
			configure?.Invoke(infoBuilder);
			_swaggerDocs.Add(name, infoBuilder.Build());
		}

		/// <summary>
		/// Provide a custom strategy for selecting actions.
		/// </summary>
		/// <param name="predicate">
		/// A lambda that returns true/false based on document name, Info, and ApiDescription
		/// </param>
		public void DocInclusionPredicate(Func<string, Info, ApiDescription, bool> predicate)
		{
			_docInclusionPredicate = predicate;
		}

		public void Schemes(IEnumerable<string> schemes)
        {
            _schemes = schemes;
        }

        public BasicAuthSchemeBuilder BasicAuth(string name)
        {
            var schemeBuilder = new BasicAuthSchemeBuilder();
            _securitySchemeBuilders[name] = schemeBuilder;
            return schemeBuilder;
        }

        public ApiKeySchemeBuilder ApiKey(string name)
        {
            var schemeBuilder = new ApiKeySchemeBuilder();
            _securitySchemeBuilders[name] = schemeBuilder;
            return schemeBuilder;
        }

        public OAuth2SchemeBuilder OAuth2(string name)
        {
            var schemeBuilder = new OAuth2SchemeBuilder();
            _securitySchemeBuilders[name] = schemeBuilder;
            return schemeBuilder;
        }

        public void PrettyPrint()
        {
            _prettyPrint = true;
        }

        public void IgnoreObsoleteActions()
        {
            _ignoreObsoleteActions = true;
        }

        public void GroupActionsBy(Func<ApiDescription, string> keySelector)
        {
            _groupingKeySelector = keySelector;
        }

        public void OrderActionGroupsBy(IComparer<string> keyComparer)
        {
            _groupingKeyComparer = keyComparer;
        }

        public void MapType<T>(Func<Schema> factory)
        {
            MapType(typeof(T), factory);
        }

        public void MapType(Type type, Func<Schema> factory)
        {
            _customSchemaMappings.Add(type, factory);
        }

		public void SchemaFilter<TFilter>()
            where TFilter : ISchemaFilter, new()
        {
            SchemaFilter(() => new TFilter());
        }

        public void SchemaFilter(Func<ISchemaFilter> factory)
        {
            _schemaFilters.Add(factory);
        }

        // NOTE: In next major version, ModelFilter will completely replace SchemaFilter
        internal void ModelFilter<TFilter>()
            where TFilter : IModelFilter, new()
        {
            ModelFilter(() => new TFilter());
        }

        // NOTE: In next major version, ModelFilter will completely replace SchemaFilter
        internal void ModelFilter(Func<IModelFilter> factory)
        {
            _modelFilters.Add(factory);
        }

        public void SchemaId(Func<Type, string> schemaIdStrategy)
        {
            _schemaIdSelector = schemaIdStrategy;
        }

        public void UseFullTypeNameInSchemaIds()
        {
            _schemaIdSelector = t => t.FriendlyId(true);
        }

        public void DescribeAllEnumsAsStrings(bool camelCase = false)
        {
            _describeAllEnumsAsStrings = true;
            _describeStringEnumsInCamelCase = camelCase;
        }

        public void IgnoreObsoleteProperties()
        {
            _ignoreObsoleteProperties = true;
        }

        public void OperationFilter<TFilter>()
            where TFilter : IOperationFilter, new()
        {
            OperationFilter(() => new TFilter());
        }

        public void OperationFilter(Func<IOperationFilter> factory)
        {
            _operationFilters.Add(factory);
        }

        public void DocumentFilter<TFilter>()
            where TFilter : IDocumentFilter, new()
        {
            DocumentFilter(() => new TFilter());
        }

        public void DocumentFilter(Func<IDocumentFilter> factory)
        {
            _documentFilters.Add(factory);
        }

        public void IncludeXmlComments(Func<XPathDocument> xmlDocFactory)
        {
            _xmlDocFactories.Add(xmlDocFactory);
        }

        public void IncludeXmlComments(string filePath)
        {
            _xmlDocFactories.Add(() => new XPathDocument(filePath));
        }

        public void ResolveConflictingActions(Func<IEnumerable<ApiDescription>, ApiDescription> conflictingActionsResolver)
        {
            _conflictingActionsResolver = conflictingActionsResolver;
        }

        public void RootUrl(Func<HttpRequestMessage, string> rootUrlResolver)
        {
            _rootUrlResolver = rootUrlResolver;
        }

        public void CustomProvider(Func<ISwaggerProvider, ISwaggerProvider> customProviderFactory)
        {
            _customProviderFactory = customProviderFactory;
        }

        internal ISwaggerProvider GetSwaggerProvider(HttpRequestMessage swaggerRequest)
        {
            var httpConfig = swaggerRequest.GetConfiguration();

            var securityDefintitions = _securitySchemeBuilders.Any()
                ? _securitySchemeBuilders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build())
                : null;

            // NOTE: Instantiate & add the XML comments filters here so they're executed before any
            // custom filters AND so they can share the same XPathDocument (perf. optimization)
            var modelFilters = _modelFilters.Select(factory => factory()).ToList();
            var operationFilters = _operationFilters.Select(factory => factory()).ToList();
            foreach (var xmlDocFactory in _xmlDocFactories)
            {
                var xmlDoc = xmlDocFactory();
                modelFilters.Insert(0, new ApplyXmlTypeComments(xmlDoc));
                operationFilters.Insert(0, new ApplyXmlActionComments(xmlDoc));
            }

            var options = new SwaggerGeneratorOptions(
                docInclusionPredicate: _docInclusionPredicate,
                schemes: _schemes,
                securityDefinitions: securityDefintitions,
                ignoreObsoleteActions: _ignoreObsoleteActions,
                groupingKeySelector: _groupingKeySelector,
                groupingKeyComparer: _groupingKeyComparer,
                customSchemaMappings: _customSchemaMappings,
                schemaFilters: _schemaFilters.Select(factory => factory()).ToList(),
                modelFilters: modelFilters,
                ignoreObsoleteProperties: _ignoreObsoleteProperties,
                schemaIdSelector: _schemaIdSelector,
                describeAllEnumsAsStrings: _describeAllEnumsAsStrings,
                describeStringEnumsInCamelCase: _describeStringEnumsInCamelCase,
                operationFilters: operationFilters,
                documentFilters: _documentFilters.Select(factory => factory()).ToList(),
                conflictingActionsResolver: _conflictingActionsResolver
            );

            var defaultProvider = new SwaggerGenerator(
                httpConfig.Services.GetApiExplorer(),
                httpConfig.SerializerSettingsOrDefault(),
				_swaggerDocs,
                options);

            return (_customProviderFactory != null)
                ? _customProviderFactory(defaultProvider)
                : defaultProvider;
        }

		internal string GetRootUrl(HttpRequestMessage swaggerRequest) => _rootUrlResolver(swaggerRequest);

		internal IDictionary<string, Info> GetSwaggerDocs() => _swaggerDocs;

        internal Formatting GetFormatting() => _prettyPrint ? Formatting.Indented : Formatting.None;

        public static string DefaultRootUrlResolver(HttpRequestMessage request)
        {
            var scheme = GetHeaderValue(request, "X-Forwarded-Proto") ?? request.RequestUri.Scheme;
            var host = GetHeaderValue(request, "X-Forwarded-Host") ?? request.RequestUri.Host;
            var port = GetHeaderValue(request, "X-Forwarded-Port") ?? request.RequestUri.Port.ToString(CultureInfo.InvariantCulture);
            var prefix = GetHeaderValue(request, "X-Forwarded-Prefix") ?? string.Empty;

            var httpConfiguration = request.GetConfiguration();
            var virtualPathRoot = httpConfiguration.VirtualPathRoot;

            var urb = new UriBuilder(scheme, host, int.Parse(port), prefix + virtualPathRoot);

            return urb.Uri.AbsoluteUri.TrimEnd('/');
        }

        private static string GetHeaderValue(HttpRequestMessage request, string headerName) => request.Headers.TryGetValues(headerName, out IEnumerable<string> list) ? list.FirstOrDefault() : null;
    }
}