using System.Net.Http.Headers;
using System.Text;
using Maya.AnyHttpClient.Model;
using Maya.Ext.Rop;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Maya.AnyHttpClient.Helpers
{
    internal static class HttpHelper
    {
        internal static string ComposeUrl(string endpoint, IEnumerable<string> actions)
        {
            if (!actions.Any()) return endpoint;

            var list = new List<string> { endpoint };

            list.AddRange(actions);

            var tmp = list.Select(x =>
            {
                if (x.StartsWith('/') || x.StartsWith('\\'))
                {
                    x = x.Substring(1);
                }

                if (x.EndsWith('/') || x.EndsWith('\\'))
                {
                    x = x[0..^1];
                }

                return x;
            });

            return String.Join("/", tmp);
        }

        internal static string AddConfiguredWrapper(HttpClient httpClient, IHttpClientConnector httpClientConnector, object data)
        {
            if (httpClientConnector == null)
            {
                throw new ArgumentNullException(nameof(httpClientConnector));
            }

            AddAuth(httpClient, httpClientConnector);
            AddHeaders(httpClient, httpClientConnector);

            return ExtendBody(httpClientConnector, data);
        }

        internal static void AddAuth(HttpClient httpClient, IHttpClientConnector httpClientConnector)
        {
            if (string.IsNullOrEmpty(httpClientConnector.AuthType))
            {
                return;
            }

            if (httpClientConnector.AuthType == AuthTypeKinds.Basic)
            {
                if (string.IsNullOrEmpty(httpClientConnector.UserName)) throw new ArgumentNullException(nameof(httpClientConnector.UserName));
                if (string.IsNullOrEmpty(httpClientConnector.Password)) throw new ArgumentNullException(nameof(httpClientConnector.Password));

                var token = CreateToken(httpClientConnector.UserName, httpClientConnector.Password);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
                return;
            }

            if (httpClientConnector.AuthType == AuthTypeKinds.Bearer)
            {
                if (string.IsNullOrEmpty(httpClientConnector.Token)) throw new ArgumentNullException(nameof(httpClientConnector.Token));

                var token = CreateToken(httpClientConnector.UserName, httpClientConnector.Password);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                return;
            }
        }

        internal static void AddHeaders(HttpClient httpClient, IHttpClientConnector httpClientConnector)
        {
            if (httpClientConnector.Headers != null && httpClientConnector.Headers.Any())
            {
                foreach (var header in httpClientConnector.Headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
                }
            }
        }

        internal static string ExtendBody(IHttpClientConnector httpClientConnector, object data)
        {
            if (httpClientConnector.BodyProperties != null && httpClientConnector.BodyProperties.Any())
            {
                data = data ?? new { };

                var json = JsonConvert.SerializeObject(data);
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                foreach (var prop in httpClientConnector.BodyProperties)
                {
                    dictionary.Add(prop.Name, prop.Value);
                }

                data = dictionary;
            }

            return (data == null) ? null : Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
        }

        internal static string CreateToken(string username, string password)
        {
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
        }

        internal static HttpClientHandler CreateHttpClientHanler()
        {
#if DEBUG
            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#else
            var httpClientHandler = new HttpClientHandler();
#endif
            return httpClientHandler;
        }

        internal static HttpClient CreateHttpClient(HttpClientHandler httpClientHandler, IHttpClientConnector httpClientConnector)
        {
            var httpClient = new HttpClient(httpClientHandler);

            httpClient.Timeout = TimeSpan.FromSeconds(httpClientConnector.TimeoutSeconds);

            return httpClient;
        }
    }
}
