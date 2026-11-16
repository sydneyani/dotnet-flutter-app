import 'package:intl/intl.dart';
import 'package:material_ui/material_ui.dart';

import 'theme.dart';

class OfflineBanner extends StatelessWidget {
  const OfflineBanner({super.key, required this.visible});

  final bool visible;

  @override
  Widget build(BuildContext context) {
    if (!visible) {
      return const SizedBox.shrink();
    }
    return Material(
      color: MeridianColors.amber.withValues(alpha: 0.16),
      child: const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        child: Row(
          children: [
            Icon(Icons.cloud_off, size: 18, color: MeridianColors.ink),
            SizedBox(width: 8),
            Expanded(child: Text('Showing the last copy saved on this device.')),
          ],
        ),
      ),
    );
  }
}

class StatusPill extends StatelessWidget {
  const StatusPill({super.key, required this.label, this.urgent = false});

  final String label;
  final bool urgent;

  @override
  Widget build(BuildContext context) {
    final color = switch (label) {
      'Completed' || 'Pass' || 'InService' => MeridianColors.ok,
      'Cancelled' || 'Fail' || 'OutOfService' || 'Urgent' || 'Critical' => MeridianColors.danger,
      'WaitingParts' || 'Conditional' || 'High' => MeridianColors.amber,
      _ => MeridianColors.teal,
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        prettyLabel(label),
        style: TextStyle(color: urgent ? MeridianColors.danger : color, fontWeight: FontWeight.w700, fontSize: 12),
      ),
    );
  }

  }

String prettyLabel(String label) {
  return label.replaceAllMapped(RegExp(r'([a-z])([A-Z])'), (match) => '${match[1]} ${match[2]}');
}

class EmptyView extends StatelessWidget {
  const EmptyView({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(padding: const EdgeInsets.all(24), child: Text(message, textAlign: TextAlign.center)),
    );
  }
}

class MetricTile extends StatelessWidget {
  const MetricTile({super.key, required this.label, required this.value, this.emphasis = false});

  final String label;
  final String value;
  final bool emphasis;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: Theme.of(context).textTheme.labelLarge),
            const SizedBox(height: 8),
            Text(
              value,
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                color: emphasis ? MeridianColors.danger : MeridianColors.ink,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class PriorityBars extends StatelessWidget {
  const PriorityBars({super.key, required this.counts});

  final List<({String label, int count})> counts;

  @override
  Widget build(BuildContext context) {
    final maxCount = counts.fold<int>(1, (max, item) => item.count > max ? item.count : max);
    return Column(
      children: [
        for (final item in counts) ...[
          Row(
            children: [
              SizedBox(width: 72, child: Text(item.label)),
              Expanded(
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(6),
                  child: LinearProgressIndicator(
                    minHeight: 10,
                    value: item.count / maxCount,
                    backgroundColor: const Color(0xFFE4E8E6),
                    color: item.label == 'Urgent' ? MeridianColors.danger : MeridianColors.teal,
                  ),
                ),
              ),
              SizedBox(width: 32, child: Text('${item.count}', textAlign: TextAlign.end)),
            ],
          ),
          const SizedBox(height: 8),
        ],
      ],
    );
  }
}

String formatStamp(DateTime? value, {String empty = 'Not set'}) {
  if (value == null) {
    return empty;
  }
  return DateFormat('MMM d, h:mm a').format(value.toLocal());
}
