using System.ComponentModel.DataAnnotations;

public class ProcessorChannelSettings
{
    [Range(1, 100, ErrorMessage = "Capacity must be at least 1")]
    public int Capacity { get; set; }

    [Range(1, 100)]
    public int Step1Capacity { get; set; }

    [Range(1, 10)]
    public int Step1MaxConcurrency { get; set; }

    [Range(1, 100)]
    public int Step2Capacity { get; set; }

    [Range(1, 10)]
    public int Step2MaxConcurrency { get; set; }

    [Range(1, 10)]
    public int MaxConcurrency { get; set; } = 5;

    public bool UseUnifiedSpanForAllPipelines { get; set; }
}
