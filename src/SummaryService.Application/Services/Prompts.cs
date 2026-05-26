namespace SummaryService.Application.Services;

public static class Prompts
{
    private static readonly Dictionary<string, string> _prompts = new()
    {
        ["summarize"] =
            """
            Resume el siguiente texto de forma concisa.
            Responde siempre en español.

            Texto:
            {text}
            """,

        ["summarize-chunk"] =
            """
            Resume esta parte del documento de forma concisa.
            Responde siempre en español.

            Texto:
            {text}
            """,

        ["reduce"] =
            """
            Combina los siguientes resúmenes en un único resumen coherente.
            Responde siempre en español.

            Texto:
            {text}
            """,

        ["executive-summary"] =
            """
            Genera un resumen ejecutivo del siguiente contenido.
            Responde siempre en español.

            Texto:
            {text}
            """
    };

    public static string Get(string name)
    {
        if (_prompts.TryGetValue(name, out var prompt))
            return prompt;

        throw new KeyNotFoundException($"Prompt '{name}' not found");
    }
}
