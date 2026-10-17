using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TutorialLibrary;

public static partial class ThumbnailService
{
    static readonly HttpClient Client = CreateClient();

    static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.All });
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Fuwei/1.8");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
        return client;
    }

    public static async Task<byte[]?> FindCoverAsync(string pageUrl, CancellationToken cancellationToken = default)
    {
        if (!DataStore.IsWebUrl(pageUrl)) return null;
        Uri page = new(pageUrl);
        if (GetYouTubeThumbnailUrl(page) is Uri youtubeImage)
        {
            try
            {
                using var thumbnailResponse = await Client.GetAsync(youtubeImage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (thumbnailResponse.IsSuccessStatusCode)
                {
                    byte[] thumbnail = await ReadLimitedAsync(thumbnailResponse.Content, 6 * 1024 * 1024, cancellationToken);
                    using var input = new MemoryStream(thumbnail);
                    using var verifiedImage = System.Drawing.Image.FromStream(input, true, true);
                    return thumbnail;
                }
            }
            catch (HttpRequestException) { /* Try the page metadata below. */ }
            catch (InvalidDataException) { /* Try the page metadata below. */ }
            catch (ArgumentException) { /* Try the page metadata below. */ }
        }
        using var response = await Client.GetAsync(page, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        if (response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            return await ReadLimitedAsync(response.Content, 6 * 1024 * 1024, cancellationToken);

        byte[] htmlBytes = await ReadLimitedAsync(response.Content, 2 * 1024 * 1024, cancellationToken);
        string html = Encoding.UTF8.GetString(htmlBytes);
        Uri? imageUrl = FindImageUrl(page, html);
        if (imageUrl is null) return null;
        using var imageResponse = await Client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!imageResponse.IsSuccessStatusCode) return null;
        byte[] bytes = await ReadLimitedAsync(imageResponse.Content, 6 * 1024 * 1024, cancellationToken);
        using var memory = new MemoryStream(bytes);
        using var image = System.Drawing.Image.FromStream(memory, true, true);
        return bytes;
    }

    public static Uri? GetYouTubeThumbnailUrl(Uri page)
    {
        string host = page.IdnHost.ToLowerInvariant();
        bool shortLink = host is "youtu.be" or "www.youtu.be";
        bool youtube = host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "music.youtube.com" or "youtube-nocookie.com" or "www.youtube-nocookie.com";
        if (!shortLink && !youtube) return null;

        string? id = null;
        if (shortLink) id = page.AbsolutePath.Trim('/').Split('/')[0];
        else if (page.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string field in page.Query.TrimStart('?').Split('&'))
            {
                string[] pair = field.Split('=', 2);
                if (pair.Length == 2 && pair[0] == "v") { id = Uri.UnescapeDataString(pair[1]); break; }
            }
        }
        else
        {
            string[] parts = page.AbsolutePath.Trim('/').Split('/');
            if (parts.Length >= 2 && parts[0] is "shorts" or "live" or "embed" or "v") id = parts[1];
        }
        if (id is null || id.Length != 11 || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '_' and not '-')) return null;
        return new Uri($"https://i.ytimg.com/vi/{id}/hqdefault.jpg");
    }

    public static Uri? FindImageUrl(Uri page, string html)
    {
        foreach (Match tag in MetaTags().Matches(html))
        {
            string? key = null, content = null;
            foreach (Match attribute in Attributes().Matches(tag.Value))
            {
                string name = (attribute.Groups[1].Success ? attribute.Groups[1].Value : attribute.Groups[4].Value).ToLowerInvariant();
                string value = WebUtility.HtmlDecode(attribute.Groups[3].Success ? attribute.Groups[3].Value : attribute.Groups[5].Value);
                if (name is "property" or "name") key = value.ToLowerInvariant();
                if (name == "content") content = value;
            }
            if (key is "og:image" or "og:image:url" or "twitter:image" or "twitter:image:src")
            {
                if (Uri.TryCreate(page, content, out var image) && DataStore.IsWebUrl(image.ToString())) return image;
            }
        }
        return null;
    }

    static async Task<byte[]> ReadLimitedAsync(HttpContent content, int maxBytes, CancellationToken token)
    {
        if (content.Headers.ContentLength > maxBytes) throw new InvalidDataException("网页或封面图片过大。 ");
        await using var source = await content.ReadAsStreamAsync(token);
        using var memory = new MemoryStream();
        byte[] buffer = new byte[16 * 1024];
        while (true)
        {
            int read = await source.ReadAsync(buffer, token);
            if (read == 0) break;
            if (memory.Length + read > maxBytes) throw new InvalidDataException("网页或封面图片过大。 ");
            memory.Write(buffer, 0, read);
        }
        return memory.ToArray();
    }

    [GeneratedRegex("<meta\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MetaTags();

    [GeneratedRegex("([\\w:-]+)\\s*=\\s*([\"'])(.*?)\\2|([\\w:-]+)\\s*=\\s*([^\\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Attributes();
}
