import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import 'core/router.dart';
import 'core/theme.dart';

class MeridianApp extends ConsumerWidget {
  const MeridianApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp.router(
      title: 'Meridian',
      theme: meridianTheme(),
      routerConfig: ref.watch(routerProvider),
    );
  }
}
