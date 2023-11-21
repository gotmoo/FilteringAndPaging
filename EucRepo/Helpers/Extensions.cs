using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EucRepo.Helpers;

public static class Extensions
{
    public static DataTable ToPivotTable<T, TColumn, TRow, TData>(
        this IEnumerable<T> source,
        Func<T, TColumn> columnSelector,
        Expression<Func<T, TRow>> rowSelector,
        Func<IEnumerable<T>, TData> dataSelector)
    {
        DataTable table = new DataTable();
        var rowName = ((MemberExpression)rowSelector.Body).Member.Name;
        table.Columns.Add(new DataColumn(rowName));
        var enumerable = source.ToList();
        var columns = enumerable.Select(columnSelector).Distinct();

        var enumerable1 = columns.ToList();
        foreach (var column in enumerable1)
            table.Columns.Add(new DataColumn(column?.ToString()));

        var rows = enumerable.GroupBy(rowSelector.Compile())
            .Select(rowGroup => new
            {
                Key = rowGroup.Key,
                Values = enumerable1.GroupJoin(
                    rowGroup,
                    c => c,
                    columnSelector,
                    (c, columnGroup) => dataSelector(columnGroup))
            });

        foreach (var row in rows)
        {
            var dataRow = table.NewRow();
            var items = row.Values.Cast<object>().ToList();
            Debug.Assert(row.Key != null, "row.Key != null");
            items.Insert(0, row.Key);
            dataRow.ItemArray = items.ToArray();
            table.Rows.Add(dataRow);
        }

        return table;
    }
}

public static class StringExtensions
{
    public static string TruncateLongString(this string? longString, int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(longString))
            return "";
        return longString.Length <= maxLength ? longString : string.Concat(longString.AsSpan(0, maxLength), "......");
    }
}
public static class DateExtensions
{
    public static string? ToNullableShortDate(this DateTime? myDate)
    {
        if (myDate.HasValue)
        {
            return myDate.Value.ToShortDateString();
        }
        else
        {
            return null;
        }
    }

    public static string? ToNullableDateString(this DateTime? myDate, string format)
    {
        if (myDate.HasValue)
        {
            return myDate.Value.ToString(format);
        }
        else
        {
            return null;
        }
    }

    public static DateTime Trim(this DateTime date, long ticks)
    {
        return new DateTime(date.Ticks - (date.Ticks % ticks), date.Kind);
    }

    public static string ToRelativeTimeString(this DateTime myDate, bool isLocalTime = false)
    {
        var timespan = (isLocalTime ? DateTime.Now : DateTime.UtcNow) - myDate;
        return  timespan.ToNaturalLanguage();
    }
    public static string ToNaturalLanguage(this TimeSpan @this)
    {
        const int daysInWeek = 7;
        const int daysInMonth = 30;
        const int daysInYear = 365;
        const long threshold = 100 * TimeSpan.TicksPerMillisecond;
        @this = @this.TotalSeconds < 0
            ? TimeSpan.FromSeconds(@this.TotalSeconds * -1)
            : @this;
        return (@this.Ticks + threshold) switch
        {
            < 2 * TimeSpan.TicksPerSecond => "a second",
            < 1 * TimeSpan.TicksPerMinute => @this.Seconds + " seconds",
            < 2 * TimeSpan.TicksPerMinute => "a minute",
            < 1 * TimeSpan.TicksPerHour => @this.Minutes + " minutes",
            < 2 * TimeSpan.TicksPerHour => "an hour",
            < 1 * TimeSpan.TicksPerDay => @this.Hours + " hours",
            < 2 * TimeSpan.TicksPerDay => "a day",
            < 1 * daysInWeek * TimeSpan.TicksPerDay => @this.Days + " days",
            < 2 * daysInWeek * TimeSpan.TicksPerDay => "a week",
            < 1 * daysInMonth * TimeSpan.TicksPerDay => (@this.Days / daysInWeek).ToString("F0") + " weeks",
            < 2 * daysInMonth * TimeSpan.TicksPerDay => "a month",
            < 1 * daysInYear * TimeSpan.TicksPerDay => (@this.Days / daysInMonth).ToString("F0") + " months",
            < 2 * daysInYear * TimeSpan.TicksPerDay => "a year",
            _ => (@this.Days / daysInYear).ToString("F0") + " years"
        };
    }

    /// <summary>
    /// Convert a <see cref="DateTime"/> to a natural language representation.
    /// </summary>
    /// <example>
    /// <code>
    /// (DateTime.Now - TimeSpan.FromSeconds(10)).ToNaturalLanguage()
    /// // 10 seconds ago
    /// </code>
    /// </example>
    public static string ToNaturalLanguage(this DateTime @this)
    {
        TimeSpan timeSpan = @this - DateTime.Now;
        return timeSpan.TotalSeconds switch
        {
            >= 1 => timeSpan.ToNaturalLanguage() + " until",
            <= -1 => timeSpan.ToNaturalLanguage() + " ago",
            _ => "now",
        };
    }
    public static string JoinToList(this IEnumerable<string> myList)
    {
        return string.Join("\n", myList);
    }
    public static string JoinToList(this IEnumerable<int?> myList)
    {
        return string.Join("\n", myList);
    }
    public static string[] SplitToStringArray(this string? myStringList, bool sort = true)
    {
        if (string.IsNullOrWhiteSpace(myStringList)) return Array.Empty<string>();
        string[] stringList = Regex.Split(myStringList, @"\s+");
        if (sort)
        {
            stringList= stringList.Order().ToArray();
        }
        stringList = stringList.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return stringList;
    }

}