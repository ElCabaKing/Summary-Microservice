using Microsoft.SemanticKernel;

namespace SummaryService.Infrastructure.Factory;
public interface IKernelFactory
{
    Kernel Create(
        string provider,
        string model);
}
