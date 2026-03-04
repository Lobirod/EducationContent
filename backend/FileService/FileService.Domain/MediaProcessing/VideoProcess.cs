using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain.MediaProcessing;

public sealed class VideoProcess
{
    private static readonly Dictionary<StepType, int> _stepWeights = new()
    {
        { StepType.INITIALIZE, 0 },
        { StepType.EXTRACT_METADATA, 10 },
        { StepType.GENERATE_HLS, 70 },
        { StepType.UPLOAD_HLS, 15 },
        { StepType.CLEANUP, 5 }
    };

    private readonly List<ProcessingStep> _steps = [];

    public Guid Id { get; private set; }

    public Guid VideoAssetId { get; private set; }

    public ProcessingStatus Status { get; private set; }

    public int ProgressPercentage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool IsCriticalError { get; private set; }

    public int RetryCount { get; private set; }

    public int MaxRetries { get; private set; } = 3;

    public DateTime? NextRetryAt { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<ProcessingStep> Steps => _steps.AsReadOnly();

    public ProcessingStep? CurrentStep => _steps.FirstOrDefault(s => s.Status == StepStatus.IN_PROGRESS);

    public VideoProcess(Guid videoAssetId)
    {
        Id = Guid.NewGuid();
        VideoAssetId = videoAssetId;
        Status = ProcessingStatus.IN_PROGRESS;
        ProgressPercentage = 0;
        StartedAt = DateTime.UtcNow;

        InitializeSteps();
    }

    // Ef Core
    private VideoProcess()
    {
    }

    public Result<ProcessingStep?, Error> ProcessNextStep()
    {
        if (Status != ProcessingStatus.IN_PROGRESS)
        {
            return Error.Failure(
                "processing.invalid.status",
                $"Cannot process step when status is {Status}");
        }

        ProcessingStep? currentStep = CurrentStep;
        if (currentStep is not null)
            return currentStep;

        ProcessingStep? nextStep = _steps
            .OrderBy(s => s.Order)
            .FirstOrDefault(s => s.Status == StepStatus.PENDING);

        if (nextStep is null)
        {
            Complete();
            return Result.Success<ProcessingStep?, Error>(null);
        }

        UnitResult<Error> startResult = nextStep.Start();
        if (startResult.IsFailure)
            return startResult.Error;

        return nextStep;
    }

    public UnitResult<Error> CompleteCurrentStep(string? resultData = null)
    {
        if (Status != ProcessingStatus.IN_PROGRESS)
            return Error.Validation("processing.invalid.status", $"Cannot complete step when status is {Status}");

        ProcessingStep? currentStep = CurrentStep;
        if (currentStep is null)
            return Error.Validation("processing.no.active.step", "No active step to complete");

        UnitResult<Error> completeResult = currentStep.Complete(resultData);
        if (completeResult.IsFailure)
            return completeResult.Error;

        RecalculateProgress();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> FailCurrentStep(string errorMessage)
    {
        if (Status != ProcessingStatus.IN_PROGRESS)
            return Error.Validation("processing.invalid.status", $"Cannot fail step when status is {Status}");

        ProcessingStep? currentStep = CurrentStep;
        if (currentStep is null)
            return Error.Validation("processing.no.active.step", "No active step to fail");

        UnitResult<Error> failResult = currentStep.Fail(errorMessage);
        if (failResult.IsFailure)
            return failResult.Error;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Fail(string errorMessage, bool isCritical = false)
    {
        if (Status != ProcessingStatus.IN_PROGRESS)
            return Error.Validation("processing.invalid.status", $"Can only fail from IN_PROGRESS status, current: {Status}");

        if (string.IsNullOrWhiteSpace(errorMessage))
            return Error.Validation("processing.error.required", "Error message is required");

        Status = ProcessingStatus.FAILED;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        IsCriticalError = isCritical;

        return UnitResult.Success<Error>();
    }

    public bool CanRetry() => RetryCount < MaxRetries && !IsCriticalError;

    public UnitResult<Error> Reset()
    {
        if (Status != ProcessingStatus.FAILED)
            return Error.Validation("processing.invalid.status", "Can only reset from FAILED status");

        Status = ProcessingStatus.IN_PROGRESS;
        ProgressPercentage = 0;
        CompletedAt = null;
        ErrorMessage = null;
        IsCriticalError = false;

        foreach (ProcessingStep step in _steps)
        {
            step.Reset();
        }

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ScheduleRetry(DateTime nextRetryAt)
    {
        if (Status != ProcessingStatus.FAILED)
            return Error.Validation("processing.invalid.status", "Can only schedule retry from FAILED status");

        if (IsCriticalError)
            return Error.Validation("processing.retry.critical", "Cannot retry critical failure");

        if (RetryCount >= MaxRetries)
            return Error.Validation("processing.retry.exhausted", "Max retries exceeded");

        RetryCount++;
        NextRetryAt = nextRetryAt;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Complete()
    {
        if (Status != ProcessingStatus.IN_PROGRESS)
            return Error.Validation("processing.invalid.status", $"Can only complete from IN_PROGRESS status, current: {Status}");

        bool allStepsCompleted = _steps.All(s => s.Status == StepStatus.COMPLETED);
        if (!allStepsCompleted)
            return Error.Validation("processing.incomplete.steps", "Cannot complete processing when not all steps are completed");

        Status = ProcessingStatus.COMPLETED;
        CompletedAt = DateTime.UtcNow;
        ProgressPercentage = 100;

        return UnitResult.Success<Error>();
    }

    private void RecalculateProgress()
    {
        int totalProgress = _steps
            .Where(s => s.Status == StepStatus.COMPLETED)
            .Sum(s => s.Weight);

        ProgressPercentage = totalProgress;
    }

    private void InitializeSteps()
    {
        int order = 1;
        foreach ((StepType stepType, int weight) in _stepWeights)
        {
            _steps.Add(new ProcessingStep(stepType, order++, weight));
        }
    }
}

public enum ProcessingStatus
{
    IN_PROGRESS,
    COMPLETED,
    FAILED
}