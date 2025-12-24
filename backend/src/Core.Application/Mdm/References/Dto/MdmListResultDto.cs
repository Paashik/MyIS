using System;
using System.Collections.Generic;

namespace MyIS.Core.Application.Mdm.References.Dto;

public sealed class MdmListResultDto<T>
{
    public int Total { get; set; }

    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
}

