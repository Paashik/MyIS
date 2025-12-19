namespace MyIS.Core.WebApi.Dto.Integration;

public class Component2020ConnectionDto
{
    public string? Id { get; set; }

    public string MdbPath { get; set; } = null!;

    public string? Login { get; set; }

    public bool IsActive { get; set; } = true;

    public bool HasPassword { get; set; }

    public string? Password { get; set; }

    public bool ClearPassword { get; set; }
}
