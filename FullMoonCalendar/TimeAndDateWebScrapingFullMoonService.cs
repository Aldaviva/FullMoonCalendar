using System.Globalization;
using AngleSharp;
using AngleSharp.Dom;
using jaytwo.FluentUri;

namespace FullMoonCalendar;

public class TimeAndDateWebScrapingFullMoonService: FullMoonService {

    // ExceptionAdjustment: M:System.Uri.#ctor(System.String) -T:System.UriFormatException
    private static readonly Uri         BASE_URI     = new("https://www.timeanddate.com/moon/phases/usa/san-jose");
    private static readonly CultureInfo CULTURE_INFO = new("en-US");

    private readonly IBrowsingContext browser;

    public TimeAndDateWebScrapingFullMoonService(IBrowsingContext browser) {
        this.browser = browser;
    }

    public async IAsyncEnumerable<DateTime> getFullMoons(DateTime start, DateTime end) {
        foreach (int year in Enumerable.Range(start.Year, end.Year - start.Year + 1)) {
            using IDocument yearPage = await browser.OpenAsync(Url.Convert(BASE_URI.WithQueryParameter("year", year)));

            foreach (IElement lunationRow in yearPage.QuerySelectorAll("table.tb-sm tbody tr")) {
                string rawDate = lunationRow.Children[5].Text().Trim();
                string rawTime = lunationRow.Children[6].Text().Trim()
                    .Replace("midnight", "am")
                    .Replace("noon", "pm");

                if (!string.IsNullOrEmpty(rawDate) && !string.IsNullOrEmpty(rawTime)) {
                    DateTime dateTime;
                    try {
                        dateTime = DateTime.ParseExact($"{rawDate}, {year} {rawTime}", "MMM d, yyyy h:mm tt", CULTURE_INFO);
                    } catch (FormatException) {
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