using System.Collections.Generic;
using Avalonia.Markup.Xaml.HotReload.Blocks;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal class DiffScoreCache
    {
        private readonly Dictionary<(ObjectBlock, ObjectBlock), double> _blockScores;
        private readonly Dictionary<(PropertyBlock, PropertyBlock), double> _propertyScores;

        public IReadOnlyDictionary<(ObjectBlock, ObjectBlock), double> BlockScores => _blockScores;
        public IReadOnlyDictionary<(PropertyBlock, PropertyBlock), double> PropertyScores => _propertyScores;

        public DiffScoreCache()
        {
            _blockScores = new Dictionary<(ObjectBlock, ObjectBlock), double>();
            _propertyScores = new Dictionary<(PropertyBlock, PropertyBlock), double>();
        }

        public void Add(ObjectBlock first, ObjectBlock second, double score)
        {
            _blockScores[(first, second)] = score;
            _blockScores[(second, first)] = score;
        }

        public void Add(PropertyBlock first, PropertyBlock second, double score)
        {
            _propertyScores[(first, second)] = score;
            _propertyScores[(second, first)] = score;
        }

        public bool TryGetScore(ObjectBlock first, ObjectBlock second, out double score)
        {
            return _blockScores.TryGetValue((first, second), out score);
        }

        public bool TryGetScore(PropertyBlock first, PropertyBlock second, out double score)
        {
            return _propertyScores.TryGetValue((first, second), out score);
        }
    }
}
