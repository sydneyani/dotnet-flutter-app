import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/storage.dart';

const apiBaseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://localhost:8080');
const _sessionKey = 'meridian.session';

class CrewMember {
  const CrewMember({required this.id, required this.email, required this.displayName, required this.role});

  final String id;
  final String email;
  final String displayName;
  final String role;

  factory CrewMember.fromJson(Map<String, dynamic> json) => CrewMember(
    id: json['id'] as String,
    email: json['email'] as String,
    displayName: json['displayName'] as String,
    role: json['role'] as String,
  );

  Map<String, dynamic> toJson() => {'id': id, 'email': email, 'displayName': displayName, 'role': role};
}

class Session {
  const Session({
    required this.accessToken,
    required this.refreshToken,
    required this.accessTokenExpiresAt,
    required this.user,
  });

  final String accessToken;
  final String refreshToken;
  final DateTime accessTokenExpiresAt;
  final CrewMember user;

  factory Session.fromJson(Map<String, dynamic> json) => Session(
    accessToken: json['accessToken'] as String,
    refreshToken: json['refreshToken'] as String,
    accessTokenExpiresAt: DateTime.parse(json['accessTokenExpiresAt'] as String),
    user: CrewMember.fromJson(json['user'] as Map<String, dynamic>),
  );

  Map<String, dynamic> toJson() => {
    'accessToken': accessToken,
    'refreshToken': refreshToken,
    'accessTokenExpiresAt': accessTokenExpiresAt.toUtc().toIso8601String(),
    'user': user.toJson(),
  };
}

final storeProvider = Provider<KeyValueStore>((ref) => throw StateError('KeyValueStore was not provided.'));

final cacheProvider = Provider<JsonCache>((ref) => JsonCache(ref.watch(storeProvider)));

final rawDioProvider = Provider<Dio>((ref) {
  return Dio(
    BaseOptions(
      baseUrl: apiBaseUrl,
      connectTimeout: const Duration(seconds: 8),
      receiveTimeout: const Duration(seconds: 20),
      headers: {'Content-Type': 'application/json'},
    ),
  );
});

final sessionProvider = NotifierProvider<SessionController, Session?>(SessionController.new);

class SessionController extends Notifier<Session?> {
  @override
  Session? build() {
    final raw = ref.watch(storeProvider).read(_sessionKey);
    if (raw == null || raw.isEmpty) {
      return null;
    }
    return Session.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  Future<void> signIn(String email, String password) async {
    final dio = ref.read(rawDioProvider);
    final response = await dio.post<Map<String, dynamic>>(
      '/api/auth/login',
      data: {'email': email.trim(), 'password': password},
    );
    final session = Session.fromJson(response.data!);
    await _persist(session);
    state = session;
  }

  Future<String?> rotate() async {
    final current = state;
    if (current == null) {
      return null;
    }
    try {
      final response = await ref.read(rawDioProvider).post<Map<String, dynamic>>(
        '/api/auth/refresh',
        data: {'refreshToken': current.refreshToken},
      );
      final session = Session.fromJson(response.data!);
      await _persist(session);
      state = session;
      return session.accessToken;
    } on DioException {
      await signOut(notifyApi: false);
      return null;
    }
  }

  Future<void> signOut({bool notifyApi = true}) async {
    final current = state;
    state = null;
    await ref.read(storeProvider).delete(_sessionKey);
    if (notifyApi && current != null) {
      try {
        await ref.read(rawDioProvider).post<void>('/api/auth/logout', data: {'refreshToken': current.refreshToken});
      } on DioException {
        // The local session is already gone. A failed revoke just expires on the server.
      }
    }
  }

  Future<void> _persist(Session session) => ref.read(storeProvider).write(_sessionKey, jsonEncode(session.toJson()));
}

final apiProvider = Provider<MeridianApi>((ref) {
  return MeridianApi(
    dio: ref.watch(rawDioProvider),
    accessToken: () => ref.read(sessionProvider)?.accessToken,
    rotate: () => ref.read(sessionProvider.notifier).rotate(),
  );
});
