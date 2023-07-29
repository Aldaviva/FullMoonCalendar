using System.Text;
using BenMakesGames.MoonMath;
using FullMoonCalendar;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Net.Http.Headers;
using NodaTime;

/*DateTime start = new(DateTime.Now.Year, 1, 1, 12 + 10, 0, 0);
while (start.Year == DateTime.Now.Year) {
    if (start.GetMoonAge() >= 14.155) {
        Console.WriteLine(start.ToString());
        start = start.AddDays(22);
    } else {
        start = start.AddDays(1);
    }
}

return;*/

const string ICALENDAR_MIME_TYPE = "text/calendar;charset=UTF-8";
Encoding     utf8                = new UTF8Encoding(false, true);
DateTimeZone zone                = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];

WebApplicationBuilder webappBuilder = WebApplication.CreateBuilder(args);
// webappBuilder.WebHost.ConfigureKestrel(options => options.AllowSynchronousIO             = true);
// webappBuilder.Services.Configure<IISServerOptions>(options => options.AllowSynchronousIO = true);
WebApplication webapp = webappBuilder.Build();

webapp.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto })
    .Use(async (context, next) => {
        context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromDays(15) };
        context.Response.Headers[HeaderNames.Vary]      = new[] { "Accept-Encoding" };
        await next();
    });

webapp.MapGet("/", async request => {
    request.Response.ContentType = ICALENDAR_MIME_TYPE;
    Calendar fullMoonCalendar = new() { Method = CalendarMethods.Publish };

    LocalDate today = SystemClock.Instance.GetCurrentInstant().InZone(zone).Date;
    LocalDate start = today.PlusYears(-1);
    LocalDate end   = today.PlusYears(1);

    LocalDate currentDate = findNextFullMoon(start, false);
    while (currentDate <= end) {
        fullMoonCalendar.Events.Add(new CalendarEvent {
            Uid      = $"{currentDate.ToDateTimeUnspecified():yyyyMMdd}",
            Start    = currentDate.AtStartOfDayInZone(zone).ToIDateTime(),
            IsAllDay = true,
            Summary  = "🌕 Full Moon",
            Alarms   = { new Alarm { Action = AlarmAction.Display, Trigger = new Trigger(TimeSpan.FromHours(0)) } }
        });
        currentDate = findNextFullMoon(currentDate, true);
    }

    await new CalendarSerializer().SerializeAsync(fullMoonCalendar, request.Response.Body, utf8);
});

static LocalDate findNextFullMoon(LocalDate current, bool excludeStart) {
    if (excludeStart && isFullMoon(current)) {
        // seek to roughly the next new moon so we'll get a different full moon below
        current = current.PlusDays(22);
    }

    while (!isFullMoon(current)) {
        current = current.PlusDays(1);
    }

    return current;
}

static bool isFullMoon(LocalDate date) => date.ToDateTimeUnspecified().GetMoonAge() is >= 14.155 and < 16.61096;

webapp.Run();