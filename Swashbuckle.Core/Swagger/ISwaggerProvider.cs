using System;

namespace Swashbuckle.Swagger
{
    public interface ISwaggerProvider
    {
        SwaggerDocument GetSwagger(string rootUrl, string name);
    }

    public class UnknownDocumentException : Exception
    {
        public UnknownDocumentException(string name)
            : base($"Unknown document - {name}")
        {
		}
    }
}