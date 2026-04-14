using System.Text;
using Microsoft.Extensions.Logging;
using Mjml.Net;

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
        var options = new MjmlOptions { Beautify = false };

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

        var sb = new StringBuilder(html);
        foreach (var kv in tokens)
            sb.Replace("{{" + kv.Key + "}}", kv.Value);
        return sb.ToString();
    }
}
