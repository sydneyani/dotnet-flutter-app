import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

abstract class KeyValueStore {
  String? read(String key);
  Future<void> write(String key, String value);
  Future<void> delete(String key);
}

class MemoryStore implements KeyValueStore {
  final Map<String, String> values = {};

  @override
  String? read(String key) => values[key];

  @override
  Future<void> write(String key, String value) async {
    values[key] = value;
  }

  @override
  Future<void> delete(String key) async {
    values.remove(key);
  }
}

class PreferencesStore implements KeyValueStore {
  PreferencesStore(this._preferences);

  final SharedPreferences _preferences;

  @override
  String? read(String key) => _preferences.getString(key);

  @override
  Future<void> write(String key, String value) => _preferences.setString(key, value);

  @override
  Future<void> delete(String key) => _preferences.remove(key);
}

class JsonCache {
  JsonCache(this._store);

  final KeyValueStore _store;

  Future<void> write(String key, Object value) => _store.write('cache:$key', jsonEncode(value));

  Map<String, dynamic>? readMap(String key) {
    final raw = _store.read('cache:$key');
    if (raw == null || raw.isEmpty) {
      return null;
    }
    final decoded = jsonDecode(raw);
    return decoded is Map<String, dynamic> ? decoded : null;
  }
}
