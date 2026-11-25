import 'package:material_ui/material_ui.dart';

import '../../pages/home_page.dart';
import '../../pages/login_page.dart';
import 'auth_state.dart';

// AuthGate menentukan page yang patut dilihat berdasarkan AuthState.
class AuthGate extends StatelessWidget {
  const AuthGate({super.key, required this.auth});

  final AuthState auth;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: auth,
      builder: (context, _) {
        // AuthState.notifyListeners() akan menyebabkan widget ini rebuild.
        if (auth.isAuthenticated) {
          return HomePage(auth: auth);
        }

        return LoginPage(auth: auth);
      },
    );
  }
}
