import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../auth/session.dart';

class HomeShell extends ConsumerWidget {
  const HomeShell({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final location = GoRouterState.of(context).uri.path;
    final index = location.startsWith('/assets') ? 2 : location.startsWith('/work-orders') ? 1 : 0;
    final name = ref.watch(sessionProvider)?.user.displayName ?? 'Crew';
    return Scaffold(
      appBar: AppBar(
        title: const Text('Meridian'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: Center(child: Text(name)),
          ),
          IconButton(
            tooltip: 'Sign out',
            onPressed: () => ref.read(sessionProvider.notifier).signOut(),
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: child,
      floatingActionButton: index == 1
          ? FloatingActionButton.extended(
              onPressed: () => context.push('/work-orders/new'),
              icon: const Icon(Icons.add),
              label: const Text('Work order'),
            )
          : null,
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) {
          switch (value) {
            case 1:
              context.go('/work-orders');
            case 2:
              context.go('/assets');
            default:
              context.go('/dashboard');
          }
        },
        destinations: const [
          NavigationDestination(icon: Icon(Icons.dashboard_outlined), selectedIcon: Icon(Icons.dashboard), label: 'Board'),
          NavigationDestination(icon: Icon(Icons.assignment_outlined), selectedIcon: Icon(Icons.assignment), label: 'Work'),
          NavigationDestination(icon: Icon(Icons.precision_manufacturing_outlined), selectedIcon: Icon(Icons.precision_manufacturing), label: 'Assets'),
        ],
      ),
    );
  }
}
