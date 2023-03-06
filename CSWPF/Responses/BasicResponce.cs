using System;
using System.Net;
using System.Net.Http;
using JetBrains.Annotations;

namespace CSWPF.Responses;

public class BasicResponce
{
    [PublicAPI]
    public Uri FinalUri { get; }

    [PublicAPI]
    public HttpStatusCode StatusCode { get; }

    internal BasicResponce(HttpResponseMessage httpResponseMessage) {
        ArgumentNullException.ThrowIfNull(httpResponseMessage);

        FinalUri = httpResponseMessage.Headers.Location ?? httpResponseMessage.RequestMessage?.RequestUri ?? throw new InvalidOperationException();
        StatusCode = httpResponseMessage.StatusCode;
    }
    
    internal BasicResponce(BasicResponce basicResponse) {
        ArgumentNullException.ThrowIfNull(basicResponse);

        FinalUri = basicResponse.FinalUri;
        StatusCode = basicResponse.StatusCode;
    }
    
}