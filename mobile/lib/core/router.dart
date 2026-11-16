import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../features/assets/asset_pages.dart';
import '../features/auth/login_page.dart';
import '../features/auth/session.dart';
import '../features/dashboard/dashboard_page.dart';
import '../features/inspections/inspection_form_page.dart';
import '../features/shell/home_shell.dart';
import '../features/work_orders/work_order_pages.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final refresh = ValueNotifier<int>(0);
  ref.listen(sessionProvider, (_, _) => refresh.value++);
  ref.onDispose(refresh.dispose);

  return GoRouter(
    initialLocation: '/dashboard',
    refreshListenable: refresh,
    redirect: (context, state) {
      final signedIn = ref.read(sessionProvider) != null;
      final loggingIn = state.matchedLocation == '/login';
      if (!signedIn && !loggingIn) {
        return '/login';
      }
      if (signedIn && loggingIn) {
        return '/dashboard';
      }
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const LoginPage()),
      ShellRoute(
        builder: (context, state, child) => HomeShell(child: child),
        routes: [
          GoRoute(path: '/dashboard', builder: (context, state) => const DashboardPage()),
          GoRoute(path: '/work-orders', builder: (context, state) => const WorkOrderListPage()),
          GoRoute(path: '/assets', builder: (context, state) => const AssetListPage()),
        ],
      ),
      GoRoute(path: '/work-orders/new', builder: (context, state) => const WorkOrderFormPage()),
      GoRoute(
        path: '/work-orders/:id',
        builder: (context, state) => WorkOrderDetailPage(id: state.pathParameters['id']!),
        routes: [
          GoRoute(
            path: 'edit',
            builder: (context, state) => WorkOrderFormPage(id: state.pathParameters['id']),
          ),
        ],
      ),
      GoRoute(
        path: '/assets/:id',
        builder: (context, state) => AssetDetailPage(id: state.pathParameters['id']!),
        routes: [
          GoRoute(
            path: 'inspections/new',
            builder: (context, state) => InspectionFormPage(assetId: state.pathParameters['id']!),
          ),
        ],
      ),
    ],
  );
});
