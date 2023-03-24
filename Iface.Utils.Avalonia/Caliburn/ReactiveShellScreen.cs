using Caliburn.Micro;
using ReactiveUI;

namespace Caliburn.Micro
{

    public class ReactiveShellScreen : Screen, ReactiveUI.IScreen
    {
        public RoutingState Router { get; } = new RoutingState();

    }
}
