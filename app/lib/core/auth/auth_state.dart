import 'package:flutter/foundation.dart';

import '../services/api_service.dart';

// AuthState menyimpan status login di satu tempat.
// Page hanya perlu panggil login(), register() atau logout().
class AuthState extends ChangeNotifier {
  AuthState({required this.api}) : _isAuthenticated = api.isAuthenticated;

  final ApiService api;
  bool _isAuthenticated;

  bool get isAuthenticated => _isAuthenticated;

  // Login berjaya -> simpan status authenticated -> AuthGate rebuild.
  Future<void> login({
    required String email,
    required String password,
  }) async {
    await api.login(email: email, password: password);
    _isAuthenticated = true;
    notifyListeners();
  }

  // Register terus menghasilkan session, jadi user boleh masuk ke Home.
  Future<void> register({
    required String name,
    required String email,
    required String password,
  }) async {
    await api.register(name: name, email: email, password: password);
    _isAuthenticated = true;
    notifyListeners();
  }

  // Logout API dan tukar semula status kepada tidak login.
  Future<void> logout() async {
    await api.logout();
    _isAuthenticated = false;
    notifyListeners();
  }
}
