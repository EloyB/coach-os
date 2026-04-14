namespace CoachOS.Infrastructure.Email;

public interface IMjmlTemplateRenderer
{
    /// <summary>
    /// Render a pre-compiled MJML template by name (file name without extension)
    /// with {{token}} placeholder substitution. Throws if the template was not loaded
    /// at startup (i.e. the .mjml file is missing or did not compile).
    /// </summary>
    string Render(string templateName, IReadOnlyDictionary<string, string> tokens);
}
