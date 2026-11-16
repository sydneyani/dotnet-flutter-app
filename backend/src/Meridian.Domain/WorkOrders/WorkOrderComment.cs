using Meridian.Domain.Common;

namespace Meridian.Domain.WorkOrders;

public sealed class WorkOrderComment : Entity
{
    private WorkOrderComment()
    {
    }

    public Guid WorkOrderId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    public static WorkOrderComment Create(
        Guid workOrderId,
        Guid authorUserId,
        string body,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        if (workOrderId == Guid.Empty)
        {
            throw new DomainException("Comment work order is required.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new DomainException("Comment author is required.");
        }

        return new WorkOrderComment
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            WorkOrderId = workOrderId,
            AuthorUserId = authorUserId,
            Body = DomainText.Required(body, "Comment", 2000),
            CreatedAt = utcNow
        };
    }
}
