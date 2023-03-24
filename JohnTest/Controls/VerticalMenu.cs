using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Styling;

namespace AppNs.Controls;

public class VerticalMenu : Menu , IStyleable
{
  private static readonly ITemplate<Panel> DefaultPanel =
    new FuncTemplate<Panel>(() => new StackPanel { Orientation = Orientation.Vertical });

  Type IStyleable.StyleKey => typeof(VerticalMenu);

  public VerticalMenu()
  {
  }

  public VerticalMenu(IMenuInteractionHandler interactionHandler) : base(interactionHandler)
  {
  }

  static VerticalMenu()
  {
    // хорошо, что в качестве ключа при поиске используется не IStyleable.StyleKey, а конечный тип (иначе бы не сработало при StyleKey => typeof(Menu))
    ItemsPanelProperty.OverrideDefaultValue(typeof(VerticalMenu), DefaultPanel);
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