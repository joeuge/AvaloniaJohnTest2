namespace Iface.Utils;

public enum OperatingSystemId
{
  Unknown,
  Windows,
  Linux,
}

//====================================================================
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ViewTypeAttribute : Attribute
{
  public Type ViewType { get; }
  public string? Context { get; }

  public ViewTypeAttribute(Type viewType)
  {
    ViewType = viewType;
  }

  public ViewTypeAttribute(Type viewType, string context)
  {
    ViewType = viewType;
    Context = context;
  }

  public bool IdentityTest(object context)
  {
    if (Context == null)
      return context == null;
    return Context.Equals(context as string);
  }
}

//====================================================================
public abstract class InstanceAttribute : Attribute
{
  public OperatingSystemId OperatingSystemId { get; set; }

  public bool IsConform(OperatingSystemId id)
  {
    return OperatingSystemId == OperatingSystemId.Unknown || OperatingSystemId == id;
  }
}


//====================================================================
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TransientInstanceAttribute : InstanceAttribute
{
}


//====================================================================
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class SingletonInstanceAttribute : InstanceAttribute
{
  public bool IsDisabled { get; set; }

  public override bool Equals(object? obj)
  {
    return ReferenceEquals(this, obj);
  }
}


//====================================================================
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ContractAttribute : Attribute
{
  public string? ContractName { get; }
  public Type? ContractType { get; }

  public ContractAttribute()
    : this(null, null)
  {
  }

  public ContractAttribute(Type? contractType)
    : this(null, contractType)
  {
  }

  public ContractAttribute(string? contractName)
    : this(contractName, null)
  {
  }

  public ContractAttribute(string? contractName, Type? contractType)
  {
    ContractName = contractName;
    ContractType = contractType;
  }
}
