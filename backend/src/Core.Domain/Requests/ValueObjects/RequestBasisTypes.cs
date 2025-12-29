namespace MyIS.Core.Domain.Requests.ValueObjects;

public static class RequestBasisTypes
{
    public const string IncomingRequest = "IncomingRequest";
    public const string CustomerOrder = "CustomerOrder";
    public const string ProductionOrder = "ProductionOrder";
    public const string Other = "Other";

    public static bool IsValid(string value)
    {
        return value == IncomingRequest
               || value == CustomerOrder
               || value == ProductionOrder
               || value == Other;
    }
}
