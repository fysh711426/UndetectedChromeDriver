using System;
using System.Net;
using System.Net.Http;

namespace SeleniumUndetectedChromeDriver
{
    internal static class Http
    {
        private static readonly Lazy<HttpClient> HttpClientLazy = new(() =>
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false
            };

            if (handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            return new HttpClient(handler, true);
        });

        public static HttpClient Client => HttpClientLazy.Value;
    }
}
