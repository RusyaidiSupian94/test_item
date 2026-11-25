import 'package:material_ui/material_ui.dart';

import '../core/auth/auth_state.dart';
import '../core/services/api_service.dart';
import '../models/user_model.dart';
import '../widgets/footer.dart';

// DashboardPage memanggil endpoint protected dan memaparkan user daripada SQLite.
class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key, required this.auth});

  final AuthState auth;

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  UserModel? _currentUser;
  List<UserModel> _users = const [];
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    // Load data apabila dashboard mula dibina.
    _loadData();
  }

  // Panggil user semasa dan senarai semua user daripada API.
  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      // ApiService akan cuba refresh token jika access JWT sudah tamat.
      final currentUser = await widget.auth.api.getMe();
      final users = await widget.auth.api.getUsers();

      if (!mounted) return;
      setState(() {
        _currentUser = currentUser;
        _users = users;
        _isLoading = false;
      });
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() {
        _errorMessage = error.message;
        _isLoading = false;
      });
    }
  }

  String _formatDate(DateTime value) {
    final local = value.toLocal();
    String twoDigits(int number) => number.toString().padLeft(2, '0');
    return '${local.day}/${local.month}/${local.year} '
        '${twoDigits(local.hour)}:${twoDigits(local.minute)}';
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: _isLoading
                  ? const LinearProgressIndicator()
                  : Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Selamat datang, ${_currentUser?.name ?? ''}!',
                          style: Theme.of(context).textTheme.headlineSmall,
                        ),
                        const SizedBox(height: 8),
                        Text(_currentUser?.email ?? ''),
                        const SizedBox(height: 8),
                        const Text(
                          'Data ini datang daripada endpoint protected Web API.',
                        ),
                      ],
                    ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Data users dalam SQLite',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 8),
          if (_errorMessage != null)
            Card(
              color: Theme.of(context).colorScheme.errorContainer,
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Text(_errorMessage!),
              ),
            )
          else if (!_isLoading && _users.isEmpty)
            const Card(
              child: Padding(
                padding: EdgeInsets.all(16),
                child: Text('Belum ada user.'),
              ),
            )
          else if (!_isLoading)
            Card(
              clipBehavior: Clip.antiAlias,
              child: SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: DataTable(
                  columns: const [
                    DataColumn(label: Text('ID')),
                    DataColumn(label: Text('Nama')),
                    DataColumn(label: Text('Email')),
                    DataColumn(label: Text('Dicipta')),
                  ],
                  rows: _users
                      .map(
                        (user) => DataRow(
                          cells: [
                            DataCell(Text(user.id.toString())),
                            DataCell(Text(user.name)),
                            DataCell(Text(user.email)),
                            DataCell(Text(_formatDate(user.createdAt))),
                          ],
                        ),
                      )
                      .toList(),
                ),
              ),
            ),
          const SizedBox(height: 24),
          const Footer(),
        ],
      ),
    );
  }
}
