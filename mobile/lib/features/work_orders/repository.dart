import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/storage.dart';
import '../auth/session.dart';
import 'models.dart';

final workOrderRepositoryProvider = Provider<WorkOrderRepository>((ref) {
  return WorkOrderRepository(ref.watch(apiProvider), ref.watch(cacheProvider));
});

class WorkOrderRepository {
  WorkOrderRepository(this._api, this._cache);

  final MeridianApiClient _api;
  final JsonCache _cache;

  Future<WorkOrderPage> list({String? status, String? search}) async {
    final key = 'work-orders:${status ?? 'all'}:${search ?? ''}';
    try {
      final json = await _api.get('/api/work-orders', query: {'status': status, 'search': search, 'pageSize': 50});
      await _cache.write(key, json);
      return WorkOrderPage.fromJson(json as Map<String, dynamic>);
    } on ApiException catch (error) {
      if (error.offline) {
        final cached = _cache.readMap(key);
        if (cached != null) {
          return WorkOrderPage.fromJson(cached, fromCache: true);
        }
      }
      rethrow;
    }
  }

  Future<WorkOrderDetail> getById(String id) async {
    final json = await _api.get('/api/work-orders/$id');
    return WorkOrderDetail.fromJson(json as Map<String, dynamic>);
  }

  Future<WorkOrderDetail> create(WorkOrderDraft draft, String? userId) async {
    final json = await _api.post('/api/work-orders', draft.toJson(userId));
    return WorkOrderDetail.fromJson(json as Map<String, dynamic>);
  }

  Future<WorkOrderDetail> update(String id, WorkOrderDraft draft, String? userId, double? laborHours) async {
    final body = draft.toJson(userId)..['laborHours'] = laborHours;
    final json = await _api.put('/api/work-orders/$id', body);
    return WorkOrderDetail.fromJson(json as Map<String, dynamic>);
  }

  Future<WorkOrderDetail> transition(String id, String status, {double? laborHours}) async {
    final json = await _api.post('/api/work-orders/$id/transitions', {'status': status, 'laborHours': laborHours});
    return WorkOrderDetail.fromJson(json as Map<String, dynamic>);
  }

  Future<WorkOrderDetail> comment(String id, String body) async {
    final json = await _api.post('/api/work-orders/$id/comments', {'body': body});
    return WorkOrderDetail.fromJson(json as Map<String, dynamic>);
  }
}
