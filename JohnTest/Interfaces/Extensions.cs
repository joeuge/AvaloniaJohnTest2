

namespace AppNs.Interfaces;

//==================================
public static class EnumExtensions
{
  public static bool UseExists(this DataIdUsageFlags flags) { return (flags & DataIdUsageFlags.Use) != 0; }
  public static bool ReadOnlyExists(this DataIdUsageFlags flags) { return (flags & DataIdUsageFlags.ReadOnly) != 0; }

  public static ScreenRoles ToRoles(this ScreenRole role)
  {
    switch (role)
    {
      case ScreenRole.Page:
        return ScreenRoles.Page;
    }
    return 0;
  }

  public static ScreenRole ToRole(this ScreenOwnerType ownerType)
  {
    switch (ownerType)
    {
      case ScreenOwnerType.OverlayContainer:
        return ScreenRole.Overlay;

      case ScreenOwnerType.Bar:
        return ScreenRole.BarItem;

      case ScreenOwnerType.DocumentDecorator:
      case ScreenOwnerType.SidePanelManager:
      case ScreenOwnerType.SideScreen:
      case ScreenOwnerType.ModalScreen:
        return ScreenRole.Page;
    }
    return ScreenRole.None;
  }

  public static IEnumerable<ScreenRole> Roles(this ScreenRoles roles)
  {
    if ((roles & ScreenRoles.Page) != 0)
      yield return ScreenRole.Page;

    if ((roles & ScreenRoles.FreeRole) != 0)
      yield return ScreenRole.FreeRole;
  }

}