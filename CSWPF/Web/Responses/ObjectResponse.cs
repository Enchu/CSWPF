using System;

namespace CSWPF.Web.Responses;

public sealed class ObjectResponse<T> : BasicResponse {
    public T? Content { get; }

    public ObjectResponse(BasicResponse basicResponse, T content) : this(basicResponse) => Content = content ?? throw new ArgumentNullException(nameof(content));

    public ObjectResponse(BasicResponse basicResponse) : base(basicResponse) => ArgumentNullException.ThrowIfNull(basicResponse);
}