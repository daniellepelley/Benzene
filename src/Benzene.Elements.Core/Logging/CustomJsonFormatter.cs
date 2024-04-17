using System;
using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace Benzene.Elements.Core.Logging
{
    public class CustomJsonFormatter : ITextFormatter
    {
        private readonly JsonValueFormatter _valueFormatter;

        public CustomJsonFormatter(JsonValueFormatter valueFormatter = null) =>
            _valueFormatter = valueFormatter ?? new JsonValueFormatter("$type");

        public void Format(LogEvent logEvent, TextWriter output)
        {
            FormatEvent(logEvent, output, _valueFormatter);
            output.WriteLine();
        }

        private static void FormatEvent(
            LogEvent logEvent,
            TextWriter output,
            JsonValueFormatter valueFormatter)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (valueFormatter == null)
                throw new ArgumentNullException(nameof(valueFormatter));
            output.Write("{\"timeStamp\":\"");
            output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
            output.Write("\",\"messageTemplate\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);
            output.Write(",\"message\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.RenderMessage(), output);
            var source = logEvent.MessageTemplate.Tokens.OfType<PropertyToken>()
                .Where(pt => pt.Format != null)
                .ToArray();
            if (source.Any())
            {
                output.Write(",\"@r\":[");
                var str = "";
                foreach (var propertyToken in source)
                {
                    output.Write(str);
                    str = ",";
                    var stringWriter1 = new StringWriter();
                    var properties = logEvent.Properties;
                    propertyToken.Render(properties, stringWriter1);
                    JsonValueFormatter.WriteQuotedJsonString(stringWriter1.ToString(), output);
                }

                output.Write(']');
            }

            output.Write(",\"level\":\"");
            output.Write(logEvent.Level);
            output.Write('"');

            if (logEvent.Exception != null)
            {
                output.Write(",\"exception\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            foreach (var property in
                     logEvent.Properties)
            {
                var str = property.Key;
                if (str.Length > 0 && str[0] == '@')
                    str = "@" + str;
                output.Write(',');
                JsonValueFormatter.WriteQuotedJsonString(str, output);
                output.Write(':');
                valueFormatter.Format(property.Value, output);
            }

            output.Write('}');
        }
    }
}
