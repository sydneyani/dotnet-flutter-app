using Meridian.Domain.Common;

namespace Meridian.Domain.WorkOrders;

public sealed class WorkOrder : Entity
{
    private readonly List<WorkOrderComment> _comments = [];

    private WorkOrder()
    {
    }

    public string Number { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public Guid AssetId { get; private set; }
    public Guid SiteId { get; private set; }
    public WorkOrderType Type { get; private set; }
    public WorkOrderPriority Priority { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public decimal LaborHours { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<WorkOrderComment> Comments => _comments;

    public bool IsTerminal => Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled;

    public static WorkOrder Open(
        string number,
        string title,
        string description,
        Guid assetId,
        Guid siteId,
        WorkOrderType type,
        WorkOrderPriority priority,
        Guid createdByUserId,
        Guid? assignedToUserId,
        DateTimeOffset? dueAt,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new DomainException("Work order number is required.");
        }

        if (assetId == Guid.Empty)
        {
            throw new DomainException("Asset is required.");
        }

        if (siteId == Guid.Empty)
        {
            throw new DomainException("Site is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Creator is required.");
        }

        var workOrder = new WorkOrder
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            Number = number.Trim().ToUpperInvariant(),
            AssetId = assetId,
            SiteId = siteId,
            Status = WorkOrderStatus.Open,
            CreatedByUserId = createdByUserId,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        workOrder.UpdateDetails(title, description, type, priority, dueAt, utcNow);
        workOrder.Assign(assignedToUserId, utcNow);
        return workOrder;
    }

    public void UpdateDetails(
        string title,
        string description,
        WorkOrderType type,
        WorkOrderPriority priority,
        DateTimeOffset? dueAt,
        DateTimeOffset utcNow)
    {
        EnsureMutable();
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Work order type is invalid.");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new DomainException("Work order priority is invalid.");
        }

        Title = DomainText.Required(title, "Title", 140);
        Description = DomainText.Required(description, "Description", 4000);
        Type = type;
        Priority = priority;
        DueAt = dueAt;
        UpdatedAt = utcNow;
    }

    public void Assign(Guid? userId, DateTimeOffset utcNow)
    {
        EnsureMutable();
        if (userId == Guid.Empty)
        {
            throw new DomainException("Assignee is invalid.");
        }

        AssignedToUserId = userId;
        UpdatedAt = utcNow;
    }

    public void RecordLabor(decimal hours, DateTimeOffset utcNow)
    {
        EnsureMutable();
        if (hours < 0m || hours > 999.99m)
        {
            throw new DomainException("Labor hours must be between 0 and 999.99.");
        }

        LaborHours = decimal.Round(hours, 2, MidpointRounding.AwayFromZero);
        UpdatedAt = utcNow;
    }

    public void TransitionTo(WorkOrderStatus next, DateTimeOffset utcNow, decimal? laborHours = null)
    {
        if (!Enum.IsDefined(next))
        {
            throw new DomainException("Work order status is invalid.");
        }

        if (!WorkOrderTransitions.CanTransition(Status, next))
        {
            throw new DomainException($"Cannot move a work order from {Status} to {next}.");
        }

        if (laborHours is not null)
        {
            RecordLabor(laborHours.Value, utcNow);
        }

        if (next == WorkOrderStatus.Completed && LaborHours <= 0m)
        {
            throw new DomainException("Record labor hours before completing a work order.");
        }

        if (next == WorkOrderStatus.InProgress && StartedAt is null)
        {
            StartedAt = utcNow;
        }

        if (next == WorkOrderStatus.Completed)
        {
            CompletedAt = utcNow;
        }

        Status = next;
        UpdatedAt = utcNow;
    }

    public WorkOrderComment AddComment(Guid authorUserId, string body, DateTimeOffset utcNow, Guid? id = null)
    {
        var comment = WorkOrderComment.Create(Id, authorUserId, body, utcNow, id);
        _comments.Add(comment);
        return comment;
    }

    private void EnsureMutable()
    {
        if (IsTerminal)
        {
            throw new DomainException($"Work order {Number} is {Status} and cannot be changed.");
        }
    }
}
