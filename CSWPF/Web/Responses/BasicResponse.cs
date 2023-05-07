using System;
using System.Net;
using System.Net.Http;

namespace CSWPF.Web.Responses;

public class BasicResponse {
    public Uri FinalUri { get; }
    public HttpStatusCode StatusCode { get; }

    internal BasicResponse(HttpResponseMessage httpResponseMessage) {
        ArgumentNullException.ThrowIfNull(httpResponseMessage);

        FinalUri = httpResponseMessage.Headers.Location ?? httpResponseMessage.RequestMessage?.RequestUri ?? throw new InvalidOperationException();
        StatusCode = httpResponseMessage.StatusCode;
    }

    internal BasicResponse(BasicResponse basicResponse) {
        ArgumentNullException.ThrowIfNull(basicResponse);

        FinalUri = basicResponse.FinalUri;
        StatusCode = basicResponse.StatusCode;
    }
}