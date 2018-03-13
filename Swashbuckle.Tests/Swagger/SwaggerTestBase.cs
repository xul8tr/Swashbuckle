using System;
using Swashbuckle.Application;

namespace Swashbuckle.Tests.Swagger
{
    public class SwaggerTestBase : HttpMessageHandlerTestBase<SwaggerDocsHandler>
    {
        protected SwaggerTestBase(string routeTemplate)
            : base(routeTemplate)
        {}

        protected void SetUpHandler(Action<SwaggerDocsConfig> configure = null)
        {
            var swaggerDocsConfig = new SwaggerDocsConfig();
			swaggerDocsConfig.SwaggerDoc("v1", (i) => i.Version("v1").Title("Test API"));
			configure?.Invoke(swaggerDocsConfig);

			Handler = new SwaggerDocsHandler(swaggerDocsConfig);
        }

		protected void SetUpHandlerWithoutDoc(Action<SwaggerDocsConfig> configure = null)
		{
			var swaggerDocsConfig = new SwaggerDocsConfig();
			configure?.Invoke(swaggerDocsConfig);

			Handler = new SwaggerDocsHandler(swaggerDocsConfig);
		}
	}
}
