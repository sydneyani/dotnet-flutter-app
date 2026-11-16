using Meridian.Application.Common;

namespace Meridian.Application.Authorization;

public static class AccessRules
{
    public static bool CanManageAssets(UserRole role) =>
        role is UserRole.Supervisor or UserRole.Admin;

    public static bool CanModifyWorkOrder(UserRole role, Guid userId, WorkOrder workOrder) =>
        role is UserRole.Supervisor or UserRole.Admin
        || workOrder.AssignedToUserId == userId
        || workOrder.CreatedByUserId == userId;

    public static void EnsureCanAssign(UserRole role, Guid userId, Guid? currentAssignee, Guid? requestedAssignee)
    {
        if (requestedAssignee == currentAssignee)
        {
            return;
        }

        if (role is UserRole.Supervisor or UserRole.Admin)
        {
            return;
        }

        if (requestedAssignee == userId)
        {
            return;
        }

        throw new ForbiddenException("Technicians can only assign work orders to themselves.");
    }
}
