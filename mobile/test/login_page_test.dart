import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:meridian/core/storage.dart';
import 'package:meridian/features/auth/login_page.dart';
import 'package:meridian/features/auth/session.dart';

void main() {
  testWidgets('empty login explains what is missing', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [storeProvider.overrideWithValue(MemoryStore())],
        child: const MaterialApp(home: LoginPage()),
      ),
    );

    await tester.tap(find.text('Sign in'));
    await tester.pump();

    expect(find.text('Enter a work email.'), findsOneWidget);
    expect(find.text('Password must be at least 8 characters.'), findsOneWidget);
  });

  testWidgets('demo technician fills the sign-in form', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [storeProvider.overrideWithValue(MemoryStore())],
        child: const MaterialApp(home: LoginPage()),
      ),
    );

    await tester.tap(find.text('Use demo technician'));
    await tester.pump();

    expect(find.text('maya.okonkwo@meridian.demo'), findsOneWidget);
    expect(find.text('Meridian#demo1'), findsOneWidget);
  });
}
