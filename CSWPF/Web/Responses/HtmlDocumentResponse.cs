using System;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

namespace CSWPF.Web.Responses;

public sealed class HtmlDocumentResponse : BasicResponse, IDisposable {
    public IDocument? Content { get; }

    public HtmlDocumentResponse(BasicResponse basicResponse) : base(basicResponse) => ArgumentNullException.ThrowIfNull(basicResponse);

    private HtmlDocumentResponse(BasicResponse basicResponse, IDocument content) : this(basicResponse) => Content = content ?? throw new ArgumentNullException(nameof(content));

    public void Dispose() => Content?.Dispose();
    
    public static async Task<HtmlDocumentResponse?> Create(StreamResponse streamResponse) {
        ArgumentNullException.ThrowIfNull(streamResponse);

        IBrowsingContext context = BrowsingContext.New();

        IDocument document = await context.OpenAsync(request => request.Content(streamResponse.Content, true)).ConfigureAwait(false);

        return new HtmlDocumentResponse(streamResponse, document);
    }
}