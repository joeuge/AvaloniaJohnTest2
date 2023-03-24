using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Caliburn.Micro;

namespace Iface.Utils.Avalonia
{
  public abstract class LightModel : INotifyPropertyChanged
  {
    [Browsable(false)]
    public bool IsNotifying { get; set; } // added 15 apr 2022

    public virtual event PropertyChangedEventHandler PropertyChanged;

    protected LightModel()
    {
      IsNotifying = true;
    }


    private bool _inRefresh;

    public virtual void Refresh()
    {
      _inRefresh = true; 
      try
      {
        NotifyOfPropertyChange(string.Empty);
      }
      finally
      {
        _inRefresh = false;
      }
    }

    public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
    {
      NotifyOfPropertyChange(Caliburn.Micro.ExpressionExtensions.GetMemberInfo(property).Name);
    }

    public void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)
    {
      if (!IsNotifying || PropertyChanged == null)
        return;
      OnUIThread(() => OnPropertyChanged(new PropertyChangedEventArgs(propertyName)));
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
      PropertyChanged?.Invoke(this, e);
    }

    protected virtual void OnUIThread(System.Action action)
    {
      action.OnUIThread();
    }

    protected void SetPropertyValue<TValue>(ref TValue field, TValue value, [CallerMemberName] string propertyName = null)
    {
      if (_inRefresh)
        return; // This is Patch for TwoWay-binding-strange-bahavior when Refresh()

      if (Equals(field, value))
        return;
      field = value;
      NotifyOfPropertyChange(propertyName);
    }
  }
}
