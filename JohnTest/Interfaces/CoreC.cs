﻿using System.Globalization;

namespace AppNs.Interfaces;

public static class CoreDefaults
{
  public const ResizeMode DialogResizeMode = ResizeMode.CanResize;
  public const bool ProblemWithFocus = true;
  public const bool ProblemWithExit = true;
}


// Фабрики документов
public static class FactoryIds
{
  public const uint DummyPage = 100001;
}


public static class VarKeys
{
  #region Doc Factories

  public static VarKey DummyPage { get; } = new VarKey(FactoryIds.DummyPage);

  #endregion
}
