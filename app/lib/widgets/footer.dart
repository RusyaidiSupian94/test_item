import 'package:material_ui/material_ui.dart';

// Widget kecil yang boleh digunakan semula di page lain.
class Footer extends StatelessWidget {
  const Footer({super.key});

  @override
  Widget build(BuildContext context) {
    return Text(
      'Flutter + .NET 10 Web API + SQLite',
      textAlign: TextAlign.center,
      style: Theme.of(context).textTheme.bodySmall,
    );
  }
}
