using System.Text;
using BenMakesGames.MoonMath;
using FullMoonCalendar;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Net.Http.Headers;

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

    DateTime today = DateTime.Today;
    DateTime start = today.AddYears(-1);
    DateTime end   = today.AddYears(1);

    DateTime currentDate = findNextFullMoon(start, false);
    while (currentDate <= end) {
        fullMoonCalendar.Events.Add(new CalendarEvent {
            Uid      = $"{currentDate:yyyyMMdd}",
            Start    = currentDate.ToIDateTime(),
            IsAllDay = true,
            Summary  = "🌕 Full Moon",
            Alarms   = { new Alarm { Action = AlarmAction.Display, Trigger = new Trigger(TimeSpan.FromHours(2)) } }
        });
        currentDate = findNextFullMoon(currentDate, true);
    }

    await new CalendarSerializer().SerializeAsync(fullMoonCalendar, request.Response.Body, utf8);
});

static DateTime findNextFullMoon(DateTime start, bool excludeStart) {
    if (excludeStart && isFullMoon(start)) {
        // seek to roughly the next new moon so we'll get a different full moon below
        start = start.AddDays(22);
    }

    while (!isFullMoon(start)) {
        start = start.AddDays(1);
    }

    return start;
}

static bool isFullMoon(DateTime date) => date.GetMoonAge() is >= 14.155 and < 16.61096;

webapp.Run();