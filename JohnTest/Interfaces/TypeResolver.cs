namespace AppNs.Interfaces;

public interface ITypeResolver
{
  Type ResolveType();
}

public interface IDataIdResolver : ITypeResolver
{
  VarKey ResolveDataId();
}

//==================================
public class TypeResolverBase : ITypeResolver, IAutoDiscover
{
  public virtual Type ResolveType() { return null; }
}

//==================================
public class DataIdResolverBase : TypeResolverBase, IDataIdResolver
{
  public virtual VarKey ResolveDataId() { return null; }
}
