using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    public class BoxShadowsAnimator :  Animator<BoxShadows>
    {
        private static readonly BoxShadowAnimator s_boxShadowAnimator = new BoxShadowAnimator();
        public override BoxShadows Interpolate(double progress, BoxShadows oldValue, BoxShadows newValue)
        {
            int cnt = progress >= 1d ? newValue.Count : oldValue.Count;
            if (cnt == 0)
                return new BoxShadows();

            BoxShadow first;
            if (oldValue.Count > 0 && newValue.Count > 0)
                first = s_boxShadowAnimator.Interpolate(progress, oldValue[0], newValue[0]);
            else if (oldValue.Count > 0)
                first = oldValue[0];
            else
                first = newValue[0];

            if (cnt == 1)
                return new BoxShadows(first);

            var rest = new BoxShadow[cnt - 1];
            for (var c = 0; c < rest.Length; c++)
            {
                var idx = c + 1;
                if (oldValue.Count > idx && newValue.Count > idx)
                    rest[c] = s_boxShadowAnimator.Interpolate(progress, oldValue[idx], newValue[idx]);
                else if (oldValue.Count > idx)
                    rest[c] = oldValue[idx];
                else
                    rest[c] = newValue[idx];
            }

            return new BoxShadows(first, rest);
        }
    }
}
