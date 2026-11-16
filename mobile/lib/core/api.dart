import 'package:dio/dio.dart';

class ApiException implements Exception {
  ApiException(this.message, {this.statusCode, this.offline = false});

  final String message;
  final int? statusCode;
  final bool offline;

  @override
  String toString() => message;
}

typedef TokenReader = String? Function();
typedef TokenRotator = Future<String?> Function();

abstract class MeridianApiClient {
  Future<dynamic> get(String path, {Map<String, dynamic>? query});
  Future<dynamic> post(String path, Object? body, {bool authorize = true});
  Future<dynamic> put(String path, Object? body);
}

class MeridianApi implements MeridianApiClient {
  MeridianApi({required this.dio, required this.accessToken, required this.rotate});

  final Dio dio;
  final TokenReader accessToken;
  final TokenRotator rotate;

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) {
    return _send(() => dio.get(path, queryParameters: _clean(query), options: _headers()));
  }

  @override
  Future<dynamic> post(String path, Object? body, {bool authorize = true}) {
    return _send(
      () => dio.post(path, data: body, options: _headers(authorize: authorize)),
      retry: authorize,
    );
  }

  @override
  Future<dynamic> put(String path, Object? body) {
    return _send(() => dio.put(path, data: body, options: _headers()));
  }

  Future<dynamic> _send(Future<Response<dynamic>> Function() call, {bool retry = true}) async {
    try {
      final response = await call();
      return response.data;
    } on DioException catch (error) {
      if (retry && error.response?.statusCode == 401) {
        final renewed = await rotate();
        if (renewed != null) {
          return _send(call, retry: false);
        }
      }
      throw _map(error);
    }
  }

  Options _headers({bool authorize = true}) {
    final token = authorize ? accessToken() : null;
    return Options(headers: {if (token != null) 'Authorization': 'Bearer $token'});
  }

  Map<String, dynamic>? _clean(Map<String, dynamic>? query) {
    if (query == null) {
      return null;
    }
    return {for (final entry in query.entries) if (entry.value != null) entry.key: entry.value};
  }

  ApiException _map(DioException error) {
    final offline =
        error.type == DioExceptionType.connectionError ||
        error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout;
    final data = error.response?.data;
    final message = data is Map && data['detail'] is String
        ? data['detail'] as String
        : offline
        ? 'You are offline. Showing the last saved copy when one is available.'
        : 'The Meridian API could not complete that request.';
    return ApiException(message, statusCode: error.response?.statusCode, offline: offline);
  }
}
