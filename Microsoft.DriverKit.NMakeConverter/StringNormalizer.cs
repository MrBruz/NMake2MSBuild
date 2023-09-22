using System.Linq;

namespace Microsoft.DriverKit.NMakeConverter;

internal class StringNormalizer
{
	private int[] sourceLengths = new int[10];

	private uint sourceLengthCounter;

	public string NormalizeLength(string name, int traceIndentSize = 0)
	{
		int num = traceIndentSize + name.Length;
		sourceLengths[sourceLengthCounter++ % sourceLengths.Length] = num;
		return name + new string(' ', sourceLengths.Max() - num);
	}
}
