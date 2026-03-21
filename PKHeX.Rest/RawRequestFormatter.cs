using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace PKHeX.Rest
{
    public class RawRequestBodyFormatter : InputFormatter
    {
        public RawRequestBodyFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
            // Add other binary types as needed: image/png, application/pdf, etc.
        }

        public override bool CanRead(InputFormatterContext context)
        {
            var contentType = context.HttpContext.Request.ContentType;
            return !string.IsNullOrEmpty(contentType) &&
                   (contentType == "application/octet-stream" ||
                    contentType.StartsWith("image/") ||
                    contentType.StartsWith("application/"));
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms);
            return await InputFormatterResult.SuccessAsync(ms.ToArray());
        }
    }
}
