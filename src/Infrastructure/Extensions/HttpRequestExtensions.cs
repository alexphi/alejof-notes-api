#nullable enable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Alejof.Notes.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool GetPublishedQueryParam(this HttpRequest req)
            => req.GetQueryParameterDictionary().TryGetValue("published", out var value) && bool.TryParse(value, out var boolValue) ? boolValue : false;

        public static string? GetQueryParam(this HttpRequest req, string paramName)
            => req.GetQueryParameterDictionary().TryGetValue("format", out var formatValue) ? formatValue : null;

        public static async Task<T> GetJsonBodyAs<T>(this HttpRequest req)
            => JsonConvert.DeserializeObject<T>(await req.ReadAsStringAsync());
    }
}
