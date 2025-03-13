using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurSyncLib
{
    internal class RestAPI2
    {
        public static string baseUrl = "us2.api.concursolutions.com";
        private string loginUri = "/oauth2/v0/token";
        private AuthToken authToken;

        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private static readonly Queue<DateTime> requestTimestamps = new Queue<DateTime>();
        private const int MAX_CALLS_PER_MINUTE = 600;
        private static readonly TimeSpan TIME_WINDOW = TimeSpan.FromMinutes(1);

        public async Task<string> MakeRestCall(string method, string uri, NameValueCollection nvc, bool logErrors)
        {
            await EnforceRateLimitAsync();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://" + baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.access_token);

                var query = new FormUrlEncodedContent(nvc.AllKeys.ToDictionary(k => k, k => nvc[k]));
                var requestUri = $"{uri}?{await query.ReadAsStringAsync()}";
                requestUri = requestUri.TrimEnd('?');

                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), requestUri));

                if (!response.IsSuccessStatusCode)
                {
                    if (logErrors)
                    {
                        StringBuilder s = new StringBuilder();
                        s.AppendLine("URL: " + requestUri);
                        s.AppendLine("Response Status Code: " + (int)response.StatusCode);
                        s.AppendLine("Response Header: " + response.Headers.ToString());
                        s.AppendLine("Response Body: " + await response.Content.ReadAsStringAsync());
                        Log.LogTrace(s.ToString());
                    }
                    return null;
                }
                return await response.Content.ReadAsStringAsync();
            }
        }

        private async Task EnforceRateLimitAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                DateTime now = DateTime.UtcNow;

                // Remove outdated timestamps
                while (requestTimestamps.Count > 0 && now - requestTimestamps.Peek() > TIME_WINDOW)
                {
                    requestTimestamps.Dequeue();
                }

                // If we're at the limit, wait until a slot is available
                while (requestTimestamps.Count >= MAX_CALLS_PER_MINUTE)
                {
                    TimeSpan delay = TIME_WINDOW - (now - requestTimestamps.Peek());
                    Log.LogInfo($"Rate limit reached. Waiting {delay.TotalMilliseconds} ms before next API call.");
                    await Task.Delay(delay);
                    now = DateTime.UtcNow;

                    // Clean up old timestamps after delay
                    while (requestTimestamps.Count > 0 && now - requestTimestamps.Peek() > TIME_WINDOW)
                    {
                        requestTimestamps.Dequeue();
                    }
                }

                // Register this request
                requestTimestamps.Enqueue(now);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
