#r "nuget: HtmlAgilityPack, 1.11.61"

#nullable enable

using HtmlAgilityPack;

string repoName = Args[0];
string htmlPath = Args[1];

Console.WriteLine($"repoName: {repoName}");
Console.WriteLine($"htmlPath: {htmlPath}");
Console.WriteLine($"CommandLine: {Environment.CommandLine}");
Console.WriteLine($"CurrentDirectory: {Environment.CurrentDirectory}");

var baseUri = new Uri(repoName, UriKind.RelativeOrAbsolute);
var document = new HtmlDocument() { BackwardCompatibility = false };
document.Load(htmlPath);
	
// Rewrite urls
RewriteTagAttributeUrls("script", "src");  // Rewrite `src` attribute of `<script>` tag.
RewriteTagAttributeUrls("link", "href");   // Rewrite `href` attribute of `<link>` tag.
RewriteTagAttributeUrls("a", "href");      // Rewrite `href` attribute of `<a>` tag.
RewriteTagAttributeUrls("img", "src");     // Rewrite `src` attribute of `<img>` tag.
RewriteTagAttributeUrls("meta", "content", DocfxMetaTagFilter); // Rewrite `content` attrivute of `<meta>` tags.
	
document.Save(htmlPath);

// Helper function to rewrite tag attributes.
void RewriteTagAttributeUrls(
    string tagName, 
    string attributeName,
    Func<HtmlNode, bool>? filter = null)
{
    var nodes = document.DocumentNode.SelectNodes($"//{tagName}[@{attributeName}]") ?? Enumerable.Empty<HtmlNode>();

    if (filter != null)
        nodes = nodes.Where(filter);

    foreach (HtmlNode node in nodes)
    {
        var attr = node.Attributes[attributeName];
        if (string.IsNullOrEmpty(attr.Value))
            continue;
        attr.Value = ResolveUrl(attr.Value);
    }
}

string ResolveUrl(string targetUrl)
{
    if (!Uri.TryCreate(targetUrl, UriKind.RelativeOrAbsolute, out var resultUri))
        return targetUrl;

    if (resultUri.IsAbsoluteUri)
        return targetUrl;

    if (baseUri.IsAbsoluteUri)
        return new Uri(baseUri, targetUrl).AbsoluteUri;

    // Otherwise return `Site Relative URL`.
    var result = new Uri(baseUri: new Uri($"http://localhost/{baseUri}/"), resultUri);
    return result.PathAndQuery;
}

static bool DocfxMetaTagFilter(HtmlNode node)
{
    var key = node.GetAttributeValue<string>("name", "");
    key = node.GetAttributeValue<string>("property", key); // Used by `default` template. 

    return key switch
    {
        "docfx:navrel" => true,
        "docfx:tocrel" => true,
        "docfx:rel" => true,
        _ => false,
    };
}
