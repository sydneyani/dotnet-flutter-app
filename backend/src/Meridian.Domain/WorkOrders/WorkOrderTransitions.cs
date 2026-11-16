namespace Meridian.Domain.WorkOrders;

public static class WorkOrderTransitions
{
    public static IReadOnlyList<WorkOrderStatus> AllowedFrom(WorkOrderStatus current) => current switch
    {
        WorkOrderStatus.Open => [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        WorkOrderStatus.InProgress => [WorkOrderStatus.WaitingParts, WorkOrderStatus.Completed, WorkOrderStatus.Cancelled],
        WorkOrderStatus.WaitingParts => [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        _ => []
    };

    public static bool CanTransition(WorkOrderStatus current, WorkOrderStatus next) =>
        AllowedFrom(current).Contains(next);
}
