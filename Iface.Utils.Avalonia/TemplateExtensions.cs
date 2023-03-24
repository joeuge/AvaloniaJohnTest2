using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Iface.Utils.Avalonia;

public static class TemplateExtensions
{
  /// <summary>
  /// Gets a named control from a templated control's template children.
  /// </summary>
  /// <param name="template">The control template.</param>
  /// <param name="name">The name of the control.</param>
  /// <param name="templatedParent">The templated parent control.</param>
  /// <returns>An <see cref="IControl"/> or null if the control was not found.</returns>
  public static Control FindName(this IControlTemplate template, string name, Control templatedParent)
  {
    return ((Control)templatedParent.GetVisualChildren().FirstOrDefault())?.FindControl<Control>(name); // todo: why was <Canvas>?
  }

  /// <summary>
  /// Gets a named control from a templated control's template children.
  /// </summary>
  /// <typeparam name="T">The type of the template child.</typeparam>
  /// <param name="template">The control template.</param>
  /// <param name="name">The name of the control.</param>
  /// <param name="templatedParent">The templated parent control.</param>
  /// <returns>An <see cref="IControl"/> or null if the control was not found.</returns>
  public static T FindName<T>(this IControlTemplate template, string name, Control templatedParent)
      where T : Control
  {
    return template.FindName(name, templatedParent) as T;
  }

  public static IEnumerable<Control> GetTemplateChildren(this TemplatedControl control)
  {
    foreach (Control child in GetTemplateChildren((Control)control, control))
    {
      yield return child;
    }
  }
  private static IEnumerable<Control> GetTemplateChildren(Control control, TemplatedControl templatedParent)
  {
    foreach (Control child in control.GetVisualChildren())
    {
      var childTemplatedParent = child.TemplatedParent;
      if (childTemplatedParent == templatedParent)
      {
        yield return child;
      }
      if (childTemplatedParent != null)
      {
        foreach (var descendant in GetTemplateChildren(child, templatedParent))
        {
          yield return descendant;
        }
      }
    }
  }
}