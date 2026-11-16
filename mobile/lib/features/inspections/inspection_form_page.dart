import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/api.dart';
import '../auth/session.dart';

class InspectionFormPage extends ConsumerStatefulWidget {
  const InspectionFormPage({super.key, required this.assetId});

  final String assetId;

  @override
  ConsumerState<InspectionFormPage> createState() => _InspectionFormPageState();
}

class _InspectionFormPageState extends ConsumerState<InspectionFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _notes = TextEditingController();
  final _findings = TextEditingController();
  String _result = 'Pass';
  DateTime? _nextDue;
  String? _error;
  var _busy = false;

  @override
  void dispose() {
    _notes.dispose();
    _findings.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Log inspection')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'Pass', label: Text('Pass')),
                ButtonSegment(value: 'Conditional', label: Text('Conditional')),
                ButtonSegment(value: 'Fail', label: Text('Fail')),
              ],
              selected: {_result},
              onSelectionChanged: (value) => setState(() => _result = value.first),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _notes,
              decoration: const InputDecoration(labelText: 'Notes'),
              minLines: 3,
              maxLines: 5,
              validator: (value) => (value == null || value.trim().isEmpty) ? 'Notes are required.' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _findings,
              decoration: const InputDecoration(labelText: 'Findings', helperText: 'Required when the result is Fail'),
              minLines: 2,
              maxLines: 4,
              validator: (value) {
                if (_result == 'Fail' && (value == null || value.trim().isEmpty)) {
                  return 'A failed inspection needs findings.';
                }
                return null;
              },
            ),
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Next due'),
              subtitle: Text(_nextDue == null ? 'Not set' : _nextDue!.toIso8601String().split('T').first),
              trailing: const Icon(Icons.event),
              onTap: () async {
                final picked = await showDatePicker(
                  context: context,
                  firstDate: DateTime.now(),
                  lastDate: DateTime.now().add(const Duration(days: 730)),
                  initialDate: DateTime.now().add(const Duration(days: 30)),
                );
                if (picked != null) {
                  setState(() => _nextDue = picked);
                }
              },
            ),
            if (_error != null) Text(_error!),
            const SizedBox(height: 12),
            FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? 'Saving…' : 'Save inspection')),
          ],
        ),
      ),
    );
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
      await ref.read(apiProvider).post('/api/inspections', {
        'assetId': widget.assetId,
        'inspectedAt': DateTime.now().toUtc().toIso8601String(),
        'result': _result,
        'notes': _notes.text.trim(),
        'findings': _findings.text.trim().isEmpty ? null : _findings.text.trim(),
        'nextDueOn': _nextDue?.toIso8601String().split('T').first,
      });
      if (mounted) {
        context.pop();
      }
    } on ApiException catch (error) {
      setState(() => _error = error.message);
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
