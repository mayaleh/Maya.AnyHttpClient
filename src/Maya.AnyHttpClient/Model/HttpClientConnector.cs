using System.Collections.Generic;
using System.Text.Json;

namespace Maya.AnyHttpClient.Model
{
    public class HttpClientConnector : IHttpClientConnector
    {
        public string Endpoint { get; set; }

        public string Token { get; set; }

        public string UserName { get; set; }
        
        public string Password { get; set; }
        
        public string AuthType { get; set; }
        
        public IEnumerable<KeyValue> Headers { get; set; }
        
        public IEnumerable<KeyValue> BodyProperties { get; set; }

        public double TimeoutSeconds { get; set; }

        public JsonSerializerOptions CustomJsonSerializerOptions { get; set; } = new JsonSerializerOptions();
    }
}
