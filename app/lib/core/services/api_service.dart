import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../../config.dart';
import '../../models/user_model.dart';

// Exception khas supaya page boleh memaparkan mesej error daripada API.
class ApiException implements Exception {
  const ApiException(this.message);

  final String message;

  @override
  String toString() => message;
}

// Semua request HTTP diletakkan dalam class ini.
// Page Flutter tidak perlu tahu cara bina URL, header atau JSON.
class ApiService {
  ApiService(
    this._preferences, {
    String? baseUrl,
  }) : _baseUrl = (baseUrl ?? apiBaseUrl).replaceFirst(RegExp(r'/$'), '');

  static const _tokenKey = 'auth_token';
  static const _refreshTokenKey = 'refresh_token';

  final SharedPreferences _preferences;
  final String _baseUrl;

  // Dua token diperlukan untuk menganggap session masih wujud.
  bool get isAuthenticated =>
      _preferences.getString(_tokenKey) != null &&
      _preferences.getString(_refreshTokenKey) != null;

  Uri _uri(String path) => Uri.parse('$_baseUrl$path');

  // Header untuk request yang menghantar JSON.
  Map<String, String> get _jsonHeaders => const {
        'Accept': 'application/json',
        'Content-Type': 'application/json; charset=UTF-8',
      };

  // Header protected menambah access JWT kepada request.
  Map<String, String> get _authHeaders {
    final token = _preferences.getString(_tokenKey);
    return {
      ..._jsonHeaders,
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  // Tukar body response JSON kepada object Dart.
  dynamic _decode(http.Response response) {
    if (response.body.trim().isEmpty) {
      return <String, dynamic>{};
    }
    return jsonDecode(response.body);
  }

  String _errorMessage(http.Response response) {
    try {
      final body = _decode(response);
      if (body is Map && body['message'] != null) {
        return body['message'].toString();
      }
    } catch (_) {
      // Guna mesej umum jika response bukan JSON yang sah.
    }
    return 'Request gagal (${response.statusCode}).';
  }

  // Status 2xx bermaksud request berjaya.
  void _throwIfFailed(http.Response response) {
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(_errorMessage(response));
    }
  }

  // Simpan kedua-dua token selepas login/register/refresh berjaya.
  Future<void> _saveAuth(AuthResponse result) async {
    await _preferences.setString(_tokenKey, result.token);
    await _preferences.setString(_refreshTokenKey, result.refreshToken);
  }

  Future<void> _clearAuth() async {
    await _preferences.remove(_tokenKey);
    await _preferences.remove(_refreshTokenKey);
  }

  // POST /api/auth/register
  Future<AuthResponse> register({
    required String name,
    required String email,
    required String password,
  }) async {
    final response = await http.post(
      _uri('/api/auth/register'),
      headers: _jsonHeaders,
      body: jsonEncode({
        'name': name,
        'email': email,
        'password': password,
      }),
    );

    _throwIfFailed(response);
    final result = AuthResponse.fromJson(
      Map<String, dynamic>.from(_decode(response) as Map),
    );
    await _saveAuth(result);
    return result;
  }

  // POST /api/auth/login
  Future<AuthResponse> login({
    required String email,
    required String password,
  }) async {
    final response = await http.post(
      _uri('/api/auth/login'),
      headers: _jsonHeaders,
      body: jsonEncode({
        'email': email,
        'password': password,
      }),
    );

    _throwIfFailed(response);
    final result = AuthResponse.fromJson(
      Map<String, dynamic>.from(_decode(response) as Map),
    );
    await _saveAuth(result);
    return result;
  }

  // Diperlukan apabila access JWT sudah expired.
  Future<bool> refreshAccessToken() async {
    final refreshToken = _preferences.getString(_refreshTokenKey);
    if (refreshToken == null) {
      return false;
    }

    try {
      final response = await http.post(
        _uri('/api/auth/refresh'),
        headers: _jsonHeaders,
        body: jsonEncode({'refreshToken': refreshToken}),
      );

      if (response.statusCode < 200 || response.statusCode >= 300) {
        await _clearAuth();
        return false;
      }

      final result = AuthResponse.fromJson(
        Map<String, dynamic>.from(_decode(response) as Map),
      );
      await _saveAuth(result);
      return true;
    } catch (_) {
      // Jangan buang token jika server cuma sementara tidak dapat dicapai.
      return false;
    }
  }

  // Jika request pertama menerima 401, refresh token sekali dan cuba semula.
  Future<http.Response> _authorizedGet(String path) async {
    var response = await http.get(
      _uri(path),
      headers: _authHeaders,
    );

    if (response.statusCode == 401 && await refreshAccessToken()) {
      response = await http.get(
        _uri(path),
        headers: _authHeaders,
      );
    }

    return response;
  }

  // GET /api/auth/me
  Future<UserModel> getMe() async {
    final response = await _authorizedGet('/api/auth/me');
    _throwIfFailed(response);
    return UserModel.fromJson(
      Map<String, dynamic>.from(_decode(response) as Map),
    );
  }

  // GET /api/users
  Future<List<UserModel>> getUsers() async {
    final response = await _authorizedGet('/api/users');
    _throwIfFailed(response);

    final body = _decode(response) as List;
    return body
        .map((item) => UserModel.fromJson(Map<String, dynamic>.from(item as Map)))
        .toList();
  }

  // Revoke refresh token di API dan buang token tempatan.
  Future<void> logout() async {
    final refreshToken = _preferences.getString(_refreshTokenKey);

    if (refreshToken != null) {
      try {
        await http.post(
          _uri('/api/auth/logout'),
          headers: _jsonHeaders,
          body: jsonEncode({'refreshToken': refreshToken}),
        );
      } catch (_) {
        // Token tempatan tetap dibuang walaupun API tidak dapat dicapai.
      }
    }

    await _clearAuth();
  }
}
