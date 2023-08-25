using System.Text;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using NodaTime;
using NodaTime.Extensions;

// ReSharper disable InconsistentNaming - extension methods

namespace FullMoonCalendar;

public static class Extensions {

    public static IDateTime ToIDateTime(this DateTimeOffset dateTimeOffset) => new CalDateTime(dateTimeOffset.DateTime, dateTimeOffset.ToZonedDateTime().Zone.Id);

    public static IDateTime ToIDateTime(this DateTime dateTime) => new CalDateTime(dateTime);

    public static IDateTime ToIDateTime(this ZonedDateTime zonedDateTime) => new CalDateTime(zonedDateTime.ToDateTimeUnspecified(), zonedDateTime.Zone.Id);

    /// <summary>
    /// Without this, you would have to manually configure your web server (Kestrel and IIS) to allow synchronous writes:
    /// <code>
    /// webappBuilder.WebHost.ConfigureKestrel(options =&gt; options.AllowSynchronousIO = true);
    /// webappBuilder.Services.Configure&lt;IISServerOptions&gt;(options =&gt; options.AllowSynchronousIO = true);
    /// </code>
    /// </summary>
    public static async Task SerializeAsync(this SerializerBase serializerBase, object obj, Stream stream, Encoding encoding) {
        await using StreamWriter streamWriter = new(stream, encoding, 1024, true);

        serializerBase.SerializationContext.Push(obj);
        await streamWriter.WriteAsync(serializerBase.SerializeToString(obj));
        serializerBase.SerializationContext.Pop();
    }

}