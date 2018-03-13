using System;
using System.Web.Http;

namespace Swashbuckle.Dummy.Controllers
{
    public class MultipleApiVersionsController : ApiController
    {
        [Route("{documentName:regex(v1|v2)}/todos")]
        public int Create([FromBody]string description)
        {
            throw new NotImplementedException();
        }

        [HttpPut]
        [Route("{documentName:regex(v2)}/todos/{id}")]
        public void Update(int id, [FromBody]string description)
        {
            throw new NotImplementedException();
        }
    }
}