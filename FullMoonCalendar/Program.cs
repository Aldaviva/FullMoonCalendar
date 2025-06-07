using AngleSharp;
using AngleSharp.Io;
using Bom.Squad;
using FullMoonCalendar;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Net.Http.Headers;
using System.Text;
using Unfucked;
using HeaderNames = Microsoft.Net.Http.Headers.HeaderNames;

const string ICALENDAR_MIME_TYPE    = "text/calendar;charset=utf-8";
const int    CACHE_DURATION_MINUTES = 24 * 60;
const string USER_AGENT_STRING      = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

string[]                varyHeaderValue         = ["Accept-Encoding"];
CacheControlHeaderValue cacheControlHeaderValue = new() { Public = true, MaxAge = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES) };

BomSquad.DefuseUtf8Bom();

WebApplicationBuilder webappBuilder = WebApplication.CreateBuilder(args);
webappBuilder.Services
    .AddOutputCache()
    .AddResponseCaching()
    .AddSingleton<FullMoonService, TimeAndDateWebScrapingFullMoonService>()
    .AddSingleton(_ => BrowsingContext.New(Configuration.Default.With(new DefaultHttpRequester(USER_AGENT_STRING)).WithDefaultLoader()));

using WebApplication webapp = webappBuilder.Build();

webapp.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto })
    .UseOutputCache()
    .UseResponseCaching()
    .Use(async (context, next) => {
        context.Response.GetTypedHeaders().CacheControl = cacheControlHeaderValue;
        context.Response.Headers[HeaderNames.Vary]      = varyHeaderValue;
        await next();
    });

webapp.MapGet("/", [OutputCache(Duration = CACHE_DURATION_MINUTES * 60)] async (HttpContext request, FullMoonService fullMoonService) => {
    Calendar fullMoonCalendar = new() { Method = CalendarMethods.Publish };
    request.Response.ContentType = ICALENDAR_MIME_TYPE;

    DateTime today = DateTime.Today;
    DateTime start = today.AddYears(-1);
    DateTime end   = today.AddYears(1);

    await foreach (DateTime fullMoon in fullMoonService.getFullMoons(start, end)) {
        fullMoonCalendar.Events.Add(new CalendarEvent {
            Uid      = $"{fullMoon:yyyyMMdd}",
            Start    = fullMoon.ToIcalDateTime(),
            IsAllDay = true,
            Summary  = "🌕 Full Moon"
        });
    }

    await new CalendarSerializer().SerializeAsync(fullMoonCalendar, request.Response.Body, Encoding.UTF8);
});

webapp.Run();