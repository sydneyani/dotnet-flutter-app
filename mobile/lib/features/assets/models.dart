class AssetSummary {
  const AssetSummary({
    required this.id,
    required this.assetTag,
    required this.name,
    required this.category,
    required this.status,
    required this.criticality,
    required this.siteCode,
    required this.siteName,
    required this.manufacturer,
    required this.model,
    required this.serialNumber,
    required this.installedOn,
    required this.description,
    required this.lastInspectedAt,
  });

  final String id;
  final String assetTag;
  final String name;
  final String category;
  final String status;
  final String criticality;
  final String siteCode;
  final String siteName;
  final String manufacturer;
  final String model;
  final String serialNumber;
  final String installedOn;
  final String description;
  final DateTime? lastInspectedAt;

  factory AssetSummary.fromJson(Map<String, dynamic> json) => AssetSummary(
    id: json['id'] as String,
    assetTag: json['assetTag'] as String,
    name: json['name'] as String,
    category: json['category'] as String,
    status: json['status'] as String,
    criticality: json['criticality'] as String,
    siteCode: json['siteCode'] as String,
    siteName: json['siteName'] as String,
    manufacturer: json['manufacturer'] as String,
    model: json['model'] as String,
    serialNumber: json['serialNumber'] as String,
    installedOn: json['installedOn'] as String,
    description: json['description'] as String,
    lastInspectedAt: json['lastInspectedAt'] == null ? null : DateTime.parse(json['lastInspectedAt'] as String),
  );
}

class AssetPage {
  const AssetPage({required this.items, required this.totalCount, this.fromCache = false});

  final List<AssetSummary> items;
  final int totalCount;
  final bool fromCache;

  factory AssetPage.fromJson(Map<String, dynamic> json, {bool fromCache = false}) => AssetPage(
    items: (json['items'] as List<dynamic>).map((item) => AssetSummary.fromJson(item as Map<String, dynamic>)).toList(),
    totalCount: json['totalCount'] as int,
    fromCache: fromCache,
  );
}
