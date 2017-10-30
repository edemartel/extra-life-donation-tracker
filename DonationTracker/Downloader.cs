using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DonationTracker
{
    public class Downloader : IDisposable
    {
        private readonly HttpClient _client;

        public Downloader()
        {
            _client = new HttpClient();
        }

        public async Task<JToken> Get(string url)
        {
            using (var response = await _client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                using (var reader = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync())))
                    return JToken.Load(reader);
            }
        }

        public void Dispose()
        {
            _client.CancelPendingRequests();
            _client.Dispose();
        }
    }
}
