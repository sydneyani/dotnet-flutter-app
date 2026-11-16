namespace Meridian.Domain;

public enum UserRole
{
    Technician = 1,
    Supervisor = 2,
    Admin = 3
}

public enum AssetCategory
{
    Transformer = 1,
    CircuitBreaker = 2,
    Switchgear = 3,
    ProtectiveRelay = 4,
    Pump = 5,
    Compressor = 6,
    Turbine = 7,
    Valve = 8,
    Motor = 9,
    Instrument = 10
}

public enum AssetStatus
{
    InService = 1,
    OutOfService = 2,
    Retired = 3
}

public enum AssetCriticality
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum WorkOrderType
{
    Preventive = 1,
    Corrective = 2,
    Inspection = 3,
    Emergency = 4
}

public enum WorkOrderPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum WorkOrderStatus
{
    Open = 1,
    InProgress = 2,
    WaitingParts = 3,
    Completed = 4,
    Cancelled = 5
}

public enum InspectionResult
{
    Pass = 1,
    Conditional = 2,
    Fail = 3
}
