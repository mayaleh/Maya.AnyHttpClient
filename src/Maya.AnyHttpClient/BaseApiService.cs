using System.Net.Http.Headers;
using System.Text;
using Maya.AnyHttpClient.Model;
using Maya.Ext.Rop;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Maya.AnyHttpClient
{
    public class BaseApiService
    {
        protected IHttpClientConnector HttpClientConnenctor { get; }

        protected ILogger Logger { get; set; }


        public BaseApiService(IHttpClientConnector httpClientConnenctor)
        {
            HttpClientConnenctor = httpClientConnenctor;
        }

        public BaseApiService WithLogger(ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Logger = logger;

            return this;
        }

        /// <summary>
        /// HTTP GET Request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="acceptJson"></param>
        /// <returns></returns>
        protected async Task<Result<T, Exception>> HttpGet<T>(Uri uri, bool acceptJson = false)
        {
            try
            {
                var httpClientHandler = CreateHttpClientHanler();

                using (var client = CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {

                    if (acceptJson)
                    {
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    }

                    AddConfiguredWrapper(client, HttpClientConnenctor, null);

                    if (typeof(T) == typeof(byte[]))
                    {
                        var result = await client.GetByteArrayAsync(uri);
                        return Result<T, Exception>.Succeeded((T)Convert.ChangeType(result, typeof(T)));
                    }

                    var httpResponseMessage = await client.GetAsync(uri);

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var content = await httpResponseMessage.Content.ReadAsStringAsync();

                        if (typeof(T) == typeof(string))
                        {
                            return Result<T, Exception>.Succeeded((T)Convert.ChangeType(content, typeof(T)));
                        }

                        T reusultData = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);

                        return Result<T, Exception>.Succeeded(reusultData);
                    }

                    //var apiException = await ProcessFailedHttpResponseMessageAsync(httpResponseMessage, uri.AbsoluteUri, null);

                    if (Logger != null)
                    {
                        Logger?.LogError($"action=BaseApiService.HttpGet, apiException={Newtonsoft.Json.JsonConvert.SerializeObject(httpResponseMessage)}");
                    }

                    return Result<T, Exception>.Failed(new Exception(await httpResponseMessage.Content.ReadAsStringAsync()));
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    Logger?.LogError(ex, $"action=BaseApiService.HttpGet({uri}), message=Request was Canceled: {ex.Message}");

                    return Result<T, Exception>.Failed(ex);
                }

                Logger?.LogError(ex, $"action=BaseApiService.HttpGet, message=Request reached timeout: {ex.Message}");

                return Result<T, Exception>.Failed(ex);
            }
            catch (Exception e)
            {
                if (Logger != null)
                {
                    Logger?.LogError(e, $"action=BaseApiService.HttpGet({uri}), message={e.Message}");
                }

                return Result<T, Exception>.Failed(e);
            }
        }

        /// <summary>
        /// HTTP POST Request, json format.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected async Task<Result<T, Exception>> HttpPost<T>(Uri uri, object data, Func<HttpResponseMessage, Exception>? onError = null)
        {
            var logAction = $"{nameof(BaseApiService)}.{nameof(BaseApiService.HttpPost)}";
            var content = "";
            try
            {
                var httpClientHandler = CreateHttpClientHanler();

                using (var client = CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {
                    var bodyContent = AddConfiguredWrapper(client, HttpClientConnenctor, data);

                    using (var message = new HttpRequestMessage(HttpMethod.Post, uri)
                    {
                        Content = new System.Net.Http.StringContent(
                            bodyContent,
                            Encoding.UTF8,
                            "application/json")
                    })
                    {
                        var httpResponseMessage = await client.SendAsync(message);

                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            if (typeof(T) == typeof(Maya.Ext.Unit)) // void is not value type, this is for response, that has not any body response
                            {
                                return Result<T, Exception>.Succeeded((T)Convert.ChangeType(Maya.Ext.Unit.Default, typeof(T)));
                            }

                            if (typeof(T) == typeof(byte[]))
                            {
                                var result = await httpResponseMessage.Content.ReadAsByteArrayAsync();
                                return Result<T, Exception>.Succeeded((T)Convert.ChangeType(result, typeof(T)));
                            }

                            content = await httpResponseMessage.Content.ReadAsStringAsync();

                            if (typeof(T) == typeof(string))
                            {
                                return Result<T, Exception>.Succeeded((T)Convert.ChangeType(content, typeof(T)));
                            }

                            T reusultData = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);

                            return Result<T, Exception>.Succeeded(reusultData);
                        }

                        if (onError != null)
                        {
                            var ownException = onError.Invoke(httpResponseMessage);

                            return Result<T, Exception>.Failed(ownException);
                        }

                        //var apiException = await ProcessFailedHttpResponseMessageAsync(httpResponseMessage, uri.AbsoluteUri, null);

                        if (Logger != null)
                        {
                            Logger?.LogError($"action={logAction}, apiException={Newtonsoft.Json.JsonConvert.SerializeObject(httpResponseMessage)}");
                        }

                        return Result<T, Exception>.Failed(new Exception(await httpResponseMessage.Content.ReadAsStringAsync()));
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    Logger?.LogError(ex, $"action={logAction}, message=Request was Canceled: {ex.Message}");

                    return Result<T, Exception>.Failed(ex);
                }

                Logger?.LogError(ex, $"action={logAction}, message=Request reached timeout: {ex.Message}");

                return Result<T, Exception>.Failed(ex);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"action={logAction}, message={e.Message}, content={content}");

                return Result<T, Exception>.Failed(e);
            }
        }

        /// <summary>
        /// HTTP PUT Request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        protected async Task<Result<T, Exception>> HttpPut<T>(Uri uri, object data, Func<HttpResponseMessage, Exception> onError = null)
        {
            try
            {
                var httpClientHandler = CreateHttpClientHanler();

                using (var client = CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {
                    var bodyContent = AddConfiguredWrapper(client, HttpClientConnenctor, data);

                    using (var message = new HttpRequestMessage(HttpMethod.Put, uri)
                    {
                        Content = new System.Net.Http.StringContent(
                            bodyContent,
                            Encoding.UTF8,
                            "application/json")
                    })
                    {
                        var httpResponseMessage = await client.SendAsync(message);

                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            if (typeof(T) == typeof(Maya.Ext.Unit)) // void is not value type, this is for response, that has not any body response
                            {
                                return Result<T, Exception>.Succeeded((T)Convert.ChangeType(Maya.Ext.Unit.Default, typeof(T)));
                            }

                            var content = await httpResponseMessage.Content.ReadAsStringAsync();

                            if (typeof(T) == typeof(String))
                            {
                                return Result<T, Exception>.Succeeded((T)Convert.ChangeType(content, typeof(T)));
                            }

                            T reusultData = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);

                            return Result<T, Exception>.Succeeded(reusultData);
                        }

                        if (onError != null)
                        {
                            var ownException = onError.Invoke(httpResponseMessage);

                            return Result<T, Exception>.Failed(ownException);
                        }

                        //var apiException = await ProcessFailedHttpResponseMessageAsync(httpResponseMessage, uri.AbsoluteUri, null);

                        if (Logger != null)
                        {
                            Logger?.LogError($"action=BaseApiService.HttpPut, apiException={Newtonsoft.Json.JsonConvert.SerializeObject(httpResponseMessage)}");
                        }

                        return Result<T, Exception>.Failed(new Exception(await httpResponseMessage.Content.ReadAsStringAsync()));
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    Logger?.LogError(ex, $"action=BaseApiService.HttpPut, message=Request was Canceled: {ex.Message}");

                    return Result<T, Exception>.Failed(ex);
                }

                Logger?.LogError(ex, $"action=BaseApiService.HttpPut, message=Request reached timeout: {ex.Message}");

                return Result<T, Exception>.Failed(ex);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"action=BaseApiService.HttpPut, message={e.Message}");

                return Result<T, Exception>.Failed(e);
            }
        }

        /// <summary>
        /// HTTP Delete Request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="acceptJson"></param>
        /// <returns></returns>
        protected async Task<Result<T, Exception>> HttpDelete<T>(Uri uri, bool acceptJson = false)
        {
            var logAction = nameof(BaseApiService) + "." + nameof(BaseApiService.HttpDelete);
            try
            {
                var httpClientHandler = CreateHttpClientHanler();

                using (var client = CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {

                    if (acceptJson)
                    {
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    }

                    AddConfiguredWrapper(client, HttpClientConnenctor, null);

                    var httpResponseMessage = await client.DeleteAsync(uri);

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        if (typeof(T) == typeof(Maya.Ext.Unit)) // void is not value type, this is for response, that has not any body response
                        {
                            return Result<T, Exception>.Succeeded((T)Convert.ChangeType(Maya.Ext.Unit.Default, typeof(T)));
                        }

                        var content = await httpResponseMessage.Content.ReadAsStringAsync();

                        if (typeof(T) == typeof(string))
                        {
                            return Result<T, Exception>.Succeeded((T)Convert.ChangeType(content, typeof(T)));
                        }

                        T reusultData = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);

                        return Result<T, Exception>.Succeeded(reusultData);
                    }

                    //var apiException = await ProcessFailedHttpResponseMessageAsync(httpResponseMessage, uri.AbsoluteUri, null);

                    if (Logger != null)
                    {
                        Logger?.LogError($"action={logAction}, apiException={Newtonsoft.Json.JsonConvert.SerializeObject(httpResponseMessage)}");
                    }

                    return Result<T, Exception>.Failed(new Exception(await httpResponseMessage.Content.ReadAsStringAsync()));
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    Logger?.LogError(ex, $"action={logAction}({uri}), message=Request was Canceled: {ex.Message}");

                    return Result<T, Exception>.Failed(ex);
                }

                Logger?.LogError(ex, $"action={logAction}, message=Request reached timeout: {ex.Message}");

                return Result<T, Exception>.Failed(ex);
            }
            catch (Exception e)
            {
                if (Logger != null)
                {
                    Logger?.LogError(e, $"action={logAction}({uri}), message={e.Message}");
                }

                return Result<T, Exception>.Failed(e);
            }
        }

        public static Uri ComposeUri(string endpoint, IEnumerable<string> actions, params KeyValuePair<string, string>[] queryParameters)
        {
            var uriBuilder = new UriBuilder(ComposeUrl(endpoint, actions));

            if (queryParameters.Any())
            {
                var parameters = System.Web.HttpUtility.ParseQueryString(string.Empty);

                queryParameters.ToList()
                    .ForEach(p => parameters[p.Key] = p.Value);

                uriBuilder.Query = parameters.ToString();
            }

            return uriBuilder.Uri;
        }

        private static string ComposeUrl(string endpoint, IEnumerable<string> actions)
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

        private static string AddConfiguredWrapper(HttpClient httpClient, IHttpClientConnector httpClientConnector, object data)
        {
            if (httpClientConnector == null)
            {
                throw new ArgumentNullException(nameof(httpClientConnector));
            }

            AddAuth(httpClient, httpClientConnector);
            AddHeaders(httpClient, httpClientConnector);

            return ExtendBody(httpClientConnector, data);
        }

        private static void AddAuth(HttpClient httpClient, IHttpClientConnector httpClientConnector)
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

        private static void AddHeaders(HttpClient httpClient, IHttpClientConnector httpClientConnector)
        {
            if (httpClientConnector.Headers != null && httpClientConnector.Headers.Any())
            {
                foreach (var header in httpClientConnector.Headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Name, header.Value);
                }
            }
        }

        private static string ExtendBody(IHttpClientConnector httpClientConnector, object data)
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

        private static string CreateToken(string username, string password)
        {
            if (String.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
        }

        private static HttpClientHandler CreateHttpClientHanler()
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

        private static HttpClient CreateHttpClient(HttpClientHandler httpClientHandler, IHttpClientConnector httpClientConnector)
        {
            var httpClient = new HttpClient(httpClientHandler);

            httpClient.Timeout = TimeSpan.FromSeconds(httpClientConnector.TimeoutSeconds);

            return httpClient;
        }
    }
}
