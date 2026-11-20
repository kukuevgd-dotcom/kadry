namespace Kadry.Core.Domain;

/// <summary>Сущность с ключом.</summary>
public interface IHasId
{
    int Id { get; }
}

/// <summary>Сущность с названием (справочники).</summary>
public interface IHasName
{
    string Name { get; }
}
