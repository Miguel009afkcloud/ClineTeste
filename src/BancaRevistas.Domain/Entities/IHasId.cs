namespace BancaRevistas.Domain.Entities;

/// <summary>
/// Contrato comum das entidades persistidas, usado para simplificar
/// operações genéricas de CRUD e de auditoria.
/// </summary>
public interface IHasId
{
    int Id { get; set; }
}
