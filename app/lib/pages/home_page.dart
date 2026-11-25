import 'package:material_ui/material_ui.dart';

import '../core/auth/auth_state.dart';
import 'dashboard_page.dart';

// HomePage ialah shell utama selepas login.
// Kandungan dashboard dipisahkan ke DashboardPage.
class HomePage extends StatelessWidget {
  const HomePage({super.key, required this.auth});

  final AuthState auth;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Home'),
        actions: [
          IconButton(
            tooltip: 'Logout',
            onPressed: () {
              auth.logout();
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: DashboardPage(auth: auth),
    );
  }
}
