using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSWPF.Web.Responses;

public sealed class StreamResponse : BasicResponse, IAsyncDisposable, IDisposable {
    public Stream? Content { get; }
    
    public long Length { get; }

    private readonly HttpResponseMessage ResponseMessage;

    internal StreamResponse(HttpResponseMessage httpResponseMessage, Stream content) : this(httpResponseMessage) => Content = content ?? throw new ArgumentNullException(nameof(content));

    internal StreamResponse(HttpResponseMessage httpResponseMessage) : base(httpResponseMessage) {
        ResponseMessage = httpResponseMessage ?? throw new ArgumentNullException(nameof(httpResponseMessage));
        Length = httpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault();
    }

    public void Dispose() {
        Content?.Dispose();
        ResponseMessage.Dispose();
    }

    public async ValueTask DisposeAsync() {
        if (Content != null) {
            await Content.DisposeAsync().ConfigureAwait(false);
        }

        ResponseMessage.Dispose();
    }
}