using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Mdm.References.Dto;

namespace MyIS.Core.Application.Mdm.References;

public interface IMdmReferencesQueryService
{
    Task<MdmListResultDto<MdmUnitReferenceDto>> GetUnitsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmUnitReferenceDto?> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmSupplierReferenceDto>> GetSuppliersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmSupplierReferenceDto?> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmCustomerReferenceDto>> GetCustomersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmCustomerReferenceDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmCounterpartyReferenceDto>> GetCounterpartiesAsync(
        string? q,
        bool? isActive,
        string? roleType,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmCounterpartyReferenceDto?> GetCounterpartyByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmItemGroupReferenceDto>> GetItemGroupsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmItemGroupReferenceDto?> GetItemGroupByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmItemReferenceDto>> GetItemsAsync(
        string? q,
        bool? isActive,
        Guid? groupId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmItemReferenceDto?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmManufacturerReferenceDto>> GetManufacturersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmManufacturerReferenceDto?> GetManufacturerByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmSimpleReferenceDto>> GetBodyTypesAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmSimpleReferenceDto?> GetBodyTypeByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmCurrencyReferenceDto>> GetCurrenciesAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmCurrencyReferenceDto?> GetCurrencyByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmSimpleReferenceDto>> GetTechnicalParametersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmSimpleReferenceDto?> GetTechnicalParameterByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmSimpleReferenceDto>> GetParameterSetsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmSimpleReferenceDto?> GetParameterSetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmSimpleReferenceDto>> GetSymbolsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<MdmSimpleReferenceDto?> GetSymbolByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MdmListResultDto<MdmExternalEntityLinkDto>> GetExternalEntityLinksAsync(
        string? q,
        string? entityType,
        string? externalSystem,
        string? externalEntity,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
