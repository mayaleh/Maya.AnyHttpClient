using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maya.AnyHttpClient.Helpers;

namespace Maya.AnyHttpClient.Model
{
    public record UriRequest(IEnumerable<string>? Actions, params KeyValuePair<string, string>[]? QueryParameters)
    {
        //public IEnumerable<string>? Actions { get; init; }

        //public KeyValuePair<string, string>[]? QueryParameters { get; init; }

        public bool TryGetUri(string endpoint, out Uri? uri)
        {
            try
            {
                var actions = Actions ?? (new string[] { });
                var uriBuilder = new UriBuilder(HttpHelper.ComposeUrl(endpoint, actions));

                if (QueryParameters != null && QueryParameters.Any())
                {
                    var parameters = System.Web.HttpUtility.ParseQueryString(string.Empty);

                    QueryParameters.ToList()
                        .ForEach(p => parameters[p.Key] = p.Value);

                    uriBuilder.Query = parameters.ToString();
                }

                uri = uriBuilder.Uri;
                return true;
            }
            catch (Exception)
            {
                uri = null;
                return false;
            }
        }

        public override string ToString()
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(this);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
