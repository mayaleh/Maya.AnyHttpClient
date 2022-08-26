using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maya.AnyHttpClient.Model;
using Maya.Ext.Rop;
using Microsoft.Extensions.Logging;

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

            return string.Join("/", tmp);
        }

        internal static string? AddConfiguredWrapper(HttpClient httpClient, IHttpClientConnector httpClientConnector, object data)
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

            // validate
            if (string.IsNullOrEmpty(httpClientConnector.UserName) &&
                string.IsNullOrEmpty(httpClientConnector.Password) &&
                string.IsNullOrEmpty(httpClientConnector.Token))
            {
                throw new ArgumentNullException("Access token or credentials data is required! (Token, UserName, Password)");
            }

            if (httpClientConnector.AuthType == AuthTypeKinds.Basic)
            {
                if (string.IsNullOrEmpty(httpClientConnector.Token) == false) // prefer the token
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", httpClientConnector.Token);
                    return;
                }
                // or generate the token
                var token = CreateToken(httpClientConnector.UserName, httpClientConnector.Password);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
                return;
            }

            if (httpClientConnector.AuthType == AuthTypeKinds.Bearer)
            {
                if (string.IsNullOrEmpty(httpClientConnector.Token) == false)
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", httpClientConnector.Token);
                    return;
                }

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

        internal static string? ExtendBody(IHttpClientConnector httpClientConnector, object data)
        {
            if (httpClientConnector.BodyProperties != null && httpClientConnector.BodyProperties.Any())
            {
                data ??= new { };

                var json = JsonSerializer.Serialize(data, httpClientConnector.CustomJsonSerializerOptions);
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(json, httpClientConnector.CustomJsonSerializerOptions);

                foreach (var prop in httpClientConnector.BodyProperties)
                {
                    dictionary.Add(prop.Name, prop.Value);
                }

                data = dictionary;
            }

            return (data == null) ? null : JsonSerializer.Serialize(data, httpClientConnector.CustomJsonSerializerOptions);
        }

        internal static string CreateToken(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
        }

        internal static HttpClientHandler CreateHttpClientHanler()
        {
#if DEBUG
            var httpClientHandler = new HttpClientHandler
            {
                // Return `true` to allow certificates that are untrusted/invalid
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
#else
            var httpClientHandler = new HttpClientHandler();
#endif
            return httpClientHandler;
        }

        internal static HttpClient CreateHttpClient(HttpClientHandler httpClientHandler, IHttpClientConnector httpClientConnector)
        {
            var httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(httpClientConnector.TimeoutSeconds)
            };

            return httpClient;
        }
    }
}
