using Microsoft.EntityFrameworkCore;

namespace Psycheflow.Api.Common.Endpoints;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query, int? page, int? pageSize, CancellationToken cancellationToken) =>
        query.ToPagedResponseAsync(page, pageSize, item => item, cancellationToken);

    /// <summary>Pagina no banco e aplica <paramref name="map"/> em memória (útil para value objects).</summary>
    public static async Task<PagedResponse<TResult>> ToPagedResponseAsync<TSource, TResult>(
        this IQueryable<TSource> query, int? page, int? pageSize, Func<TSource, TResult> map, CancellationToken cancellationToken)
    {
        int currentPage = Math.Max(page ?? 1, 1);
        int size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        int total = await query.CountAsync(cancellationToken);
        List<TSource> items = await query
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TResult>([.. items.Select(map)], currentPage, size, total);
    }
}
