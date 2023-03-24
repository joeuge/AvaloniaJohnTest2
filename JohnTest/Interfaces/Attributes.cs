namespace AppNs.Interfaces;

//====================================================================
[AttributeUsage(AttributeTargets.Class)]
public abstract class MasterScreenAttribute : Attribute
{
  public object FactoryId { get; set; }
  public string FactoryName { get; set; }
  public Type ContractType { get; set; } // Page Contract Type (IoC) 
  public object DataId { get; set; } // see VarKey.TryCreate(object value, out VarKey result)


  protected MasterScreenAttribute()
  {
  }

  protected MasterScreenAttribute(object factoryId, Type contractType)
  {
    FactoryId = factoryId;
    ContractType = contractType;
  }

  public bool TryGetFactoryId(out VarKey result)
  {
    return VarKey.TryCreate(FactoryId, out result);
  }

  public bool TryGetDataId(out VarKey result)
  {
    return VarKey.TryCreate(DataId, out result);
  }

}

//====================================================================
public class PageAttribute : MasterScreenAttribute
{
  public bool IsEmptyClass { get; set; }

  public PageAttribute()
  {
  }

  public PageAttribute(object factoryId, Type contractType = null) : base(factoryId, contractType)
  {
  }
}
