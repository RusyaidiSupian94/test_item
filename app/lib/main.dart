import 'package:material_ui/material_ui.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'core/auth/auth_gate.dart';
import 'core/auth/auth_state.dart';
import 'core/services/api_service.dart';

Future<void> main() async {
  // Diperlukan sebelum menggunakan plugin seperti shared_preferences.
  WidgetsFlutterBinding.ensureInitialized();

  // Ambil storage tempatan untuk menyimpan token.
  final preferences = await SharedPreferences.getInstance();
  final api = ApiService(preferences);
  final auth = AuthState(api: api);

  runApp(MyApp(auth: auth));
}

class MyApp extends StatelessWidget {
  const MyApp({super.key, required this.auth});

  final AuthState auth;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Flutter Auth Demo',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.indigo),
        useMaterial3: true,
      ),
      // AuthGate menentukan sama ada LoginPage atau HomePage dipaparkan.
      home: AuthGate(auth: auth),
    );
  }
}
