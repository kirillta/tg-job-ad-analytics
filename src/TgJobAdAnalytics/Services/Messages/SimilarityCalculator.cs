using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Messages;

namespace TgJobAdAnalytics.Services.Messages;

public sealed class SimilarityCalculator
{
    public SimilarityCalculator(IOptions<ParallelOptions> parallelOptions, IOptions<VectorizationOptions> vectorizationOptions)
    {
        _parallelOptions = parallelOptions.Value;
        _vectorizationOptions = vectorizationOptions.Value;
    }


    public List<AdEntity> Distinct(List<AdEntity> ads)
    {
        if (ads.Count == 0)
            return [];

        var (shingles, vocabulary) = GetShinglesAndVocabulary(ads);

        return DistinctInternal(ads, shingles);


        static HashSet<string> GetShingles(string text, int shingleSize)
        {
            if (text.Length < shingleSize)
                return Enumerable.Empty<string>().ToHashSet();

            var shingles = new HashSet<string>();
            for (int i = 0; i <= text.Length - shingleSize; i++)
                shingles.Add(text.Substring(i, shingleSize));

            return shingles;
        }


        (Dictionary<AdEntity, HashSet<string>>, List<string>) GetShinglesAndVocabulary(List<AdEntity> ads)
        {
            var vocabulary = new HashSet<string>();
            var adShingles = new Dictionary<AdEntity, HashSet<string>>();
            foreach (var ad in ads)
            {
                var shingles = GetShingles(ad.Text, _vectorizationOptions.ShingleSize);
                adShingles.Add(ad, shingles);
                vocabulary.UnionWith(shingles);
            }

            return (adShingles, vocabulary.ToList());
        }


        List<AdEntity> DistinctInternal(List<AdEntity> ads, Dictionary<AdEntity, HashSet<string>> shingles)
        {
            var minHashCalculator = new MinHashCalculator(_vectorizationOptions, vocabulary.Count);
            var lshCalculator = new LocalitySensitiveHashCalculator(_vectorizationOptions);

            var distinctAds = new ConcurrentBag<AdEntity>();
            Parallel.ForEach(ads, _parallelOptions, ad =>
            {
                var hash = minHashCalculator.GenerateSignature(shingles[ad]);

                var similarMessages = lshCalculator.GetMatches(hash);
                if (similarMessages.Count == 0)
                {
                    lshCalculator.Add(ad.Id, hash);
                    distinctAds.Add(ad);
                }
            });

            return [.. distinctAds];
        }
    }


    private readonly ParallelOptions _parallelOptions;
    private readonly VectorizationOptions _vectorizationOptions;
}
