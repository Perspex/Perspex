using Avalonia.Markup.Xaml.HotReload.Blocks;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal class BlockPair
    {
        public ObjectBlock Left { get; }
        public ObjectBlock Right { get; }

        public double Score { get; }

        public BlockPair(ObjectBlock left, ObjectBlock right, double score)
        {
            Left = left;
            Right = right;
            Score = score;
        }
    }
}
