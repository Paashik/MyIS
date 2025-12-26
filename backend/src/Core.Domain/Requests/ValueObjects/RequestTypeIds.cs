using System;

namespace MyIS.Core.Domain.Requests.ValueObjects;

public static class RequestTypeIds
{
    public static readonly RequestTypeId CustomerDevelopment = RequestTypeId.From(
        new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    public static readonly RequestTypeId InternalProduction = RequestTypeId.From(
        new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    public static readonly RequestTypeId ChangeRequest = RequestTypeId.From(
        new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    public static readonly RequestTypeId SupplyRequest = RequestTypeId.From(
        new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public static readonly RequestTypeId ExternalTechStage = RequestTypeId.From(
        new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
}
