namespace Chickensoft.Serialization.Tests.Fixtures;

using Chickensoft.Collections;
using Chickensoft.Introspection;

[Meta, Id("versioned_model")]
public abstract partial record VersionedModel;

[Meta, Version(1)]
public partial record VersionedModel1 : VersionedModel, IOutdated
{
  public object Upgrade(IReadOnlyBlackboard deps) => new VersionedModel2();
}

[Meta, Version(2)]
public partial record VersionedModel2 : VersionedModel, IOutdated
{
  public object Upgrade(IReadOnlyBlackboard deps) => new VersionedModel3();
}

[Meta, Version(3)]
public partial record VersionedModel3 : VersionedModel;
