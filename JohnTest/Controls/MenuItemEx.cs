using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppNs.UiBlocks.ContextMenuNs;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Styling;

namespace AppNs.Controls;

public class MenuItemEx : MenuItem, IStyleable
{
  Type IStyleable.StyleKey => typeof(MenuItem);

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
    PrepareContainerForItem(container as MenuItemEx, item);
  }

  internal static void PrepareContainerForItem(MenuItemEx? menuItem, object? item)
  {
    if (menuItem == null)
    {
      return;
    }
    switch (item)
    {
      case SeparatorItem _:
        //return new Separator(); // approach 1
        menuItem.Header = "-"; // approach 2 // -> PseudoClasses.Add(":separator")
        //menuItem.Classes.Add("separator"); // approach 3
        break;

      case ItemBase itemBase:
        if (itemBase.IsBaseStyle) menuItem.Classes.Add("base");
        if (itemBase.IsScreenStyle) menuItem.Classes.Add("screen");
        if (itemBase.IsCheckableStyle)
        {
          menuItem.Classes.Add("checkable");
        }

        if (itemBase is CommandItem commandItem)
        {
          menuItem.Command = commandItem.Command;
          menuItem.Classes.Add("command");
        }
        if (itemBase is GroupItem groupItem)
        {
          menuItem.Items = groupItem.Items;
          menuItem.Classes.Add("group");
        }
        break;
    }
  }

}