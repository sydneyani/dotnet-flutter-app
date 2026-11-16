import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/widgets.dart';
import '../assets/models.dart';
import '../assets/repository.dart';
import '../auth/session.dart';
import 'models.dart';
import 'repository.dart';

final workOrderListProvider = FutureProvider.family<WorkOrderPage, String>((ref, filter) {
  final separator = filter.indexOf('|');
  final status = filter.substring(0, separator);
  final search = filter.substring(separator + 1);
  return ref.watch(workOrderRepositoryProvider).list(
    status: status.isEmpty ? null : status,
    search: search.isEmpty ? null : search,
  );
});

final workOrderDetailProvider = FutureProvider.family<WorkOrderDetail, String>((ref, id) {
  return ref.watch(workOrderRepositoryProvider).getById(id);
});

class WorkOrderListPage extends ConsumerStatefulWidget {
  const WorkOrderListPage({super.key});

  @override
  ConsumerState<WorkOrderListPage> createState() => _WorkOrderListPageState();
}

class _WorkOrderListPageState extends ConsumerState<WorkOrderListPage> {
  final _search = TextEditingController();
  String _status = '';

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final page = ref.watch(workOrderListProvider('$_status|${_search.text.trim()}'));
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
          child: TextField(
            controller: _search,
            decoration: const InputDecoration(labelText: 'Search number, title, or tag', prefixIcon: Icon(Icons.search)),
            onSubmitted: (_) => setState(() {}),
          ),
        ),
        SizedBox(
          height: 56,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            children: [
              for (final choice in const ['', 'Open', 'InProgress', 'WaitingParts', 'Completed'])
                Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: FilterChip(
                    label: Text(choice.isEmpty ? 'All' : prettyLabel(choice)),
                    selected: _status == choice,
                    onSelected: (_) => setState(() => _status = choice),
                  ),
                ),
            ],
          ),
        ),
        Expanded(
          child: page.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => EmptyView(message: '$error'),
            data: (data) => WorkOrderListView(
              orders: data.items,
              fromCache: data.fromCache,
              onOpen: (id) => context.push('/work-orders/$id'),
            ),
          ),
        ),
      ],
    );
  }
}

class WorkOrderListView extends StatelessWidget {
  const WorkOrderListView({super.key, required this.orders, required this.onOpen, this.fromCache = false});

  final List<WorkOrderSummary> orders;
  final ValueChanged<String> onOpen;
  final bool fromCache;

  @override
  Widget build(BuildContext context) {
    if (orders.isEmpty) {
      return const EmptyView(message: 'No work orders match this filter.');
    }
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: orders.length + 1,
      separatorBuilder: (_, _) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        if (index == 0) {
          return OfflineBanner(visible: fromCache);
        }
        final order = orders[index - 1];
        return Card(
          child: ListTile(
            title: Text(order.title),
            subtitle: Text('${order.number} · ${order.assetTag} · ${order.siteCode}\nDue ${formatStamp(order.dueAt, empty: 'whenever')}'),
            isThreeLine: true,
            trailing: StatusPill(label: order.isOverdue ? 'Urgent' : order.status, urgent: order.isOverdue),
            onTap: () => onOpen(order.id),
          ),
        );
      },
    );
  }
}

class WorkOrderDetailPage extends ConsumerWidget {
  const WorkOrderDetailPage({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detail = ref.watch(workOrderDetailProvider(id));
    return detail.when(
      loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
      error: (error, _) => Scaffold(appBar: AppBar(title: const Text('Work order')), body: EmptyView(message: '$error')),
      data: (order) => _DetailScaffold(order: order),
    );
  }
}

class _DetailScaffold extends ConsumerStatefulWidget {
  const _DetailScaffold({required this.order});

  final WorkOrderDetail order;

  @override
  ConsumerState<_DetailScaffold> createState() => _DetailScaffoldState();
}

class _DetailScaffoldState extends ConsumerState<_DetailScaffold> {
  final _comment = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final order = widget.order;
    return Scaffold(
      appBar: AppBar(
        title: Text(order.number),
        actions: [
          IconButton(
            tooltip: 'Edit',
            onPressed: order.allowedTransitions.isEmpty && order.status != 'Open' && order.status != 'InProgress' && order.status != 'WaitingParts'
                ? null
                : () => context.push('/work-orders/${order.id}/edit'),
            icon: const Icon(Icons.edit_outlined),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Row(
            children: [
              StatusPill(label: order.status),
              const SizedBox(width: 8),
              StatusPill(label: order.priority, urgent: order.isOverdue),
            ],
          ),
          const SizedBox(height: 12),
          Text(order.title, style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 8),
          Text(order.description),
          const SizedBox(height: 12),
          Text('${order.assetTag} · ${order.assetName} · ${order.siteCode}'),
          Text('Assignee ${order.assigneeName ?? 'unassigned'} · ${order.laborHours.toStringAsFixed(1)} h · due ${formatStamp(order.dueAt)}'),
          const SizedBox(height: 16),
          Wrap(
            spacing: 8,
            children: [
              for (final next in order.allowedTransitions)
                FilledButton.tonal(onPressed: () => _transition(next), child: Text(prettyLabel(next))),
            ],
          ),
          if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!)),
          const SizedBox(height: 24),
          Text('Handoff log', style: Theme.of(context).textTheme.titleMedium),
          for (final comment in order.comments)
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(comment.authorName),
              subtitle: Text(comment.body),
              trailing: Text(formatStamp(comment.createdAt)),
            ),
          TextField(
            controller: _comment,
            decoration: const InputDecoration(labelText: 'Add a note for the next shift'),
            minLines: 2,
            maxLines: 4,
          ),
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerRight,
            child: FilledButton(onPressed: _addComment, child: const Text('Add note')),
          ),
        ],
      ),
    );
  }

  Future<void> _transition(String status) async {
    double? hours;
    if (status == 'Completed') {
      hours = await _askHours();
      if (hours == null) {
        return;
      }
    }
    try {
      await ref.read(workOrderRepositoryProvider).transition(widget.order.id, status, laborHours: hours);
      ref.invalidate(workOrderDetailProvider(widget.order.id));
      ref.invalidate(workOrderListProvider);
    } on Exception catch (error) {
      setState(() => _error = '$error');
    }
  }

  Future<double?> _askHours() async {
    final controller = TextEditingController(text: widget.order.laborHours == 0 ? '' : widget.order.laborHours.toString());
    final hours = await showDialog<double>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Labor hours'),
        content: TextField(
          controller: controller,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: const InputDecoration(labelText: 'Hours on this job'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          FilledButton(
            onPressed: () => Navigator.pop(context, double.tryParse(controller.text)),
            child: const Text('Complete'),
          ),
        ],
      ),
    );
    controller.dispose();
    return hours;
  }

  Future<void> _addComment() async {
    final body = _comment.text.trim();
    if (body.isEmpty) {
      return;
    }
    try {
      await ref.read(workOrderRepositoryProvider).comment(widget.order.id, body);
      _comment.clear();
      ref.invalidate(workOrderDetailProvider(widget.order.id));
    } on Exception catch (error) {
      setState(() => _error = '$error');
    }
  }
}

class WorkOrderFormPage extends ConsumerWidget {
  const WorkOrderFormPage({super.key, this.id});

  final String? id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final assets = ref.watch(assetChoicesProvider);
    final AsyncValue<WorkOrderDetail?> existing = id == null
        ? const AsyncData<WorkOrderDetail?>(null)
        : ref.watch(workOrderDetailProvider(id!)).whenData<WorkOrderDetail?>((value) => value);
    if (assets.isLoading || existing.isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (assets.hasError || existing.hasError) {
      return Scaffold(appBar: AppBar(), body: EmptyView(message: '${assets.error ?? existing.error}'));
    }
    return Scaffold(
      appBar: AppBar(title: Text(id == null ? 'New work order' : 'Edit work order')),
      body: WorkOrderForm(
        assets: assets.requireValue,
        initial: existing.requireValue,
        onSubmit: (draft) async {
          final userId = ref.read(sessionProvider)?.user.id;
          final repository = ref.read(workOrderRepositoryProvider);
          if (id == null) {
            await repository.create(draft, userId);
          } else {
            await repository.update(id!, draft, userId, existing.requireValue?.laborHours);
          }
          ref.invalidate(workOrderListProvider);
          if (context.mounted) {
            context.pop();
          }
        },
      ),
    );
  }
}

final assetChoicesProvider = FutureProvider<List<AssetSummary>>((ref) async {
  final page = await ref.watch(assetRepositoryProvider).list();
  return page.items;
});

class WorkOrderForm extends StatefulWidget {
  const WorkOrderForm({super.key, required this.assets, required this.onSubmit, this.initial});

  final List<AssetSummary> assets;
  final WorkOrderDetail? initial;
  final Future<void> Function(WorkOrderDraft draft) onSubmit;

  @override
  State<WorkOrderForm> createState() => _WorkOrderFormState();
}

class _WorkOrderFormState extends State<WorkOrderForm> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _title;
  late final TextEditingController _description;
  late String _assetId;
  late String _type;
  late String _priority;
  late bool _assignToMe;
  DateTime? _dueAt;
  String? _error;
  var _busy = false;

  @override
  void initState() {
    super.initState();
    final initial = widget.initial;
    _title = TextEditingController(text: initial?.title ?? '');
    _description = TextEditingController(text: initial?.description ?? '');
    _assetId = initial?.assetId ?? (widget.assets.isEmpty ? '' : widget.assets.first.id);
    _type = initial?.type ?? 'Corrective';
    _priority = initial?.priority ?? 'Medium';
    _assignToMe = initial?.assigneeName != null || initial == null;
    _dueAt = initial?.dueAt;
  }

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextFormField(
            controller: _title,
            decoration: const InputDecoration(labelText: 'Title'),
            validator: (value) => validateWorkOrderTitle(value ?? ''),
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _description,
            decoration: const InputDecoration(labelText: 'What needs to happen'),
            minLines: 3,
            maxLines: 5,
            validator: (value) => (value == null || value.trim().isEmpty) ? 'Description is required.' : null,
          ),
          const SizedBox(height: 12),
          DropdownButtonFormField<String>(
            initialValue: _assetId.isEmpty ? null : _assetId,
            decoration: const InputDecoration(labelText: 'Asset'),
            items: [
              for (final asset in widget.assets) DropdownMenuItem(value: asset.id, child: Text('${asset.assetTag} · ${asset.name}')),
            ],
            onChanged: (value) => setState(() => _assetId = value ?? ''),
            validator: (value) => value == null || value.isEmpty ? 'Choose an asset.' : null,
          ),
          const SizedBox(height: 12),
          DropdownButtonFormField<String>(
            initialValue: _type,
            decoration: const InputDecoration(labelText: 'Type'),
            items: const [
              DropdownMenuItem(value: 'Preventive', child: Text('Preventive')),
              DropdownMenuItem(value: 'Corrective', child: Text('Corrective')),
              DropdownMenuItem(value: 'Inspection', child: Text('Inspection')),
              DropdownMenuItem(value: 'Emergency', child: Text('Emergency')),
            ],
            onChanged: (value) => setState(() => _type = value ?? _type),
          ),
          const SizedBox(height: 12),
          DropdownButtonFormField<String>(
            initialValue: _priority,
            decoration: const InputDecoration(labelText: 'Priority'),
            items: const [
              DropdownMenuItem(value: 'Low', child: Text('Low')),
              DropdownMenuItem(value: 'Medium', child: Text('Medium')),
              DropdownMenuItem(value: 'High', child: Text('High')),
              DropdownMenuItem(value: 'Urgent', child: Text('Urgent')),
            ],
            onChanged: (value) => setState(() => _priority = value ?? _priority),
          ),
          const SizedBox(height: 12),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Assign to me'),
            value: _assignToMe,
            onChanged: (value) => setState(() => _assignToMe = value),
          ),
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Due'),
            subtitle: Text(formatStamp(_dueAt)),
            trailing: const Icon(Icons.event),
            onTap: () async {
              final picked = await showDatePicker(
                context: context,
                firstDate: DateTime.now().subtract(const Duration(days: 1)),
                lastDate: DateTime.now().add(const Duration(days: 365)),
                initialDate: _dueAt ?? DateTime.now().add(const Duration(days: 1)),
              );
              if (picked != null) {
                setState(() => _dueAt = picked);
              }
            },
          ),
          if (_error != null) Text(_error!, style: const TextStyle(color: Colors.red)),
          const SizedBox(height: 12),
          FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? 'Saving…' : 'Save work order')),
        ],
      ),
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    setState(() => _busy = true);
    try {
      await widget.onSubmit(
        WorkOrderDraft(
          title: _title.text.trim(),
          description: _description.text.trim(),
          assetId: _assetId,
          type: _type,
          priority: _priority,
          dueAt: _dueAt,
          assignToMe: _assignToMe,
        ),
      );
    } on Exception catch (error) {
      setState(() => _error = '$error');
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }
}
