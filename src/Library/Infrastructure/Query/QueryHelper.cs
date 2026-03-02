using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Library.Domain.Models.Queries;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Query;

/// <summary>
/// Applies <see cref="QueryRequest"/> to any <see cref="IQueryable{T}"/> using
/// expression trees.  Zero dependency on OData parsers or System.Linq.Dynamic.
/// </summary>
public static class QueryHelper
{
    private const int DefaultTop = 50;
    private const int MaxTop = 500;

    /// <summary>
    /// Applies filters, ordering, skip, and top to <paramref name="source"/>.
    /// Returns the materialised list as a convenience.
    /// </summary>
    public static async Task<List<T>> ExecuteAsync<T>(
        IQueryable<T> source,
        QueryRequest? request,
        CancellationToken ct = default)
    {
        var query = Apply(source, request);
        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Applies filters, ordering, skip, and top without materialising.
    /// </summary>
    public static IQueryable<T> Apply<T>(IQueryable<T> source, QueryRequest? request)
    {
        if (request is null)
            return source.Take(DefaultTop);

        var query = source;

        // --- Filters ---
        if (request.Filters is { Length: > 0 })
        {
            foreach (var f in request.Filters)
                query = ApplyFilter(query, f);
        }

        // --- Ordering ---
        if (request.OrderBy is { Length: > 0 })
        {
            IOrderedQueryable<T>? ordered = null;
            foreach (var sort in request.OrderBy)
            {
                var keySelector = BuildPropertyLambda<T>(sort.Field);
                ordered = ordered is null
                    ? sort.Descending
                        ? query.OrderByDescending(keySelector)
                        : query.OrderBy(keySelector)
                    : sort.Descending
                        ? ordered.ThenByDescending(keySelector)
                        : ordered.ThenBy(keySelector);
            }
            query = ordered ?? query;
        }

        // --- Paging ---
        if (request.Skip is > 0)
            query = query.Skip(request.Skip.Value);

        var top = Math.Clamp(request.Top ?? DefaultTop, 1, MaxTop);
        query = query.Take(top);

        return query;
    }

    // ── Filter application ──────────────────────────────────────────────

    private static IQueryable<T> ApplyFilter<T>(IQueryable<T> source, FilterCondition filter)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var prop = ResolveProperty(typeof(T), filter.Field);
        var member = Expression.Property(param, prop);
        var value = ConvertValue(filter.Value, prop.PropertyType);
        var constant = Expression.Constant(value, prop.PropertyType);

        Expression body = NormaliseOperator(filter.Operator) switch
        {
            "eq" => Expression.Equal(member, constant),
            "ne" => Expression.NotEqual(member, constant),
            "gt" => Expression.GreaterThan(member, constant),
            "lt" => Expression.LessThan(member, constant),
            "ge" => Expression.GreaterThanOrEqual(member, constant),
            "le" => Expression.LessThanOrEqual(member, constant),
            "contains" => BuildStringCall(member, nameof(string.Contains), constant),
            "startswith" => BuildStringCall(member, nameof(string.StartsWith), constant),
            "endswith" => BuildStringCall(member, nameof(string.EndsWith), constant),
            var op => throw new ArgumentException($"Unsupported operator: {op}")
        };

        var lambda = Expression.Lambda<Func<T, bool>>(body, param);
        return source.Where(lambda);
    }

    // ── Ordering helpers ────────────────────────────────────────────────

    /// <summary>
    /// Builds a property-access lambda typed as <c>Expression{Func{T, object}}</c>
    /// so it can be used by OrderBy / ThenBy without knowing the property type
    /// at compile time.
    /// </summary>
    private static Expression<Func<T, object>> BuildPropertyLambda<T>(string fieldName)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var prop = ResolveProperty(typeof(T), fieldName);
        Expression access = Expression.Property(param, prop);

        // Box value types so the lambda always returns object.
        if (prop.PropertyType.IsValueType)
            access = Expression.Convert(access, typeof(object));

        return Expression.Lambda<Func<T, object>>(access, param);
    }

    // ── Shared plumbing ─────────────────────────────────────────────────

    private static PropertyInfo ResolveProperty(Type type, string name)
    {
        return type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new ArgumentException($"Property '{name}' not found on {type.Name}");
    }

    private static object ConvertValue(string raw, Type target)
    {
        // Unwrap nullable.
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (underlying == typeof(string))
            return raw;
        if (underlying == typeof(bool))
            return bool.Parse(raw);
        if (underlying == typeof(DateTime))
            return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (underlying == typeof(Guid))
            return Guid.Parse(raw);
        if (underlying == typeof(long))
            return long.Parse(raw, CultureInfo.InvariantCulture);
        if (underlying == typeof(int))
            return int.Parse(raw, CultureInfo.InvariantCulture);
        if (underlying == typeof(double))
            return double.Parse(raw, CultureInfo.InvariantCulture);

        return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
    }

    private static Expression BuildStringCall(Expression member, string methodName, Expression arg)
    {
        var method = typeof(string).GetMethod(methodName, [typeof(string)])!;
        return Expression.Call(member, method, arg);
    }

    private static string NormaliseOperator(string op) =>
        op.Trim().ToLowerInvariant() switch
        {
            "==" or "=" or "eq" => "eq",
            "!=" or "<>" or "ne" => "ne",
            ">" or "gt" => "gt",
            "<" or "lt" => "lt",
            ">=" or "ge" => "ge",
            "<=" or "le" => "le",
            "contains" => "contains",
            "startswith" => "startswith",
            "endswith"  => "endswith",
            var other => other
        };
}
