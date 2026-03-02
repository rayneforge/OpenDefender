using System;

namespace Library.Domain.Models.State;

public class OrchestrationState
{
    public int Id { get; set; }
    public Guid RunId { get; set; } = Guid.NewGuid();
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}
