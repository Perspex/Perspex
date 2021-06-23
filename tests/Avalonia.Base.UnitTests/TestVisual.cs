using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Base.UnitTests
{
    public class ParamEventArgs<T> : EventArgs
    {
        public ParamEventArgs(T param)
        {
            Param = param;
        }

        public T Param { get; set; }
    }

    public class TestVisual : Visual
    {
        public IVisual Child
        {
            get
            {
                return ((IVisual)this).VisualChildren.FirstOrDefault();
            }

            set
            {
                if (Child != null)
                {
                    VisualChildren.Remove(Child);
                }

                if (value != null)
                {
                    VisualChildren.Add(value);
                }
            }
        }

        public void AddChild(Visual v)
        {
            VisualChildren.Add(v);
        }

        public void AddChildren(IEnumerable<Visual> v)
        {
            VisualChildren.AddRange(v);
        }

        public void RemoveChild(Visual v)
        {
            VisualChildren.Remove(v);
        }

        public void ClearChildren()
        {
            VisualChildren.Clear();
        }
    }
}
