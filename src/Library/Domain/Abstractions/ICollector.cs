using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Domain.Abstractions;

/// <summary>
/// Collects diagnostic metrics by executing system commands.
/// An optional <paramref name="since"/> timestamp filters results to only those recorded after that point.
/// </summary>
public interface ICollector<T>
{
    Task<IEnumerable<T>> CollectAsync(DateTime? since = null, CancellationToken ct = default);
}
