import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/storage.dart';
import '../auth/session.dart';
import 'models.dart';

final assetRepositoryProvider = Provider<AssetRepository>((ref) {
  return AssetRepository(ref.watch(apiProvider), ref.watch(cacheProvider));
});

final assetListProvider = FutureProvider.family<AssetPage, String>((ref, search) {
  return ref.watch(assetRepositoryProvider).list(search: search.isEmpty ? null : search);
});

final assetDetailProvider = FutureProvider.family<AssetSummary, String>((ref, id) {
  return ref.watch(assetRepositoryProvider).getById(id);
});

class AssetRepository {
  AssetRepository(this._api, this._cache);

  final MeridianApiClient _api;
  final JsonCache _cache;

  Future<AssetPage> list({String? search}) async {
    final key = 'assets:${search ?? ''}';
    try {
      final json = await _api.get('/api/assets', query: {'search': search, 'pageSize': 100});
      await _cache.write(key, json);
      return AssetPage.fromJson(json as Map<String, dynamic>);
    } on ApiException catch (error) {
      if (error.offline) {
        final cached = _cache.readMap(key);
        if (cached != null) {
          return AssetPage.fromJson(cached, fromCache: true);
        }
      }
      rethrow;
    }
  }

  Future<AssetSummary> getById(String id) async {
    final json = await _api.get('/api/assets/$id');
    return AssetSummary.fromJson(json as Map<String, dynamic>);
  }
}
