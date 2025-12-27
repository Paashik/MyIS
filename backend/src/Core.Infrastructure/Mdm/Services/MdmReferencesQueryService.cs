using System;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Services;

public sealed partial class MdmReferencesQueryService : IMdmReferencesQueryService
{
    private readonly AppDbContext _dbContext;

    public MdmReferencesQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    private static string? NormalizeQuery(string? q)
    {
        q = q?.Trim();
        return string.IsNullOrWhiteSpace(q) ? null : q;
    }

    private static int? TryParseRoleType(string? roleType)
    {
        roleType = NormalizeQuery(roleType);
        if (roleType == null)
        {
            return null;
        }

        if (int.TryParse(roleType, out var numeric))
        {
            return numeric;
        }

        return roleType.ToUpperInvariant() switch
        {
            "SUPPLIER" => CounterpartyRoleTypes.Supplier,
            "CUSTOMER" => CounterpartyRoleTypes.Customer,
            _ => null
        };
    }

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);


}
