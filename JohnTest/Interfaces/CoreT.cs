using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Iface.Utils;

namespace AppNs.Interfaces;

//public delegate void WidgetEventHandler(object sender, WidgetEventArgs e);


[Flags]
public enum Sides
{
  None = 0,
  Left = 1,
  Top = 2,
  Right = 4,
  Bottom = 8,
  All = Left | Top | Right | Bottom,
}


public enum ScreenOwnerType // на момент документирования прикладное значение имели: None, Window
{
  None,
  Unknown,
  Window,
  Bar,
  OverlayContainer,
  //TilesContainer,
  DocumentDecorator,
  SidePanelManager,
  SideScreen,
  ModalScreen,
  ShellPopupManager,
  OverviewContainer
}
public enum DialogGenesis
{
  Unknown,
  GlobalModal,
  LocalModal,
  LocalUnmodal,
}

public enum DialogHeaderType : short
{
  Undefined,
  NoHeader,
  NoHeaderAndOverlay,
  NormalHeader,
  ThinHeader,
  ThinHeaderAndOverlay,
}

public struct DialogOptions
{
  public DialogHeaderType HeaderType;
  public bool IsSmallCloseButton;
  public bool IsOverlayRightMargin;

  public bool UsesCloseButton;
  public bool CanFullSize;
  public bool InitialSateIsFullSize; // Используется только в ModalScreen
  public bool CloseWhenEscape; // Close Dialog, dialogResult = false
  public bool CloseWhenEnter;  // Close Dialog, dialogResult = true
  public bool CloseWhenClickOutside; // ModalScreen: CloseDialogs // в Global Modal не работает

  public bool UsesHeader => HeaderType == DialogHeaderType.NormalHeader
                            || HeaderType == DialogHeaderType.ThinHeader
                            || HeaderType == DialogHeaderType.ThinHeaderAndOverlay;
  public bool IsToolsOver => HeaderType == DialogHeaderType.NoHeaderAndOverlay
                             || HeaderType == DialogHeaderType.ThinHeaderAndOverlay;

  public static DialogOptions Default => new DialogOptions
  {
    HeaderType = DialogHeaderType.NormalHeader,
    IsSmallCloseButton = true,
    UsesCloseButton = true,
    CanFullSize = false, // по умолчанию отключено
    CloseWhenEscape = true,
    CloseWhenEnter = false, // по умолчанию отключено
    CloseWhenClickOutside = true,
    IsOverlayRightMargin = true,
  };

  public DialogOptions(DialogHeaderType headerType, bool canFullSize = false)
  {
    HeaderType = headerType;
    IsSmallCloseButton = true;
    UsesCloseButton = true;
    CanFullSize = canFullSize;
    InitialSateIsFullSize = false;
    CloseWhenEscape = true;
    CloseWhenEnter = false;
    CloseWhenClickOutside = true;
    IsOverlayRightMargin = true;
    Coerce();
  }

  public void Coerce()
  {
    switch (HeaderType)
    {
      case DialogHeaderType.Undefined:
        HeaderType = DialogHeaderType.NoHeader;
        break;
      case DialogHeaderType.NoHeader:
        break;
      case DialogHeaderType.NormalHeader:
        break;
      case DialogHeaderType.ThinHeader:
        IsSmallCloseButton = true;
        break;
      case DialogHeaderType.NoHeaderAndOverlay:
        IsSmallCloseButton = false;
        break;
      case DialogHeaderType.ThinHeaderAndOverlay:
        IsSmallCloseButton = true;
        break;
    }
  }

}




public enum DataObjectEditMethod
{
  Undefined,
  Custom,
  PropertyGrid,
  DataForm
}

[Flags]
public enum ScreenCustomizationPoint
{
  Unknown = 0,
  Loader = 1, // При загрузке из XML doc.Customize(point) вызывается дважды: 1) point=Factory 2) point=Loader
  Factory = 2,
  IsUiBlocked = 4
}

[Flags]
public enum DataIdUsageFlags
{
  //None = 0,
  Use = 1, // отсутствие флага = DataId не используется
  ReadOnly = 2
}

public enum KeyInputMethod
{
  None = 0,
  Default = 1,
  TmaStatus = 2,
  TmaAnalog = 3,
  TmaAccum = 4,
  SchemeDocId = 5, // уже не используется
  DefaultWithChoicesDialog = 6,
  DefaultWithChoicesComboBox = 7,
  ReportDocId = 8, // уже не используется
  TmEventMonitorId = 9,
  SvgDocId = 10, // уже не используется
  DocumentId = 11,
  BookmarkId = 12,
  TmTagListId = 13,
  UiProfileDocId = 14,
}


public enum WorkspaceOwnerType
{
  None,
  ShellTabs,
  ShellWindows,
  ShellSplit,
}

public enum WorkspaceType
{
  None,
  Canvas,
  //Tiles
}

public enum WorkspaceHolderLockSeverity
{
  None, // нет ограничений
  // ограничения на изменение:
  WorkspaceInstance, // holder->Workspace
  BedInstance, // + workspace->Bed
  Overlays // + workspace Overlays // widgets
}

public enum ScreenRole
{
  None,
  Page,
  Overlay,
  BarItem,
  //Tile,
  FreeRole
}

[Flags]
public enum ScreenRoles
{
  None = 0,
  All = -1,

  Page = 1,
  FreeRole = 16,
}


public enum DisplayNameType
{
  Identity,
  IdentityAndContent,
  Content
}


public enum UserAction
{
  Undefined,
  CloseSinglePage, // закрытие одиночной вкладки/окна
}


public enum ChangeOverlays
{
  None,
  Clear,
  Custom
}

public class WorkspacePreferences
{
  public static WorkspacePreferences TailTab { get; } = new WorkspacePreferences();
  public static WorkspacePreferences NextToActiveTab { get; } = new WorkspacePreferences { IsAddNextToActive = true };

  public bool IsWindow { get; protected set; }
  public bool IsAddNextToActive { get; protected set; }
  public int AddWithIndex { get; protected set; } = -1;

  // Window Settings
  //public bool IsApplyWindowSettings { get; set; }
  public WindowStartupLocation WindowStartupLocation { get; set; }
  public WindowState WindowState { get; set; }
  public double Top { get; set; }
  public double Left { get; set; }
  public double Width { get; set; }
  public double Height { get; set; }

  // for show in shell split layout
  public bool IsSplit { get; protected set; } // added 27 may 2021
  public bool AllowReplaceSplit { get; set; }
  public Sides Side { get; set; } = Sides.None;
  public double BandWidth { get; set; } = double.NaN;
}

public class CustomWorkspacePreferences : WorkspacePreferences
{
  public static CustomWorkspacePreferences ForWindow()
  {
    return new CustomWorkspacePreferences(true);
  }

  public static CustomWorkspacePreferences ForSplit()
  {
    return new CustomWorkspacePreferences(false) { IsSplit = true };
  }

  public static CustomWorkspacePreferences ForTab(bool isAddNextToActive = false, int addWithIndex = -1)
  {
    return new CustomWorkspacePreferences(false, isAddNextToActive, addWithIndex);
  }

  public CustomWorkspacePreferences(bool isWindow = false, bool isAddNextToActive = false, int addWithIndex = -1)
  {
    IsWindow = isWindow;
    IsAddNextToActive = isAddNextToActive;
    AddWithIndex = addWithIndex;
    if (!isWindow) return;
    WindowStartupLocation = WindowStartupLocation.Manual;
    WindowState = WindowState.Normal;
  }

  public CustomWorkspacePreferences(Sides side, double bandWidth, bool allowReplaceSplit)
  {
    IsSplit = true;
    Side = side;
    BandWidth = bandWidth;
    AllowReplaceSplit = allowReplaceSplit;
  }

}

