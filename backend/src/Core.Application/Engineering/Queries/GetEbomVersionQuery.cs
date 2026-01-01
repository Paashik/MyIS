using MediatR;

namespace MyIS.Core.Application.Engineering.Queries;

/// <summary>
/// Запрос на получение версии BOM по ID
/// </summary>
public record GetEbomVersionQuery(Guid BomVersionId) : IRequest<EbomVersionResponse>;

/// <summary>
/// Ответ с версией BOM
/// </summary>
public record EbomVersionResponse(EbomVersionDto Version);