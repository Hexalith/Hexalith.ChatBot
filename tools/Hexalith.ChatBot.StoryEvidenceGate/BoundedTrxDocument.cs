using System.Xml;
using System.Xml.Linq;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>Bounds XML structure before constructing an evidence document tree.</summary>
internal static class BoundedTrxDocument
{
    /// <summary>The greatest admitted reader depth, with the document element at depth zero.</summary>
    internal const int MaximumDepth = 64;

    /// <summary>The maximum aggregate reader nodes and attributes, including namespace declarations.</summary>
    internal const int MaximumNodeUnits = 1_000_000;

    /// <summary>Preflights and loads the same immutable, byte-bounded XML input.</summary>
    /// <param name="bytes">The complete input bytes, which callers must not mutate during the call.</param>
    /// <returns>The document after its byte, character, depth and structural budgets pass.</returns>
    internal static XDocument Load(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length > BoundedEvidenceInput.MaximumInputBytes)
        {
            throw new XmlException("TRX input exceeds its byte budget.");
        }

        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = BoundedEvidenceInput.MaximumInputBytes,
        };
        using MemoryStream stream = new(bytes, writable: false);
        using (XmlReader reader = XmlReader.Create(stream, settings))
        {
            int units = 0;
            while (reader.Read())
            {
                if (reader.Depth > MaximumDepth
                    || reader.AttributeCount >= MaximumNodeUnits - units)
                {
                    throw new XmlException("TRX input exceeds its structural budget.");
                }

                units += 1 + reader.AttributeCount;
            }
        }

        stream.Position = 0;
        using XmlReader treeReader = XmlReader.Create(stream, settings);
        return XDocument.Load(treeReader, LoadOptions.None);
    }
}
