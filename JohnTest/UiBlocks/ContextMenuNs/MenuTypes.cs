using System.Windows.Input;
using Avalonia;
using Caliburn.Micro;
using AppNs.Interfaces;
using Iface.Utils;
using Iface.Utils.Avalonia;

namespace AppNs.UiBlocks.ContextMenuNs
{
  public enum ItemType
  {
    Unknown, Separator, Group, Command, Screen, Checkable
  }

  public enum DesignModeType
  {
    None, WorkspaceWidgets, Bar, PageWidgets
  }

  public static class ItemIds
  {
    public const int Id1 = 1; //?
  }

  public class CollectorContext
  {
    public IWorkspaceHolder WorkspaceHolder { get; }
    public IWorkspace Workspace { get; }
    public IPage Bed { get; }
    public object OriginalSource { get; }
    public Point MousePosition { get; }
    public List<ItemBase> ContainerItems { get; } // from {Workspace | Bar} itself
    public List<ItemBase> BedItems { get; private set; } // from CanvasWorkspace.Bed
    public List<ItemBase> TailItems { get; private set; }

    public CollectorContext(IWorkspaceHolder workspaceHolder, object originalSource, Point mousePosition) : this()
    {
      WorkspaceHolder = workspaceHolder;
      Workspace = WorkspaceHolder.Workspace;
      OriginalSource = originalSource;
      MousePosition = mousePosition;
      Bed = Workspace.Bed;
    }

    private CollectorContext()
    {
      ContainerItems = new List<ItemBase>();
    }

    public List<ItemBase> EnsureBedItems()
    {
      return BedItems ?? (BedItems = new List<ItemBase>());
    }

    public List<ItemBase> EnsureTailItems()
    {
      return TailItems ?? (TailItems = new List<ItemBase>());
    }

    public List<ItemBase> GetAllItems()
    {
      var list = new List<ItemBase>();

      list.AddRange(ContainerItems);

      if (BedItems != null && BedItems.Count > 0)
      {
        if (list.Count > 0)
          list.Add(new SeparatorItem());
        list.AddRange(BedItems);
      }
      if (TailItems != null && TailItems.Count > 0)
      {
        if (list.Count > 0)
          list.Add(new SeparatorItem());
        list.AddRange(TailItems);
      }
      return list.Count != 0 ? list : null;
    }

    public bool IsEmpty()
    {
      if (ContainerItems.Count != 0)
        return false;

      if (BedItems != null && BedItems.Count != 0)
        return false;

      if (TailItems != null && TailItems.Count != 0)
        return false;

      return true;
    }
  }

  //-----------------------------------------
  public abstract class ItemBase : ModelBase
  {
    public abstract ItemType ItemType { get; }
    public bool IsSeparator => ItemType == ItemType.Separator;
    public virtual bool IsBaseStyle => false;
    public virtual bool IsScreenStyle => false;
    public virtual bool IsIconStyle => false;
    public virtual bool IsGlyphIfaceStyle => false;
    public virtual bool IsGlyphTelerikStyle => false;
    public virtual bool IsCommandStyle => false;
    public virtual bool IsGroupStyle => false;
    public virtual bool IsCheckableStyle => false;
    public int CustomId { get; set; }
    public virtual void CustomUpdate() { }

  }

  //-----------------------------------------
  public class SeparatorItem : ItemBase
  {
    public override ItemType ItemType => ItemType.Separator;
  }

  //-----------------------------------------
  public abstract class PresentationItem : ItemBase
  {
    public override bool IsBaseStyle => true;
    public override bool IsIconStyle => IsIcon;
    public override bool IsGlyphIfaceStyle => IsGlyph && IsIfaceFontFamily;
    public override bool IsGlyphTelerikStyle => IsGlyph && !IsIfaceFontFamily;

    private bool _isEnabled = true;
    public bool IsEnabled { get => _isEnabled; set => SetPropertyValue(ref _isEnabled, value); }

    private string _displayName;
    public string DisplayName { get => _displayName; set => SetPropertyValue(ref _displayName, value); }

    public bool IsIfaceFontFamily { get; set; }

    private char _glyph;
    public char Glyph { get => _glyph; set => SetPropertyValue(ref _glyph, value); }

    public char IfaceGlyph
    {
      set
      {
        IsIfaceFontFamily = true;
        Glyph = value;
      }
    }

    public char TelerikGlyph
    {
      set
      {
        IsIfaceFontFamily = false;
        Glyph = value;
      }
    }

    private double _glyphFontSize = 16d;
    public double GlyphFontSize { get => _glyphFontSize; set => this.SetPropertyValue(ref _glyphFontSize, value); }

    private Uri _iconUri;
    public Uri IconUri { get => _iconUri; set => SetPropertyValue(ref _iconUri, value); }

    public bool IsGlyph => Glyph != '\0';
    public bool IsIcon => !IsGlyph && IconUri != null;

    private bool _staysOpenOnClick;
    public bool StaysOpenOnClick { get => _staysOpenOnClick; set => SetPropertyValue(ref _staysOpenOnClick, value); }

    private string _toolTipContent;
    public string ToolTipContent { get => _toolTipContent; set => SetPropertyValue(ref _toolTipContent, value); }

  }

  //-----------------------------------------
  public class CommandItem : PresentationItem
  {
    public override ItemType ItemType => ItemType.Command;
    public override bool IsCommandStyle => true;

    public ICommand Command { get; set; }
    public object CommandParameter { get; set; }
  }

  //-----------------------------------------
  public class CheckableItem : ItemBase
  {
    public override ItemType ItemType => ItemType.Checkable;
    public override bool IsCheckableStyle => true;
    public RadioGroupItem RadioGroup { get; }

    public Func<bool, bool> CoerceInputHook { get; set; } // f(newValue) return: effective value
    public Action<bool> IsCheckedChangedHook { get; set; } // f(newValue)

    private string _displayName;
    public string DisplayName { get => _displayName; set => SetPropertyValue(ref _displayName, value); }

    private bool _isChecked;
    public bool IsChecked
    {
      get => _isChecked;
      set => SetIsChecked(value, ignoreRestrictions: false, skipCoercion: false);
    }

    public CheckableItem(bool isChecked = false, RadioGroupItem radioGroup = null)
    {
      _isChecked = isChecked;
      RadioGroup = radioGroup;
    }

    public void SetIsChecked(bool value, bool ignoreRestrictions = true, bool skipCoercion = false)
    {
      if (_isChecked == value) return;

      if (!ignoreRestrictions && RadioGroup != null && !value)
      {
        return;
      }

      if (!skipCoercion)
      {
        value = CoerceInput(value);
        if (_isChecked == value)
        {
          return;
        }
      }

      _isChecked = value;
      OnIsCheckedChanged();
      NotifyOfPropertyChange(() => IsChecked);

      if (!ignoreRestrictions && RadioGroup != null && _isChecked)
      {
        RadioGroup.UncheckOthers(this);
      }
    }

    protected virtual bool CoerceInput(bool newValue)
    {
      return CoerceInputHook?.Invoke(newValue) ?? newValue;
    }

    protected virtual void OnIsCheckedChanged()
    {
      IsCheckedChangedHook?.Invoke(_isChecked);
    }


    public CheckableItem SetIsCheckedChangedHook(Action<bool> isCheckedChangedHook)
    {
      IsCheckedChangedHook = isCheckedChangedHook;
      return this;
    }

    public CheckableItem SetCoerceInputHook(Func<bool, bool> coerceInputHook)
    {
      CoerceInputHook = coerceInputHook;
      return this;
    }

    public CheckableItem SetDisplayName(string displayName)
    {
      DisplayName = displayName;
      return this;
    }


  }

  //-----------------------------------------
  public class ScreenItem : PresentationItem
  {
    public override ItemType ItemType => ItemType.Screen;
    public override bool IsBaseStyle => false;
    public override bool IsScreenStyle => true;

    //public IInnerScreen Screen { get; set; }
  }

  //-----------------------------------------
  public class GroupItem : PresentationItem
  {
    public override ItemType ItemType => ItemType.Group;
    public override bool IsGroupStyle => true;

    public BindableCollection<ItemBase> Items { get; }

    public GroupItem()
    {
      Items = new BindableCollection<ItemBase>();
    }
  }

  //-----------------------------------------
  public class RadioGroupItem : GroupItem
  {
    public CheckableItem AddItem(string displayName, bool isChecked = false)
    {
      var item = new CheckableItem(isChecked, this)
      {
        DisplayName = displayName
      };
      Items.Add(item);
      return item;
    }

    public void UncheckOthers(CheckableItem item)
    {
      Items.OfType<CheckableItem>().ForEach(otherItem =>
      {
        if (ReferenceEquals(item, otherItem)) return;
        otherItem.SetIsChecked(false, ignoreRestrictions: true, skipCoercion: false);
      });
    }
  }

}
