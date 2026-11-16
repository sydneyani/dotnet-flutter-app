using Meridian.Domain;
using Meridian.Domain.Common;
using Meridian.Domain.WorkOrders;

namespace Meridian.Domain.Tests;

public class WorkOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 2, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid AssetId = Guid.NewGuid();
    private static readonly Guid SiteId = Guid.NewGuid();
    private static readonly Guid CreatorId = Guid.NewGuid();

    [Theory]
    [InlineData(WorkOrderStatus.Open, WorkOrderStatus.InProgress, true)]
    [InlineData(WorkOrderStatus.Open, WorkOrderStatus.Cancelled, true)]
    [InlineData(WorkOrderStatus.Open, WorkOrderStatus.Completed, false)]
    [InlineData(WorkOrderStatus.Open, WorkOrderStatus.WaitingParts, false)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.WaitingParts, true)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.Completed, true)]
    [InlineData(WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled, true)]
    [InlineData(WorkOrderStatus.WaitingParts, WorkOrderStatus.InProgress, true)]
    [InlineData(WorkOrderStatus.WaitingParts, WorkOrderStatus.Completed, false)]
    [InlineData(WorkOrderStatus.Completed, WorkOrderStatus.Open, false)]
    [InlineData(WorkOrderStatus.Cancelled, WorkOrderStatus.InProgress, false)]
    public void Transitions_follow_the_field_state_machine(WorkOrderStatus from, WorkOrderStatus to, bool allowed)
    {
        Assert.Equal(allowed, WorkOrderTransitions.CanTransition(from, to));
    }

    [Fact]
    public void Completing_requires_labor_and_stamps_the_timeline()
    {
        var workOrder = Open();
        workOrder.TransitionTo(WorkOrderStatus.InProgress, Now.AddHours(1));

        var error = Assert.Throws<DomainException>(() => workOrder.TransitionTo(WorkOrderStatus.Completed, Now.AddHours(2)));
        Assert.Contains("labor", error.Message, StringComparison.OrdinalIgnoreCase);

        workOrder.TransitionTo(WorkOrderStatus.Completed, Now.AddHours(3), 1.25m);

        Assert.Equal(AssetId, workOrder.AssetId);
        Assert.Equal(SiteId, workOrder.SiteId);
        Assert.Equal(WorkOrderStatus.Completed, workOrder.Status);
        Assert.Equal(1.25m, workOrder.LaborHours);
        Assert.Equal(Now.AddHours(1), workOrder.StartedAt);
        Assert.Equal(Now.AddHours(3), workOrder.CompletedAt);
        Assert.Empty(WorkOrderTransitions.AllowedFrom(workOrder.Status));
    }

    [Fact]
    public void Closed_work_orders_reject_edits_but_keep_the_handoff_log()
    {
        var workOrder = Open();
        workOrder.TransitionTo(WorkOrderStatus.Cancelled, Now.AddHours(1));

        Assert.Throws<DomainException>(() => workOrder.UpdateDetails("New", "Nope", WorkOrderType.Corrective, WorkOrderPriority.Low, null, Now));
        var comment = workOrder.AddComment(CreatorId, "Left the site as found.", Now.AddHours(2));

        Assert.Equal("Left the site as found.", comment.Body);
        Assert.Single(workOrder.Comments);
    }

    [Fact]
    public void Labor_hours_are_rounded_and_bounded()
    {
        var workOrder = Open();
        workOrder.RecordLabor(1.005m, Now);
        Assert.Equal(1.01m, workOrder.LaborHours);
        Assert.Throws<DomainException>(() => workOrder.RecordLabor(-0.1m, Now));
    }

    private static WorkOrder Open() =>
        WorkOrder.Open(
            "WO-00011",
            "Torque the breaker cabinet heater",
            "Heater circuit was open on the last round.",
            AssetId,
            SiteId,
            WorkOrderType.Corrective,
            WorkOrderPriority.Medium,
            CreatorId,
            CreatorId,
            Now.AddDays(2),
            Now);
}
