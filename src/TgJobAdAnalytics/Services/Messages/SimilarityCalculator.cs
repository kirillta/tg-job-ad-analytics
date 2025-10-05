using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Vectors;
using TgJobAdAnalytics.Services.Vectors;

namespace TgJobAdAnalytics.Services.Messages;

public sealed class SimilarityCalculator
{
    public SimilarityCalculator(
        IOptions<ParallelOptions> parallelOptions,
        IOptions<VectorizationOptions> vectorizationOptions,
        MinHashVectorizer? vectorizer = null,
        VectorIndex? vectorIndex = null,
        VectorStore? vectorStore = null,
        OptionVectorizationConfig? vectorizationConfig = null)
    {
        _parallelOptions = parallelOptions.Value;
        _vectorizationOptions = vectorizationOptions.Value;

        _vectorizer = vectorizer;
        _vectorIndex = vectorIndex;
        _vectorStore = vectorStore;
        _vectorizationConfig = vectorizationConfig;
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
            var minHashCalculator = new MinHashCalculator(_vectorizationOptions, _vectorizationOptions.VocabularySize);
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


    public async Task<List<AdEntity>> DistinctPersistent(List<AdEntity> ads, CancellationToken ct = default)
    {
        if (_vectorizer is null || _vectorIndex is null || _vectorStore is null)
            return Distinct(ads);

        if (ads.Count == 0)
            return [];

        var version = _vectorizationConfig?.GetActive().Version ?? _vectorizationOptions.CurrentVersion;
        var distinctAds = new ConcurrentBag<AdEntity>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _parallelOptions.MaxDegreeOfParallelism,
            TaskScheduler = _parallelOptions.TaskScheduler,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(ads, options, async (ad, token) =>
        {
            var (signature, shingleCount) = _vectorizer.Compute(ad.Text);

            var candidates = await _vectorIndex.Query(signature, token);
            bool isDuplicate = false;
            if (candidates.Count > 0)
            {
                foreach (var candidateId in candidates)
                {
                    var candidateVector = await _vectorStore.Get(candidateId, version: version, token);
                    if (candidateVector is null) continue;

                    var candidateSig = SignatureSerializer.FromBytes(candidateVector.Signature);
                    var score = SimilarityService.EstimatedJaccard(signature, candidateSig);
                    if (score >= DuplicateThreshold)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
            }

            if (!isDuplicate)
            {
                await _vectorStore.Upsert(ad.Id, signature, shingleCount, ad.UpdatedAt, token);
                await _vectorIndex.Upsert(ad.Id, signature, ad.UpdatedAt, token);
                distinctAds.Add(ad);
            }
        });

        return [.. distinctAds];
    }


    private const double DuplicateThreshold = 0.92;

    private readonly ParallelOptions _parallelOptions;
    private readonly VectorizationOptions _vectorizationOptions;

    private readonly MinHashVectorizer? _vectorizer;
    private readonly VectorIndex? _vectorIndex;
    private readonly VectorStore? _vectorStore;
    private readonly OptionVectorizationConfig? _vectorizationConfig;
}
