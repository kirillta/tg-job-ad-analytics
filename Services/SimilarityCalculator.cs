using System;
namespace TgJobAdAnalytics.Services;

public sealed class SimilarityCalculator
{
    public static Dictionary<string, int> GetTermFrequency(ReadOnlySpan<char> text)
    {
        var termFrequency = new Dictionary<string, int>();
        foreach (var word in text.Split(' '))
        {
            var wordStr = word.ToString();
            if (termFrequency.TryGetValue(wordStr, out int value))
                termFrequency[wordStr] = ++value;
            else
                termFrequency[wordStr] = 1;
        }

        return termFrequency;
    }


    public static Dictionary<string, double> GetInverseDocumentFrequency(List<Dictionary<string, int>> termFrequencies)
    {
        var documentFrequency = GetDocumentFrequency(termFrequencies);

        var inverseDocumentFrequency = new Dictionary<string, double>();
        foreach (var term in documentFrequency.Keys)
            inverseDocumentFrequency[term] = Math.Log((double)termFrequencies.Count / documentFrequency[term]);

        return inverseDocumentFrequency;
    }


    public static Dictionary<string, double> GetTfIdf(Dictionary<string, int> termFrequency, Dictionary<string, double> inverseDocumentFrequency)
    {
        var tfIdf = new Dictionary<string, double>();
        foreach (var term in termFrequency.Keys)
            tfIdf[term] = termFrequency[term] * inverseDocumentFrequency[term];

        return tfIdf;
    }


    private static Dictionary<string, int> GetDocumentFrequency(List<Dictionary<string, int>> termFrequencies)
    {
        var documentFrequency = new Dictionary<string, int>();
        foreach (var tf in termFrequencies)
        {
            foreach (var term in tf.Keys)
            {
                if (documentFrequency.TryGetValue(term, out int value))
                    documentFrequency[term] = ++value;
                else
                    documentFrequency[term] = 1;
            }
        }

        return documentFrequency;
    }
}
