using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DeviceHive.Data.Repositories
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> OrderBy<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> keySelector, SortOrder sortOrder)
        {
            return sortOrder == SortOrder.ASC ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }

        public static IOrderedQueryable<T> ThenBy<T, TKey>(this IOrderedQueryable<T> query, Expression<Func<T, TKey>> keySelector, SortOrder sortOrder)
        {
            return sortOrder == SortOrder.ASC ? query.ThenBy(keySelector) : query.ThenByDescending(keySelector);
        }
    }
}
