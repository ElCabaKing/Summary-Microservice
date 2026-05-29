using Microsoft.SemanticKernel;

namespace SummaryService.Application.Interfaces;
public interface IKernelFactory
{
    Kernel Create(
        string provider,
        string model);
}
