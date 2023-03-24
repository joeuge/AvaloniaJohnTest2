using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Platform;
using Avalonia.Styling;

namespace AppNs.Controls;

public class ContextMenuEx : ContextMenu, IStyleable
{
  Type IStyleable.StyleKey => typeof(ContextMenu);

  public ContextMenuEx()
  {
  }

  public ContextMenuEx(IMenuInteractionHandler interactionHandler) : base(interactionHandler)
  {
  }

  /*
  protected override IItemContainerGenerator CreateItemContainerGenerator()
  {
    return new MenuItemContainerGeneratorEx(this);
  }
  */

  protected override Control CreateContainerForItemOverride() => new MenuItemEx();
  protected override bool IsItemItsOwnContainerOverride(Control item) => item is MenuItem or Separator;

  protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
  {
    base.PrepareContainerForItemOverride(container, item, index);
    MenuItemEx.PrepareContainerForItem(container as MenuItemEx, item);
  }

}