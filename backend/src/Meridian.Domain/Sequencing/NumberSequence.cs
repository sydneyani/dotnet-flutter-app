using Meridian.Domain.Common;

namespace Meridian.Domain.Sequencing;

public sealed class NumberSequence
{
    public const string WorkOrder = "WorkOrder";

    private NumberSequence()
    {
    }

    public string Name { get; private set; } = "";
    public int NextValue { get; private set; }

    public static NumberSequence Start(string name, int nextValue)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Sequence name is required.");
        }

        if (nextValue < 1)
        {
            throw new DomainException("Sequence must start at 1 or greater.");
        }

        return new NumberSequence
        {
            Name = name.Trim(),
            NextValue = nextValue
        };
    }

    public int Take()
    {
        var current = NextValue;
        NextValue++;
        return current;
    }
}
