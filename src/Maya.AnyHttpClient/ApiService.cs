using System.Net.Http.Headers;
using System.Text;
using Maya.AnyHttpClient.Helpers;
using Maya.AnyHttpClient.Model;
using Maya.Ext.Rop;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace Maya.AnyHttpClient
{
    public class ApiService
    {
        protected IHttpClientConnector HttpClientConnenctor { get; }

        protected ILogger? Logger { get; set; }

        public ApiService(IHttpClientConnector httpClientConnenctor)
        {
            HttpClientConnenctor = httpClientConnenctor;
        }

        public ApiService WithLogger(ILogger logger)
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
        protected async Task<Result<T, Exception>> HttpGet<T>(UriRequest uriRequest, bool acceptJson = false)
        {
            try
            {
                if (uriRequest == null)
                {
                    return Result<T, Exception>.Failed(new ArgumentNullException(nameof(uriRequest)));
                }

                Uri? uri = null;

                var isValidUri = uriRequest.TryGetUri(HttpClientConnenctor.Endpoint, out uri);

                if (isValidUri == false)
                {
                    return Result<T, Exception>.Failed(new Exception("Invalid uriRequest"));
                }

                var httpClientHandler = HttpHelper.CreateHttpClientHanler();

                using (var client = HttpHelper.CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {

                    if (acceptJson)
                    {
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    }

                    HttpHelper.AddConfiguredWrapper(client, HttpClientConnenctor, null);

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

                        T reusultData = JsonSerializer.Deserialize<T>(content);

                        return Result<T, Exception>.Succeeded(reusultData);
                    }

                    //var apiException = await ProcessFailedHttpResponseMessageAsync(httpResponseMessage, uri.AbsoluteUri, null);

                    if (Logger != null)
                    {
                        Logger?.LogError($"action=ApiService.HttpGet, apiException={JsonSerializer.Serialize(httpResponseMessage)}");
                    }

                    return Result<T, Exception>.Failed(new Exception(await httpResponseMessage.Content.ReadAsStringAsync()));
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    Logger?.LogError(ex, $"action=ApiService.HttpGet({uriRequest}), message=Request was Canceled: {ex.Message}");

                    return Result<T, Exception>.Failed(ex);
                }

                Logger?.LogError(ex, $"action=ApiService.HttpGet, message=Request reached timeout: {ex.Message}");

                return Result<T, Exception>.Failed(ex);
            }
            catch (Exception e)
            {
                if (Logger != null)
                {
                    Logger?.LogError(e, $"action=ApiService.HttpGet({uriRequest}), message={e.Message}");
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
        protected async Task<Result<T, Exception>> HttpPost<T>(UriRequest uriRequest, object data, Func<HttpResponseMessage, Exception>? onError = null)
        {
            var logAction = $"{nameof(ApiService)}.{nameof(ApiService.HttpPost)}";
            var content = "";
            try
            {
                if (uriRequest == null)
                {
                    return Result<T, Exception>.Failed(new ArgumentNullException(nameof(uriRequest)));
                }

                Uri? uri = null;

                var isValidUri = uriRequest.TryGetUri(HttpClientConnenctor.Endpoint, out uri);

                if (isValidUri == false)
                {
                    return Result<T, Exception>.Failed(new Exception("Invalid uriRequest"));
                }

                var httpClientHandler = HttpHelper.CreateHttpClientHanler();

                using (var client = HttpHelper.CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {
                    var bodyContent = HttpHelper.AddConfiguredWrapper(client, HttpClientConnenctor, data);

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

                            T reusultData = JsonSerializer.Deserialize<T>(content);

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
                            Logger?.LogError($"action={logAction}, apiException={JsonSerializer.Serialize(httpResponseMessage)}");
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
        protected async Task<Result<T, Exception>> HttpPut<T>(UriRequest uriRequest, object data, Func<HttpResponseMessage, Exception>? onError = null)
        {
            try
            {
                if (uriRequest == null)
                {
                    return Result<T, Exception>.Failed(new ArgumentNullException(nameof(uriRequest)));
                }

                Uri? uri = null;

                var isValidUri = uriRequest.TryGetUri(HttpClientConnenctor.Endpoint, out uri);

                if (isValidUri == false)
                {
                    return Result<T, Exception>.Failed(new Exception("Invalid uriRequest"));
                }

                var httpClientHandler = HttpHelper.CreateHttpClientHanler();

                using (var client = HttpHelper.CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {
                    var bodyContent = HttpHelper.AddConfiguredWrapper(client, HttpClientConnenctor, data);

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

                            T reusultData = JsonSerializer.Deserialize<T>(content);

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
                            Logger?.LogError($"action=ApiService.HttpPut, apiException={JsonSerializer.Serialize(httpResponseMessage)}");
                        }

                        return Result<T, Exception>.Failed(new Exception(await httpResponseMessage.Content.ReadAsStringAsync()));
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    Logger?.LogError(ex, $"action=ApiService.HttpPut, message=Request was Canceled: {ex.Message}");

                    return Result<T, Exception>.Failed(ex);
                }

                Logger?.LogError(ex, $"action=ApiService.HttpPut, message=Request reached timeout: {ex.Message}");

                return Result<T, Exception>.Failed(ex);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"action=ApiService.HttpPut, message={e.Message}");

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
        protected async Task<Result<T, Exception>> HttpDelete<T>(UriRequest uriRequest, bool acceptJson = false)
        {
            var logAction = nameof(ApiService) + "." + nameof(ApiService.HttpDelete);
            try
            {
                if (uriRequest == null)
                {
                    return Result<T, Exception>.Failed(new ArgumentNullException(nameof(uriRequest)));
                }

                Uri? uri = null;

                var isValidUri = uriRequest.TryGetUri(HttpClientConnenctor.Endpoint, out uri);

                if (isValidUri == false)
                {
                    return Result<T, Exception>.Failed(new Exception("Invalid uriRequest"));
                }

                var httpClientHandler = HttpHelper.CreateHttpClientHanler();

                using (var client = HttpHelper.CreateHttpClient(httpClientHandler, HttpClientConnenctor))
                {

                    if (acceptJson)
                    {
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    }

                    HttpHelper.AddConfiguredWrapper(client, HttpClientConnenctor, null);

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

                        T reusultData = JsonSerializer.Deserialize<T>(content);

                        return Result<T, Exception>.Succeeded(reusultData);
                    }

                    //var apiException = await ProcessFailedHttpResponseMessageAsync(httpResponseMessage, uri.AbsoluteUri, null);

                    if (Logger != null)
                    {
                        Logger?.LogError($"action={logAction}, apiException={JsonSerializer.Serialize(httpResponseMessage)}");
                    }

                    return Result<T, Exception>.Failed(new Exception(await httpResponseMessage.Content.ReadAsStringAsync()));
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    Logger?.LogError(ex, $"action={logAction}({uriRequest}), message=Request was Canceled: {ex.Message}");

                    return Result<T, Exception>.Failed(ex);
                }

                Logger?.LogError(ex, $"action={logAction}, message=Request reached timeout: {ex.Message}");

                return Result<T, Exception>.Failed(ex);
            }
            catch (Exception e)
            {
                if (Logger != null)
                {
                    Logger?.LogError(e, $"action={logAction}({uriRequest}), message={e.Message}");
                }

                return Result<T, Exception>.Failed(e);
            }
        }
    }
}
