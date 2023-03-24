using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Iface.Utils.Avalonia
{
  public static class AvaloniaTreeUtility
  {
    public static AvaloniaObject? GetParent(AvaloniaObject element)
    {
      //Avalonia.LogicalTree.LogicalExtensions
      //Avalonia.VisualTree.VisualExtensions

      return (element as Visual)?.GetVisualParent() as AvaloniaObject;
    }

    public static AvaloniaObject GetParentEx(AvaloniaObject element)
    {
      var parent = (element as Visual)?.GetVisualParent() as AvaloniaObject;
      if (parent != null)
        return parent;

      parent = (element as ILogical)?.GetLogicalParent() as AvaloniaObject;
      if (parent != null)
        return parent;

      return null;
    }


    public static T? FindAncestor<T>(AvaloniaObject element)
      where T : AvaloniaObject
    {
      var parentObject = GetParent(element);

      if (parentObject == null)
        return default;

      if (parentObject is T parent)
        return parent;

      return FindAncestor<T>(parentObject);
    }


    public static AvaloniaObject? FindAncestor(AvaloniaObject element, string elementName, int ancestorLevel)
    {
      while (true)
      {
        if (ancestorLevel <= 0)
          return null;

        var parentObject = GetParent(element);

        if (parentObject == null)
          return null;

        if (parentObject is INamed parent)
        {
          if (parent.Name == elementName)
            return (parent as AvaloniaObject);
        }

        ancestorLevel--;
        element = parentObject;
      }
    }


    public static bool IsInTree(AvaloniaObject tree, AvaloniaObject? node)
    {
      while (true)
      {
        if (node == null)
          return false;

        if (ReferenceEquals(tree, node))
          return true;

        node = GetParent(node);
      }
    }



    // #john#info# можно, например, найти у TabItem что-нибудь в DataTemplate для заголовка
    /*
    public static object FindChild(IAvaloniaObject element, string toFind)
    {
      var childrenCount = VisualTreeHelper.GetChildrenCount(element);

      for (var i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(element, i);

        if (child is FrameworkElement child2)
        {
          if (child2.Name == toFind)
          {
            return child2;
          }

          var child3 = child2.FindName(toFind);

          if (child3 != null)
          {
            return child3;
          }
        }

        var child4 = FindChild(child, toFind);

        if (child4 != null)
        {
          return child4;
        }
      }
      return null;
    }
    */

  }


  //==================================
  public static class VisualTreeUtility
  {
    public static T? FindParent<T>(Visual? element) where T : Visual
    {
      while (true)
      {
        if (element == null) return default;

        var parentObject = element.GetVisualParent();

        if (parentObject == null) return default;

        if (parentObject is T parent) return parent;

        element = parentObject;
      }
    }

    public static Visual GetRoot(Visual element)
    {
      /*
      var visualRoot = element.VisualRoot;
      if (visualRoot != null)
      {
        return visualRoot;
      }
      */

      var next = element.GetVisualParent();
      while (next != null)
      {
        element = next;
        next = element.GetVisualParent();
      }
      return element;
    }

    public static Visual GetRoot(Visual element, out INamed? nearestNamedElement)
    {
      nearestNamedElement = null;
      if (element == null) return default!;
      while (true)
      {
        if (nearestNamedElement == null && !string.IsNullOrEmpty((element as INamed)?.Name))
        {
          nearestNamedElement = (INamed)element;
        }
        var next = element.GetVisualParent();
        if (next == null)
          break;
        element = next;
      }
      return element;
    }
  }


}