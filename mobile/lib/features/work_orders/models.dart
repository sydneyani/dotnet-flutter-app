class WorkOrderSummary {
  const WorkOrderSummary({
    required this.id,
    required this.number,
    required this.title,
    required this.type,
    required this.priority,
    required this.status,
    required this.assetId,
    required this.assetTag,
    required this.assetName,
    required this.siteCode,
    required this.assigneeName,
    required this.dueAt,
    required this.laborHours,
    required this.isOverdue,
  });

  final String id;
  final String number;
  final String title;
  final String type;
  final String priority;
  final String status;
  final String assetId;
  final String assetTag;
  final String assetName;
  final String siteCode;
  final String? assigneeName;
  final DateTime? dueAt;
  final double laborHours;
  final bool isOverdue;

  factory WorkOrderSummary.fromJson(Map<String, dynamic> json) => WorkOrderSummary(
    id: json['id'] as String,
    number: json['number'] as String,
    title: json['title'] as String,
    type: json['type'] as String,
    priority: json['priority'] as String,
    status: json['status'] as String,
    assetId: json['assetId'] as String,
    assetTag: json['assetTag'] as String,
    assetName: json['assetName'] as String,
    siteCode: json['siteCode'] as String,
    assigneeName: json['assigneeName'] as String?,
    dueAt: json['dueAt'] == null ? null : DateTime.parse(json['dueAt'] as String),
    laborHours: (json['laborHours'] as num).toDouble(),
    isOverdue: json['isOverdue'] as bool,
  );
}

class WorkOrderComment {
  const WorkOrderComment({required this.id, required this.authorName, required this.body, required this.createdAt});

  final String id;
  final String authorName;
  final String body;
  final DateTime createdAt;

  factory WorkOrderComment.fromJson(Map<String, dynamic> json) => WorkOrderComment(
    id: json['id'] as String,
    authorName: json['authorName'] as String,
    body: json['body'] as String,
    createdAt: DateTime.parse(json['createdAt'] as String),
  );
}

class WorkOrderDetail extends WorkOrderSummary {
  const WorkOrderDetail({
    required super.id,
    required super.number,
    required super.title,
    required super.type,
    required super.priority,
    required super.status,
    required super.assetId,
    required super.assetTag,
    required super.assetName,
    required super.siteCode,
    required super.assigneeName,
    required super.dueAt,
    required super.laborHours,
    required super.isOverdue,
    required this.description,
    required this.allowedTransitions,
    required this.comments,
  });

  final String description;
  final List<String> allowedTransitions;
  final List<WorkOrderComment> comments;

  factory WorkOrderDetail.fromJson(Map<String, dynamic> json) {
    final summary = WorkOrderSummary.fromJson(json);
    return WorkOrderDetail(
      id: summary.id,
      number: summary.number,
      title: summary.title,
      type: summary.type,
      priority: summary.priority,
      status: summary.status,
      assetId: summary.assetId,
      assetTag: summary.assetTag,
      assetName: summary.assetName,
      siteCode: summary.siteCode,
      assigneeName: summary.assigneeName,
      dueAt: summary.dueAt,
      laborHours: summary.laborHours,
      isOverdue: summary.isOverdue,
      description: json['description'] as String,
      allowedTransitions: (json['allowedTransitions'] as List<dynamic>).cast<String>(),
      comments: (json['comments'] as List<dynamic>)
          .map((item) => WorkOrderComment.fromJson(item as Map<String, dynamic>))
          .toList(),
    );
  }
}

class WorkOrderPage {
  const WorkOrderPage({required this.items, required this.totalCount, this.fromCache = false});

  final List<WorkOrderSummary> items;
  final int totalCount;
  final bool fromCache;

  factory WorkOrderPage.fromJson(Map<String, dynamic> json, {bool fromCache = false}) => WorkOrderPage(
    items: (json['items'] as List<dynamic>).map((item) => WorkOrderSummary.fromJson(item as Map<String, dynamic>)).toList(),
    totalCount: json['totalCount'] as int,
    fromCache: fromCache,
  );
}

class WorkOrderDraft {
  const WorkOrderDraft({
    required this.title,
    required this.description,
    required this.assetId,
    required this.type,
    required this.priority,
    required this.dueAt,
    required this.assignToMe,
  });

  final String title;
  final String description;
  final String assetId;
  final String type;
  final String priority;
  final DateTime? dueAt;
  final bool assignToMe;

  Map<String, dynamic> toJson(String? userId) => {
    'title': title,
    'description': description,
    'assetId': assetId,
    'type': type,
    'priority': priority,
    'assignedToUserId': assignToMe ? userId : null,
    'dueAt': dueAt?.toUtc().toIso8601String(),
  };
}

String? validateWorkOrderTitle(String value) {
  final trimmed = value.trim();
  if (trimmed.isEmpty) {
    return 'Title is required.';
  }
  if (trimmed.length > 140) {
    return 'Title must be 140 characters or fewer.';
  }
  return null;
}
