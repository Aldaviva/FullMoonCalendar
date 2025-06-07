using AngleSharp;
using AngleSharp.Dom;
using System.Globalization;
using Unfucked;

namespace FullMoonCalendar;

public class TimeAndDateWebScrapingFullMoonService(IBrowsingContext browser, ILogger<TimeAndDateWebScrapingFullMoonService> logger): FullMoonService {

    private static readonly UrlBuilder BASE_URI = new("https://www.timeanddate.com/moon/phases/usa/san-jose?year={year}");

    public async IAsyncEnumerable<DateTime> getFullMoons(DateTime start, DateTime end) {
        foreach (int year in Enumerable.Range(start.Year, end.Year - start.Year + 1)) {
            using IDocument yearPage = await browser.OpenAsync(Url.Convert(BASE_URI.ResolveTemplate("year", year)));

            foreach (IElement lunationRow in yearPage.QuerySelectorAll("table.tb-sm tbody tr")) {
                string rawDate = lunationRow.Children[5].Text().Trim();
                string rawTime = lunationRow.Children[6].Text().Trim()
                    .Replace("midnight", "am")
                    .Replace("noon", "pm");

                if (!string.IsNullOrEmpty(rawDate) && !string.IsNullOrEmpty(rawTime)) {
                    DateTime dateTime;
                    try {
                        dateTime = DateTime.ParseExact($"{rawDate}, {year} {rawTime}", "MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
                    } catch (FormatException e) {
                        logger.LogWarning(e, "Failed to parse datetime from TimeAndDate.com");
                        continue;
                    }

                    if (start <= dateTime && dateTime <= end) {
                        yield return dateTime;
                    }
                }
            }
        }
    }

}