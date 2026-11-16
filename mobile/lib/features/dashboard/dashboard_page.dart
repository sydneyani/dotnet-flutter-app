import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:material_ui/material_ui.dart';

import '../../core/api.dart';
import '../../core/storage.dart';
import '../../core/widgets.dart';
import '../auth/session.dart';
import 'models.dart';

final dashboardRepositoryProvider = Provider<DashboardRepository>((ref) {
  return DashboardRepository(ref.watch(apiProvider), ref.watch(cacheProvider));
});

final dashboardProvider = FutureProvider<DashboardSummary>((ref) {
  return ref.watch(dashboardRepositoryProvider).load();
});

class DashboardRepository {
  DashboardRepository(this._api, this._cache);

  final MeridianApiClient _api;
  final JsonCache _cache;

  Future<DashboardSummary> load() async {
    const key = 'dashboard';
    try {
      final json = await _api.get('/api/dashboard/summary');
      await _cache.write(key, json);
      return DashboardSummary.fromJson(json as Map<String, dynamic>);
    } on ApiException catch (error) {
      if (error.offline) {
        final cached = _cache.readMap(key);
        if (cached != null) {
          return DashboardSummary.fromJson(cached, fromCache: true);
        }
      }
      rethrow;
    }
  }
}

class DashboardPage extends ConsumerWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summary = ref.watch(dashboardProvider);
    return summary.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => EmptyView(message: '$error'),
      data: (data) => DashboardView(summary: data),
    );
  }
}

class DashboardView extends StatelessWidget {
  const DashboardView({super.key, required this.summary});

  final DashboardSummary summary;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        OfflineBanner(visible: summary.fromCache),
        Text('This shift', style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 12),
        Wrap(
          spacing: 12,
          runSpacing: 12,
          children: [
            SizedBox(width: 160, child: MetricTile(label: 'Open', value: '${summary.openWorkOrders}')),
            SizedBox(width: 160, child: MetricTile(label: 'In progress', value: '${summary.inProgressWorkOrders}')),
            SizedBox(
              width: 160,
              child: MetricTile(label: 'Overdue', value: '${summary.overdueWorkOrders}', emphasis: summary.overdueWorkOrders > 0),
            ),
            SizedBox(width: 160, child: MetricTile(label: 'Critical assets', value: '${summary.criticalAssets}')),
          ],
        ),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Active work by priority', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 12),
                PriorityBars(
                  counts: [
                    for (final item in summary.activeWorkOrdersByPriority) (label: item.label, count: item.count),
                  ],
                ),
                const SizedBox(height: 8),
                Text(
                  '${summary.completedLast30Days} completed in 30 days · ${summary.averageLaborHoursCompleted.toStringAsFixed(1)} h average labor · ${summary.failedInspectionsLast30Days} failed inspections',
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
