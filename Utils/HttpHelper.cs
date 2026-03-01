using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Web;
using HtmlAgilityPack;

namespace mystamps.Utils
{
    internal static class HttpHelper
    {
        public static async Task<string> GetHtml(string url)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            //Console.WriteLine($"Retrieving HTML from: {url}");

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"HTTP error {(int)response.StatusCode} for URL: {url}");
                new DbHelper("stamps.db").SaveError(url, $"Failed to retrieve HTML. Status code: {(int)response.StatusCode} ({response.ReasonPhrase})");
                throw new HttpRequestException($"Failed to retrieve HTML. Status code: {(int)response.StatusCode} ({response.ReasonPhrase}) for URL: {url}");
            }

            byte[] contentBytes = await response.Content.ReadAsByteArrayAsync();

            // Prefer x-archive-guessed-charset, then Content-Type, then fallback
            string guessedCharset = response.Headers.TryGetValues("x-archive-guessed-charset", out var guessedValues)
                ? guessedValues.FirstOrDefault()
                : null;
            string charsetFromHeader = GetCharsetFromContentType(response.Content.Headers.ContentType?.ToString());

            string html = null;
            string encodingName = guessedCharset ?? charsetFromHeader;

            // Try preferred encoding from headers
            if (!string.IsNullOrEmpty(encodingName))
            {
                if (TryDecode(contentBytes, encodingName, out html))
                {
                    //Console.WriteLine($"Using encoding from header: {encodingName}");
                }
            }
            // Try Windows-1252
            if (html == null)
            {
                if (TryDecode(contentBytes, "windows-1252", out html))
                {
                    //Console.WriteLine("Using Windows-1252 encoding");
                }
            }
            // Try ISO-8859-1
            if (html == null)
            {
                if (TryDecode(contentBytes, "ISO-8859-1", out html))
                {
                    //Console.WriteLine("Fallback to ISO-8859-1 encoding");
                }
            }
            // Try UTF-8
            if (html == null)
            {
                if (TryDecode(contentBytes, "UTF-8", out html))
                {
                    //Console.WriteLine("Fallback to UTF-8 encoding");
                }
            }
            // Fallback to default encoding
            if (html == null)
            {
                html = Encoding.Default.GetString(contentBytes);
                //Console.WriteLine("Using default encoding");
            }

            // Clean up malformed HTML - fix the <tr></html> issue
            html = CleanMalformedHtml(html);
            //Console.WriteLine("Cleaned malformed HTML tags");

            //Console.WriteLine($"Retrieved {html.Length} characters of HTML content");
            return html;
        }
        static internal string GetCharsetFromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(contentType, @"charset=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
        static private bool TryDecode(byte[] bytes, string encodingName, out string result)
        {
            try
            {
                var encoding = Encoding.GetEncoding(encodingName);
                result = encoding.GetString(bytes);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
        static internal string CleanHtmlText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Decode HTML entities (including &nbsp;)
            text = HttpUtility.HtmlDecode(text);

            // Replace Unicode left and right single quotation marks with a single quote
            text = text.Replace('\u2018', '\'').Replace('\u2019', '\'');

            // Replace multiple whitespace characters with single spaces
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            // Trim and return
            return text.Trim();
        }
        static internal string CleanMalformedHtml(string html)
        {
            // Fix the main issue: <tr></html> should be <tr></tr> or just removed
            html = System.Text.RegularExpressions.Regex.Replace(html,
                @"<tr>\s*</html>",
                "<tr></tr>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Also handle cases where </html> appears after </tr>
            html = System.Text.RegularExpressions.Regex.Replace(html,
                @"</tr>\s*<tr>\s*</html>",
                "</tr>\n<tr></tr>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove any stray </html> tags that appear within table content
            html = System.Text.RegularExpressions.Regex.Replace(html,
                @"</html>(?=\s*<(?:tr|td|table))",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return html;
        }
        static internal string GetUri(HtmlNode aTag, string url)
        {
            if (aTag == null)
            {
                Console.WriteLine("Warning: <a> tag is null, cannot extract href.");
                return null;
            }

            string href = aTag.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href))
            {
                Console.WriteLine("Warning: <a> tag does not have an href attribute.");
                return null;
            }

            // Convert relative URLs to absolute 
            if (href.StartsWith("/"))
            {
                Uri baseUri = new Uri(url);
                href = $"{baseUri.Scheme}://{baseUri.Host}{href}";
            }
            else if (!href.StartsWith("http"))
            {
                Uri baseUri = new Uri(url);
                href = new Uri(baseUri, href).ToString();
            }

            return CleanHtmlText(href);
        }
    }
}
