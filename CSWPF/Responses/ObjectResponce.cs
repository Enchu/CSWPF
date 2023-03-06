using System;
using JetBrains.Annotations;

namespace CSWPF.Responses;

public class ObjectResponce<T>: BasicResponce
{
    [PublicAPI]
    public T? Content { get; }

    public ObjectResponce(BasicResponce basicResponce, T content) : this(basicResponce) =>
        Content = content ?? throw new ArgumentException(nameof(content));

    public ObjectResponce(BasicResponce basicResponce) : base(basicResponce) =>
        ArgumentNullException.ThrowIfNull(basicResponce);
}