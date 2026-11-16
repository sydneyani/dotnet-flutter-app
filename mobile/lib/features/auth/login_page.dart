import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/theme.dart';
import 'session.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  var _busy = false;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: ListView(
              padding: const EdgeInsets.all(24),
              children: [
                const SizedBox(height: 32),
                Text('MERIDIAN', style: Theme.of(context).textTheme.headlineMedium?.copyWith(letterSpacing: 2, color: MeridianColors.ink)),
                const SizedBox(height: 8),
                Text('Field operations', style: Theme.of(context).textTheme.titleMedium?.copyWith(color: MeridianColors.tealDark)),
                const SizedBox(height: 28),
                Form(
                  key: _formKey,
                  child: Column(
                    children: [
                      TextFormField(
                        controller: _email,
                        keyboardType: TextInputType.emailAddress,
                        autofillHints: const [AutofillHints.username],
                        decoration: const InputDecoration(labelText: 'Email'),
                        validator: (value) {
                          if (value == null || !value.contains('@')) {
                            return 'Enter a work email.';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: _password,
                        obscureText: true,
                        autofillHints: const [AutofillHints.password],
                        decoration: const InputDecoration(labelText: 'Password'),
                        validator: (value) => (value == null || value.length < 8) ? 'Password must be at least 8 characters.' : null,
                      ),
                    ],
                  ),
                ),
                if (_error != null) ...[
                  const SizedBox(height: 12),
                  Text(_error!, style: const TextStyle(color: MeridianColors.danger)),
                ],
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _busy ? null : _submit,
                  child: Text(_busy ? 'Signing in…' : 'Sign in'),
                ),
                const SizedBox(height: 8),
                TextButton(onPressed: _fillDemo, child: const Text('Use demo technician')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  void _fillDemo() {
    setState(() {
      _email.text = 'maya.okonkwo@meridian.demo';
      _password.text = 'Meridian#demo1';
      _error = null;
    });
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(sessionProvider.notifier).signIn(_email.text, _password.text);
    } on DioException catch (error) {
      final data = error.response?.data;
      setState(() {
        _error = data is Map && data['detail'] is String ? data['detail'] as String : 'Sign-in failed.';
      });
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
