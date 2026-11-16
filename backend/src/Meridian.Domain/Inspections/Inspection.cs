using Meridian.Domain.Common;

namespace Meridian.Domain.Inspections;

public sealed class Inspection : Entity
{
    private Inspection()
    {
    }

    public Guid AssetId { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public Guid InspectorUserId { get; private set; }
    public DateTimeOffset InspectedAt { get; private set; }
    public InspectionResult Result { get; private set; }
    public string Notes { get; private set; } = "";
    public string? Findings { get; private set; }
    public DateOnly? NextDueOn { get; private set; }

    public static Inspection Record(
        Guid assetId,
        Guid? workOrderId,
        Guid inspectorUserId,
        DateTimeOffset inspectedAt,
        InspectionResult result,
        string notes,
        string? findings,
        DateOnly? nextDueOn,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        if (assetId == Guid.Empty)
        {
            throw new DomainException("Inspection asset is required.");
        }

        if (workOrderId == Guid.Empty)
        {
            throw new DomainException("Inspection work order is invalid.");
        }

        if (inspectorUserId == Guid.Empty)
        {
            throw new DomainException("Inspector is required.");
        }

        if (!Enum.IsDefined(result))
        {
            throw new DomainException("Inspection result is invalid.");
        }

        if (inspectedAt > utcNow.AddMinutes(5))
        {
            throw new DomainException("Inspection time cannot be in the future.");
        }

        var cleanedFindings = DomainText.Optional(findings, "Findings", 2000);
        if (result == InspectionResult.Fail && cleanedFindings is null)
        {
            throw new DomainException("A failed inspection needs findings.");
        }

        if (nextDueOn is DateOnly due && due < DateOnly.FromDateTime(inspectedAt.UtcDateTime))
        {
            throw new DomainException("Next due date cannot be before the inspection.");
        }

        return new Inspection
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            AssetId = assetId,
            WorkOrderId = workOrderId,
            InspectorUserId = inspectorUserId,
            InspectedAt = inspectedAt,
            Result = result,
            Notes = DomainText.Required(notes, "Notes", 2000),
            Findings = cleanedFindings,
            NextDueOn = nextDueOn
        };
    }
}
