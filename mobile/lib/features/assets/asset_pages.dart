import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/widgets.dart';
import 'models.dart';
import 'repository.dart';

class AssetListPage extends ConsumerStatefulWidget {
  const AssetListPage({super.key});

  @override
  ConsumerState<AssetListPage> createState() => _AssetListPageState();
}

class _AssetListPageState extends ConsumerState<AssetListPage> {
  final _search = TextEditingController();

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final page = ref.watch(assetListProvider(_search.text.trim()));
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: TextField(
            controller: _search,
            decoration: const InputDecoration(labelText: 'Search tag, name, or site', prefixIcon: Icon(Icons.search)),
            onSubmitted: (_) => setState(() {}),
          ),
        ),
        Expanded(
          child: page.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => EmptyView(message: '$error'),
            data: (data) {
              if (data.items.isEmpty) {
                return const EmptyView(message: 'No assets match that search.');
              }
              return ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                itemCount: data.items.length + 1,
                separatorBuilder: (_, _) => const SizedBox(height: 8),
                itemBuilder: (context, index) {
                  if (index == 0) {
                    return OfflineBanner(visible: data.fromCache);
                  }
                  final asset = data.items[index - 1];
                  return AssetCard(asset: asset, onOpen: () => context.push('/assets/${asset.id}'));
                },
              );
            },
          ),
        ),
      ],
    );
  }
}

class AssetCard extends StatelessWidget {
  const AssetCard({super.key, required this.asset, required this.onOpen});

  final AssetSummary asset;
  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        title: Text('${asset.assetTag} · ${asset.name}'),
        subtitle: Text('${asset.siteCode} · ${prettyLabel(asset.category)}'),
        trailing: StatusPill(label: asset.criticality, urgent: asset.criticality == 'Critical'),
        onTap: onOpen,
      ),
    );
  }
}

class AssetDetailPage extends ConsumerWidget {
  const AssetDetailPage({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final asset = ref.watch(assetDetailProvider(id));
    return asset.when(
      loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
      error: (error, _) => Scaffold(appBar: AppBar(title: const Text('Asset')), body: EmptyView(message: '$error')),
      data: (item) => Scaffold(
        appBar: AppBar(title: Text(item.assetTag)),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(item.name, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              children: [
                StatusPill(label: item.status),
                StatusPill(label: item.criticality, urgent: item.criticality == 'Critical'),
              ],
            ),
            const SizedBox(height: 16),
            Text(item.description),
            const SizedBox(height: 12),
            Text('${item.siteName} (${item.siteCode})'),
            Text('${item.manufacturer} ${item.model} · ${item.serialNumber}'),
            Text('Installed ${item.installedOn}'),
            Text('Last inspection ${formatStamp(item.lastInspectedAt, empty: 'none on record')}'),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: item.status == 'Retired' ? null : () => context.push('/assets/${item.id}/inspections/new'),
              child: const Text('Log inspection'),
            ),
          ],
        ),
      ),
    );
  }
}
