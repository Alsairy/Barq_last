using BARQ.Core.DTOs.AI;

namespace BARQ.Core.Interfaces;

public interface IAiProvider
{
    string Name { get; }
    Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync();
    Task<decimal> CalculateCostAsync(int inputTokens, int outputTokens);
}
