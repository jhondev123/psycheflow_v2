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

    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        int currentPage = Math.Max(page ?? 1, 1);
        int size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        int total = await query.CountAsync(cancellationToken);
        List<T> items = await query
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, currentPage, size, total);
    }
}
