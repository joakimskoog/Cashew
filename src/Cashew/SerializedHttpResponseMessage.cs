using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Cashew.Tests")]
namespace Cashew
{
    //todo: Add comments explaining why we need this class for now
    internal class SerializedHttpResponseMessage
    {
        public HttpResponseMessage Response { get; set; }
        public byte[] Content { get; set; }

        internal SerializedHttpResponseMessage(HttpResponseMessage response, byte[] content)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
            Content = content;
        }

        internal static async Task<SerializedHttpResponseMessage> Create(HttpResponseMessage response)
        {
            byte[] content;
            if (response.Content == null)
            {
                content = new byte[0];
            }
            else
            {
                content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            return new SerializedHttpResponseMessage(response, content);
        }

        internal HttpResponseMessage ParseHttpResponseMessage()
        {
            var headers = Response.Content.Headers;
            Response.Content = new StreamContent(new MemoryStream(Content));

            foreach (var contentHeader in headers)
            {
                Response.Content.Headers.Add(contentHeader.Key, contentHeader.Value);
            }

            return Response;
        }
    }
}