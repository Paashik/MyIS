using System;
using System.Collections.Generic;
using System.Linq;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Domain.Mdm.Services;

public static class ExternalEntityLinkSelector
{
    public static ExternalEntityLink? SelectLatestLink(IEnumerable<ExternalEntityLink> links)
    {
        return links
            .OrderByDescending(l => l.SyncedAt ?? l.UpdatedAt)
            .ThenByDescending(l => l.UpdatedAt)
            .FirstOrDefault();
    }

    public static Dictionary<Guid, ExternalEntityLink> SelectLatestLinks(IEnumerable<ExternalEntityLink> links)
    {
        return links
            .GroupBy(l => l.EntityId)
            .ToDictionary(g => g.Key, g => SelectLatestLink(g)!);
    }
}
