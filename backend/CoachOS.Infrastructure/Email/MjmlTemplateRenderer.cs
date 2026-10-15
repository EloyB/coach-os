using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Mjml.Net;
using Mjml.Net.Helpers;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Compiles all .mjml files in Email/Templates/ to HTML once at construction
/// (singleton lifetime) and exposes fast Render(name, tokens) for runtime use.
/// </summary>
public class MjmlTemplateRenderer : IMjmlTemplateRenderer
{
    private readonly Dictionary<string, string> _compiled = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<MjmlTemplateRenderer> _logger;

    public MjmlTemplateRenderer(ILogger<MjmlTemplateRenderer> logger)
    {
        _logger = logger;

        var dir = Path.Combine(AppContext.BaseDirectory, "Email", "Templates");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"MJML template directory not found: {dir}");

        var renderer = new MjmlRenderer();
        // Geen standaardfonts: MJML voegt anders een Google Fonts-<link>/@import toe zodra
        // de font-stack een "bekend" font noemt (Roboto). Onze stack valt toch terug op
        // systeemfonts; een externe font-request kost enkel privacy en spamscore.
        MjmlOptions options = new()
        {
            Beautify = false,
            Fonts = new Dictionary<string, Font>(),
        };

        // Read shared head extras (dark-mode opt-out + force-light CSS) once and inject
        // into every template's <mj-head> so the rule lives in one place.
        var headExtrasPath = Path.Combine(dir, "_head-extras.mjml-partial");
        var headExtras = File.Exists(headExtrasPath) ? File.ReadAllText(headExtrasPath) : string.Empty;

        foreach (var file in Directory.EnumerateFiles(dir, "*.mjml"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith('_')) continue;

            var mjml = File.ReadAllText(file);
            if (!string.IsNullOrEmpty(headExtras))
                mjml = mjml.Replace("<mj-head>", "<mj-head>\n" + headExtras, StringComparison.Ordinal);

            var result = renderer.Render(mjml, options);

            if (result.Errors.Count > 0)
            {
                foreach (var err in result.Errors)
                    _logger.LogError("MJML compile error in {File}: {Error}", file, err);
                throw new InvalidOperationException(
                    $"MJML template '{name}' has {result.Errors.Count} compile error(s).");
            }

            _compiled[name] = result.Html;
            _logger.LogInformation("Compiled MJML template: {Name} ({Bytes} bytes)", name, result.Html.Length);
        }
    }

    public string Render(string templateName, IReadOnlyDictionary<string, string> tokens)
    {
        if (!_compiled.TryGetValue(templateName, out var html))
            throw new KeyNotFoundException($"MJML template '{templateName}' not loaded. Available: {string.Join(", ", _compiled.Keys)}");

        // HTML-encode elke token-value om XSS/HTML-injectie via student-invoer te blokkeren
        // (student names, form responses, trainer names, enz). Keys met prefix "raw:" worden
        // NIET geëncodeerd — gebruik alleen voor door de app gegenereerde HTML (tabelrijen e.d.).
        var sb = new StringBuilder(html);
        foreach (var kv in tokens)
        {
            if (kv.Key.StartsWith("raw:", StringComparison.Ordinal))
            {
                var realKey = kv.Key.Substring(4);
                sb.Replace("{{" + realKey + "}}", kv.Value);
            }
            else
            {
                sb.Replace("{{" + kv.Key + "}}", WebUtility.HtmlEncode(kv.Value));
            }
        }
        return sb.ToString();
    }
}
