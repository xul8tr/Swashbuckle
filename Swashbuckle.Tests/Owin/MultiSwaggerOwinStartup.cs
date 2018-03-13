using System;
using System.Web.Http;
using Swashbuckle.Application;
using Swashbuckle.Swagger;

namespace Swashbuckle.Tests.Owin
{
    public class MultiSwaggerOwinStartup : OwinStartup
    {
        public MultiSwaggerOwinStartup(params Type[] supportedControllers) : base(supportedControllers)
        {
        }

        protected override void EnableSwagger(HttpConfiguration config)
        {
            base.EnableSwagger(config);

            // configure swagger as well on separate URL
            config
                .EnableSwagger("docs/{documentName}/.metadata", c => c.SwaggerDoc("v1", new Info { version = "v1", title = "A title for your API" }))
                .EnableSwaggerUi("docs-ui/{*assetPath}");
        }
    }
}