using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ParkIRC.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<decimal> SumDecimalAsync<T>(
            this IQueryable<T> source,
            Expression<Func<T, decimal>> selector)
        {
            // First materialize the data, then perform the sum
            var items = await source.Select(selector).ToListAsync();
            return items.Sum();
        }

        public static IQueryable<T> OrderByTimeSpan<T>(
            this IQueryable<T> source,
            Expression<Func<T, TimeSpan>> keySelector,
            bool ascending = true)
        {
            // Convert TimeSpan to total seconds for SQLite compatibility
            var convertedSelector = Expression.Lambda<Func<T, double>>(
                Expression.Convert(
                    Expression.Property(keySelector.Body, nameof(TimeSpan.TotalSeconds)),
                    typeof(double)
                ),
                keySelector.Parameters
            );

            return ascending
                ? source.OrderBy(convertedSelector)
                : source.OrderByDescending(convertedSelector);
        }

        public static IQueryable<T> ThenByTimeSpan<T>(
            this IOrderedQueryable<T> source,
            Expression<Func<T, TimeSpan>> keySelector,
            bool ascending = true)
        {
            // Convert TimeSpan to total seconds for SQLite compatibility
            var convertedSelector = Expression.Lambda<Func<T, double>>(
                Expression.Convert(
                    Expression.Property(keySelector.Body, nameof(TimeSpan.TotalSeconds)),
                    typeof(double)
                ),
                keySelector.Parameters
            );

            return ascending
                ? source.ThenBy(convertedSelector)
                : source.ThenByDescending(convertedSelector);
        }
    }
} 