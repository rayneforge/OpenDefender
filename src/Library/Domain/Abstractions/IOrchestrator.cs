using System.Threading.Tasks;

namespace Library.Domain.Abstractions;

public interface IOrchestrator<T>
{
    Task RunAsync(T state);
}
