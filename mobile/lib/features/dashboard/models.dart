class CountByLabel {
  const CountByLabel({required this.label, required this.count});

  final String label;
  final int count;

  factory CountByLabel.fromJson(Map<String, dynamic> json) =>
      CountByLabel(label: json['label'] as String, count: json['count'] as int);
}

class DashboardSummary {
  const DashboardSummary({
    required this.openWorkOrders,
    required this.inProgressWorkOrders,
    required this.waitingPartsWorkOrders,
    required this.overdueWorkOrders,
    required this.completedLast30Days,
    required this.averageLaborHoursCompleted,
    required this.assetsInService,
    required this.assetsOutOfService,
    required this.criticalAssets,
    required this.failedInspectionsLast30Days,
    required this.activeWorkOrdersByPriority,
    this.fromCache = false,
  });

  final int openWorkOrders;
  final int inProgressWorkOrders;
  final int waitingPartsWorkOrders;
  final int overdueWorkOrders;
  final int completedLast30Days;
  final double averageLaborHoursCompleted;
  final int assetsInService;
  final int assetsOutOfService;
  final int criticalAssets;
  final int failedInspectionsLast30Days;
  final List<CountByLabel> activeWorkOrdersByPriority;
  final bool fromCache;

  factory DashboardSummary.fromJson(Map<String, dynamic> json, {bool fromCache = false}) => DashboardSummary(
    openWorkOrders: json['openWorkOrders'] as int,
    inProgressWorkOrders: json['inProgressWorkOrders'] as int,
    waitingPartsWorkOrders: json['waitingPartsWorkOrders'] as int,
    overdueWorkOrders: json['overdueWorkOrders'] as int,
    completedLast30Days: json['completedLast30Days'] as int,
    averageLaborHoursCompleted: (json['averageLaborHoursCompleted'] as num).toDouble(),
    assetsInService: json['assetsInService'] as int,
    assetsOutOfService: json['assetsOutOfService'] as int,
    criticalAssets: json['criticalAssets'] as int,
    failedInspectionsLast30Days: json['failedInspectionsLast30Days'] as int,
    activeWorkOrdersByPriority: (json['activeWorkOrdersByPriority'] as List<dynamic>)
        .map((item) => CountByLabel.fromJson(item as Map<String, dynamic>))
        .toList(),
    fromCache: fromCache,
  );
}
