namespace MyIS.Core.Application.Integration.Component2020.Dto;

public class Component2020ConnectionDto
{
    public string MdbPath { get; set; } = null!;

    public string? Login { get; set; }

    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ClearPassword { get; set; }
}
