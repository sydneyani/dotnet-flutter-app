import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:meridian/core/api.dart';
import 'package:meridian/core/storage.dart';
import 'package:meridian/core/theme.dart';
import 'package:meridian/core/widgets.dart';
import 'package:meridian/features/dashboard/models.dart';
import 'package:meridian/features/dashboard/dashboard_page.dart';
import 'package:meridian/features/work_orders/models.dart';
import 'package:meridian/features/work_orders/repository.dart';
import 'package:meridian/features/work_orders/work_order_pages.dart';

void main() {
  test('title validation rejects a blank job', () {
    expect(validateWorkOrderTitle('  '), 'Title is required.');
    expect(validateWorkOrderTitle('Sample oil on TR-1044'), isNull);
  });

  test('offline list falls back to the cached page', () async {
    final store = MemoryStore();
    final cache = JsonCache(store);
    await cache.write('work-orders:Open:', {
      'items': [
        {
          'id': 'wo-1',
          'number': 'WO-00008',
          'title': 'Investigate 87T alarm on REL-87T',
          'type': 'Emergency',
          'priority': 'Urgent',
          'status': 'Open',
          'assetId': 'asset-1',
          'assetTag': 'REL-87T',
          'assetName': 'Transformer differential relay',
          'siteCode': 'SUB-NR-04',
          'assigneeName': 'Maya Okonkwo',
          'dueAt': '2026-04-02T09:00:00Z',
          'laborHours': 0,
          'isOverdue': true,
        },
      ],
      'totalCount': 1,
    });
    final repository = WorkOrderRepository(OfflineApi(), cache);

    final page = await repository.list(status: 'Open');

    expect(page.fromCache, isTrue);
    expect(page.items.single.number, 'WO-00008');
  });

  testWidgets('work order card shows the tag and overdue state', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: meridianTheme(),
        home: Scaffold(
          body: WorkOrderListView(
            orders: const [
              WorkOrderSummary(
                id: 'wo-1',
                number: 'WO-00003',
                title: 'Replace mechanical seal on PMP-CW-3',
                type: 'Corrective',
                priority: 'Urgent',
                status: 'WaitingParts',
                assetId: 'asset-1',
                assetTag: 'PMP-CW-3',
                assetName: 'Circulating water pump 3',
                siteCode: 'GEN-CB-01',
                assigneeName: 'Luis Ortega',
                dueAt: null,
                laborHours: 2,
                isOverdue: true,
              ),
            ],
            onOpen: (_) {},
          ),
        ),
      ),
    );

    expect(find.text('Replace mechanical seal on PMP-CW-3'), findsOneWidget);
    expect(find.textContaining('PMP-CW-3'), findsWidgets);
    expect(find.text('Urgent'), findsOneWidget);
  });

  testWidgets('dashboard shows the overdue count', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: meridianTheme(),
        home: const Scaffold(
          body: DashboardView(
            summary: DashboardSummary(
              openWorkOrders: 3,
              inProgressWorkOrders: 2,
              waitingPartsWorkOrders: 2,
              overdueWorkOrders: 2,
              completedLast30Days: 2,
              averageLaborHoursCompleted: 3,
              assetsInService: 8,
              assetsOutOfService: 1,
              criticalAssets: 2,
              failedInspectionsLast30Days: 1,
              activeWorkOrdersByPriority: [
                CountByLabel(label: 'Low', count: 1),
                CountByLabel(label: 'Urgent', count: 2),
              ],
            ),
          ),
        ),
      ),
    );

    expect(find.text('Overdue'), findsOneWidget);
    expect(find.text('2'), findsWidgets);
    expect(find.text('Urgent'), findsOneWidget);
  });

  testWidgets('offline banner is hidden until the cache is in use', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: Scaffold(body: OfflineBanner(visible: false))));
    expect(find.textContaining('last copy'), findsNothing);

    await tester.pumpWidget(const MaterialApp(home: Scaffold(body: OfflineBanner(visible: true))));
    expect(find.textContaining('last copy'), findsOneWidget);
  });
}

class OfflineApi implements MeridianApiClient {
  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    throw ApiException('offline', offline: true);
  }

  @override
  Future<dynamic> post(String path, Object? body, {bool authorize = true}) async {
    throw ApiException('offline', offline: true);
  }

  @override
  Future<dynamic> put(String path, Object? body) async {
    throw ApiException('offline', offline: true);
  }
}
