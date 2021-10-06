using System.Collections.Generic;

namespace Maya.AnyHttpClient.Model
{
    public interface IHttpClientConnector
    {
        string Endpoint { get; set; }
        
        double TimeoutSeconds { get; set; }

        string Token { get; set; }
        
        string UserName { get; set; }
        
        string Password { get; set; }
        
        string AuthType { get; set; }

        IEnumerable<KeyValue> Headers { get; set; }

        IEnumerable<KeyValue> BodyProperties { get; set; }
    }
}