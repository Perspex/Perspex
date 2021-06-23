using System;
using System.Threading.Tasks;
using Avalonia.Rendering;
using Avalonia.Threading;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering
{
    public class RenderLoopTests
    {
        [Fact]
        public void RenderLoop_Update_Runs_On_Dispatcher()
        {
            var dispatcher = new Mock<IDispatcher>();

            bool inDispatcher = false;

            dispatcher.Setup(
                d => d.Post(It.IsAny<Action>(), DispatcherPriority.Render))
                .Callback((Action a, DispatcherPriority _) =>
                {
                    inDispatcher = true;
                    a();
                    inDispatcher = false;
                });

            var timer = new Mock<IRenderTimer>();

            var loop = new RenderLoop(timer.Object, dispatcher.Object);

            var renderTask = new Mock<IRenderLoopTask>();

            renderTask.Setup(t => t.NeedsUpdate).Returns(true);
            renderTask.Setup(t => t.Update(It.IsAny<TimeSpan>()))
                .Callback((TimeSpan _) => Assert.True(inDispatcher));

            loop.Add(renderTask.Object);

            timer.Raise(t => t.Tick += null, TimeSpan.Zero);

            renderTask.Verify(t => t.Update(It.IsAny<TimeSpan>()), Times.Once());
        }

        [Fact]
        public void RenderLoop_Does_Not_Update_When_No_Tasks_Need_Update()
        {
            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(
                d => d.InvokeAsync(It.IsAny<Action>(), DispatcherPriority.Render))
                .Callback((Action a, DispatcherPriority _) => a())
                .Returns(Task.CompletedTask);

            var timer = new Mock<IRenderTimer>();
            var loop = new RenderLoop(timer.Object, dispatcher.Object);
            var renderTask = new Mock<IRenderLoopTask>();
            renderTask.Setup(t => t.NeedsUpdate).Returns(false);

            loop.Add(renderTask.Object);
            timer.Raise(t => t.Tick += null, TimeSpan.Zero);
            
            renderTask.Verify(t => t.Update(It.IsAny<TimeSpan>()), Times.Never());
        }

        [Fact]
        public void RenderLoop_Render_Runs_Off_Dispatcher()
        {
            var dispatcher = new Mock<IDispatcher>();
            bool inDispatcher = false;
            dispatcher.Setup(
                d => d.Post(It.IsAny<Action>(), DispatcherPriority.Render))
                .Callback((Action a, DispatcherPriority _) =>
                {
                    inDispatcher = true;
                    a();
                    inDispatcher = false;
                });

            var timer = new Mock<IRenderTimer>();
            var loop = new RenderLoop(timer.Object, dispatcher.Object);

            var renderTask = new Mock<IRenderLoopTask>();

            renderTask.Setup(t => t.NeedsUpdate).Returns(true);
            renderTask.Setup(t => t.Render())
                .Callback(() => Assert.False(inDispatcher));

            loop.Add(renderTask.Object);
            timer.Raise(t => t.Tick += null, TimeSpan.Zero);

            renderTask.Verify(t => t.Update(It.IsAny<TimeSpan>()), Times.Once());
        }
        
        [Fact]
        public void RenderLoop_Passes_Tick_Count_To_Update()
        {
            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(
                d => d.Post(It.IsAny<Action>(), DispatcherPriority.Render))
                .Callback((Action a, DispatcherPriority _) => a());

            var timer = new Mock<IRenderTimer>();
            var loop = new RenderLoop(timer.Object, dispatcher.Object);
            var renderTask = new Mock<IRenderLoopTask>();
            renderTask.Setup(t => t.NeedsUpdate).Returns(true);

            loop.Add(renderTask.Object);
            var time = new TimeSpan(123456789L);
            timer.Raise(t => t.Tick += null, time);

            renderTask.Verify(t => t.Update(time), Times.Once());
        }
    }
}
