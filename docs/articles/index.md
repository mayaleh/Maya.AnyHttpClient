# Any HttpClient

HttpClient Extending library for simplifying usage of HTTP methods (POST, GET, PUT...).

The simplification is mainly that HttpClient is configurable using the configuration class. This, at least in my opinion, simplifies the implementation of connecting to any HTTP API.

## Usage

For example, lets imagine that we have a fictitious API for app called `x-c`. This API has non-standard documentation and has the following specifications: 

**x-c API Authorization:**

Add application ID header `x-c-app_id: <your APPLICATION_ID>`.

Add to body property `"x_c_app_token": "<your APPLICATION_TOKE>"`.

**x-c API Available methods:**

Create FOO row:

FOO has properties:

| Name | Meaning    | Type         |
| ---- | ---------- | ------------ |
| id   | identifier | nullable int |
| name | name       | string       |

```sh
curl -L -X POST "https://x-c-app.com/api/foo" \
-H "Content-Type: application/json" \
-H "x-c-app_id: <your APPLICATION_ID>" \
-d "{
		"id": null,
		"name": "Gerald",
		"x_c_app_token": "<your APPLICATION_TOKE>"
}"
```

Response:

```json
{
    "isSuccess": true,
    "message": null
}
```

### Implementation of the example x-c API

First, lets sets the configs and create the http client:

```c#
var httpConfig = new Maya.AnyHttpClient.Model.HttpClientConnector
{
    BodyProperties = new List<Maya.AnyHttpClient.Model.KeyValue>
    {
        new AnyHttpClient.Model.KeyValue { Name = "x_c_app_token", Value = "<your APPLICATION_TOKE>" }
    },
    Headers = new List<Maya.AnyHttpClient.Model.KeyValue>
    {
        new AnyHttpClient.Model.KeyValue { Name = "x-c-app_id", Value = "<your APPLICATION_ID>"}
    },
    Endpoint = "https://x-c-app.com/api",
    TimeoutSeconds = 30,
};

var httpClient = new Maya.AnyHttpClient.BaseClient(httpConfig);
```

Lets create the response model class:

```c#
public class Result
{
    public bool isSuccess { get; set; }

    public string message { get; set; }
}
```


Now, we are able to call the POST `/foo` API method to create the row:

```c#
public async Task CreateFoo(string name)
{
    try
    {
        // this will build the endpoint uri, no needed to keeping attention to the slashes  
        var uri = Maya.AnyHttpClient.BaseApiService.ComposeUri(httpClient.HttpClientConnenctor.Endpoint, new List<string> { "foo" });

        // call the api method POST and get the result type
        var result = await this.HttpPost<Result>(uri, new() { name = name })
            .ConfigureAwait(false);
        
        if(result == null)
            throw new Exception($"The result of the called request is null...");

        if(result.isSuccess == false)
            throw new Exception(result.message);
    }
    catch (Exception)
    {
        throw;
    }
}
```

Now, the `CreateFoo` method is available to simplified usage.