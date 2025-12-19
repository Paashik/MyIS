using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IItemSequenceProvider
{
    Task<ItemSequence?> GetByItemKindAsync(ItemKind itemKind);
    Task<string> GenerateNextCodeAsync(ItemKind itemKind);
    Task UpdateSequenceAsync(ItemKind itemKind);
}