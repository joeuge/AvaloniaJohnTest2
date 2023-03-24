using System.Reactive;
using System.Windows.Input;
using Caliburn.Micro;
using AppNs.CoreNs;
using AppNs.Interfaces;
using AppNs.UiBlocks.ExtraWindows;
using Iface.Utils;
using ReactiveUI;

namespace AppNs.UiContent.Pages.Dummy;

[Page(
  FactoryId = FactoryIds.DummyPage,
  ContractType = typeof(DummyPage),
  FactoryName = "Dummy Page"
)]

[TransientInstance]

public class DummyPage : Page
{
  private readonly IShell _shell;
  private readonly IInfrastructure _infr;

  public ReactiveCommand<Unit, Unit> ToggleCommand { get; }
  public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
  public ICommand TopmostCommand { get; }

  static int _cnt = 0;
  public int TmpId { get; }

  public DummyPage()
  {
    if (!Execute.InDesignMode)
      throw new InvalidOperationException();
  }

  public DummyPage(IShell shell, IInfrastructure infr)
  {
    TmpId = ++_cnt;
    _shell = shell;
    _infr = infr;
    DisplayName = $"D:{TmpId}";

    ToggleCommand = ReactiveCommand.CreateFromTask(ToggleCommandImpl, null, RxApp.MainThreadScheduler);
    ConfirmCommand = ReactiveCommand.CreateFromTask(ConfirmCommandImpl, null, RxApp.MainThreadScheduler);

    TopmostCommand = new SimpleCommand(p =>
    {
      var workspaceHolder = TryGetWorkspaceHolder();
      var windowController = workspaceHolder.TryGetWorkspaceWindow();
      if (windowController == null) return;
      var window = windowController.Window as ExtraWindow;
      window.Topmost = false;
      window.Topmost = true;
      window.Topmost = false;
    });
  }

  protected override void OnBaseCreated()
  {
    base.OnBaseCreated();
    OverrideViewType(typeof(DummyPageView));
  }

  private async Task ToggleCommandImpl()
  {
    var workspaceHolder = TryGetWorkspaceHolder();
    await _shell.ToggleWorkspaceHolder(workspaceHolder);
  }

  private async Task ConfirmCommandImpl()
  {
    var modalService = GetGlobalModalService();
    if (modalService == null) return;
    var success = await modalService.ConfirmAsync();
  }

}
