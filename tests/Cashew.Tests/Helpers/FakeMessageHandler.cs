using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cashew.Tests.Helpers
{
    public class FakeMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return Response;
        }
    }
}