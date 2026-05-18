using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Llm;

public sealed class PromptProvider : IPromptProvider
{
    private readonly Dictionary<string, string> _prompts = new()
    {
        ["summarize"] = "Summarize the following text concisely:\n\n{text}",
        ["summarize-chunk"] = "Summarize this portion of a document concisely:\n\n{text}",
        ["reduce"] = "Combine these summaries into one coherent summary:\n\n{text}",
        ["executive-summary"] = "Provide an executive summary of the following:\n\n{text}"
    };

    public string GetPrompt(string name)
    {
        if (_prompts.TryGetValue(name, out var prompt))
            return prompt;

        throw new KeyNotFoundException($"Prompt '{name}' not found");
    }
}
