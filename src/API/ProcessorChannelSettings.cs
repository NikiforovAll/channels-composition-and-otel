using System.ComponentModel.DataAnnotations;

public class ProcessorChannelSettings(int Capacity)
{
    [Range(1, 100, ErrorMessage = "Capacity must be at least 1")]
    public int Capacity { get; set; } = Capacity;
}
