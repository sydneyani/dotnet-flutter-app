import 'package:material_ui/material_ui.dart';

class MeridianColors {
  static const ink = Color(0xFF14222B);
  static const teal = Color(0xFF0F6E6B);
  static const tealDark = Color(0xFF0A4F4C);
  static const amber = Color(0xFFC47B0A);
  static const paper = Color(0xFFF3F5F4);
  static const danger = Color(0xFF9B2C2C);
  static const ok = Color(0xFF1F7A4D);
}

ThemeData meridianTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: MeridianColors.teal,
    primary: MeridianColors.teal,
    onPrimary: Colors.white,
    secondary: MeridianColors.amber,
    surface: Colors.white,
  );
  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: MeridianColors.paper,
    appBarTheme: const AppBarTheme(
      backgroundColor: MeridianColors.ink,
      foregroundColor: Colors.white,
      centerTitle: false,
    ),
    inputDecorationTheme: const InputDecorationTheme(
      border: OutlineInputBorder(),
      filled: true,
      fillColor: Colors.white,
    ),
    navigationBarTheme: NavigationBarThemeData(
      indicatorColor: MeridianColors.teal.withValues(alpha: 0.18),
      labelTextStyle: WidgetStateProperty.all(const TextStyle(fontWeight: FontWeight.w600)),
    ),
    cardTheme: CardThemeData(
      color: Colors.white,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: Color(0xFFE1E6E4)),
      ),
    ),
  );
}
