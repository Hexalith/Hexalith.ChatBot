using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

using Hexalith.ChatBot.Client.Generated;

using Newtonsoft.Json;

using Shouldly;

namespace Hexalith.ChatBot.Client.Tests;

/// <summary>
/// Verifies strict enum values and dictionary names throughout recursive client shapes.
/// </summary>
public sealed class RecursiveStrictEnumWirePolicyTests
{
    private static readonly JsonSerializerSettings Settings = new() { ContractResolver = new StrictEnumContractResolver() };

    private static readonly Type[] DictionaryShapes =
    [
        typeof(Dictionary<PermissiveWireEnum, string>),
        typeof(IDictionary<PermissiveWireEnum, string>),
        typeof(IReadOnlyDictionary<PermissiveWireEnum, string>),
        typeof(ReadOnlyDictionary<PermissiveWireEnum, string>),
        typeof(SortedDictionary<PermissiveWireEnum, string>),
        typeof(ConcurrentDictionary<PermissiveWireEnum, string>),
        typeof(IImmutableDictionary<PermissiveWireEnum, string>),
        typeof(ImmutableDictionary<PermissiveWireEnum, string>),
        typeof(ImmutableSortedDictionary<PermissiveWireEnum, string>),
    ];

    /// <summary>
    /// Gets the common mutable, read-only and immutable enum-keyed dictionary types.
    /// </summary>
    public static TheoryData<Type> DictionaryTypes
    {
        get
        {
            TheoryData<Type> rows = new();
            foreach (Type type in DictionaryShapes)
            {
                rows.Add(type);
            }

            return rows;
        }
    }

    [Theory]
    [InlineData("1")]
    [InlineData("\"1\"")]
    [InlineData("\"\"")]
    [InlineData("\"unknown\"")]
    public void NestedCollectionsRejectPermissiveEnumValues(string token)
    {
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<List<List<PermissiveWireEnum?>>>(
            $"[[{token}]]", Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<Dictionary<string, PermissiveWireEnum?[]>>(
            $"{{\"nested\":[{token}]}}", Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<List<Dictionary<string, PermissiveWireEnum?>>>(
            $"[{{\"nested\":{token}}}]", Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<Dictionary<PermissiveWireEnum, List<PermissiveWireEnum?>>>(
            $"{{\"primary-state\":[{token}]}}", Settings));
    }

    [Fact]
    public void NestedNamedAndNullableValuesRoundTrip()
    {
        const string json = """[{"first":["secondary-state",null],"second":["primary-state"]}]""";
        List<Dictionary<string, PermissiveWireEnum?[]>> value = JsonConvert
            .DeserializeObject<List<Dictionary<string, PermissiveWireEnum?[]>>>(json, Settings).ShouldNotBeNull();

        value[0]["first"].ShouldBe([PermissiveWireEnum.Secondary, null]);
        JsonConvert.SerializeObject(value, Settings).ShouldBe(json);
        JsonConvert.DeserializeObject<PermissiveWireEnum>("\"PRIMARY-STATE\"", Settings).ShouldBe(PermissiveWireEnum.Primary);
        JsonConvert.DeserializeObject<PermissiveWireEnum>("\"Secondary\"", Settings).ShouldBe(PermissiveWireEnum.Secondary);
    }

    [Theory]
    [InlineData("""{"state":1}""")]
    [InlineData("""{"states":[1]}""")]
    [InlineData("""{"byName":{"first":1}}""")]
    [InlineData("""{"byState":{"1":["primary-state"]}}""")]
    [InlineData("""{"byState":{"primary-state":[1]}}""")]
    public void GeneratedPropertyAndItemAttributesCannotWeakenThePolicy(string json)
        => Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<RecursiveEnumCarrier>(json, Settings));

    [Fact]
    public void GeneratedPropertyAndItemAttributesPreserveNamedAndNullValues()
    {
        const string json = """{"state":"primary-state","states":["secondary-state",null],"byName":{"first":"primary-state"},"byState":{"secondary-state":["primary-state",null]}}""";
        RecursiveEnumCarrier carrier = JsonConvert.DeserializeObject<RecursiveEnumCarrier>(json, Settings).ShouldNotBeNull();

        carrier.States.ShouldNotBeNull().ShouldBe([PermissiveWireEnum.Secondary, null]);
        carrier.ByState.ShouldNotBeNull()[PermissiveWireEnum.Secondary].ShouldBe([PermissiveWireEnum.Primary, null]);
        JsonConvert.SerializeObject(carrier, Settings).ShouldBe(json);
    }

    [Theory]
    [MemberData(nameof(DictionaryTypes))]
    public void CommonEnumKeyedDictionaryShapesRoundTripNamedKeys(Type dictionaryType)
    {
        ArgumentNullException.ThrowIfNull(dictionaryType);
        const string json = """{"secondary-state":"value"}""";
        object dictionary = JsonConvert.DeserializeObject(json, dictionaryType, Settings).ShouldNotBeNull();

        dictionaryType.IsInstanceOfType(dictionary).ShouldBeTrue();
        ((IEnumerable<KeyValuePair<PermissiveWireEnum, string>>)dictionary).Single().Key.ShouldBe(PermissiveWireEnum.Secondary);
        JsonConvert.SerializeObject(dictionary, Settings).ShouldBe(json);
        JsonConvert.DeserializeObject("null", dictionaryType, Settings).ShouldBeNull();
        JsonConvert.SerializeObject(JsonConvert.DeserializeObject("{}", dictionaryType, Settings), Settings).ShouldBe("{}");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("999")]
    [InlineData("")]
    [InlineData("unknown")]
    public void CommonEnumKeyedDictionaryShapesRejectNumericEmptyAndUnknownKeys(string name)
    {
        string json = $"{{{JsonConvert.SerializeObject(name)}:\"value\"}}";
        foreach (Type dictionaryType in DictionaryShapes)
        {
            Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject(json, dictionaryType, Settings));
        }
    }

    [Fact]
    public void UndefinedNestedValuesAndDictionaryKeysCannotBeWritten()
    {
        PermissiveWireEnum undefined = (PermissiveWireEnum)999;
        Should.Throw<JsonSerializationException>(() => JsonConvert.SerializeObject(new List<PermissiveWireEnum[]> { new[] { undefined } }, Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.SerializeObject(new Dictionary<PermissiveWireEnum, string> { [undefined] = "value" }, Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.SerializeObject(new Dictionary<PermissiveWireEnum, PermissiveWireEnum> { [PermissiveWireEnum.Primary] = undefined }, Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.SerializeObject(new RecursiveEnumCarrier { States = [undefined] }, Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.SerializeObject(new RecursiveEnumCarrier { ByName = new Dictionary<string, PermissiveWireEnum> { ["first"] = undefined } }, Settings));
    }

    [Fact]
    public void NestedInvalidKeyRetainsTheResponseReaderLocation()
    {
        string json = string.Join('\n', ["{", "  \"outer\": [", "    {", "      \"unknown.key\": \"value\"", "    }", "  ]", "}"]);
        using JsonTextReader responseReader = new(new StringReader(json));
        while (responseReader.Read())
        {
            if (responseReader.TokenType == JsonToken.PropertyName && Equals(responseReader.Value, "unknown.key"))
            {
                break;
            }
        }

        responseReader.Value.ShouldBe("unknown.key");

        JsonSerializationException exception = Should.Throw<JsonSerializationException>(() => JsonConvert
            .DeserializeObject<Dictionary<string, List<Dictionary<PermissiveWireEnum, string>>>>(json, Settings));

        exception.Path.ShouldBe("outer[0]['unknown.key']");
        exception.Path.ShouldBe(responseReader.Path);
        exception.LineNumber.ShouldBe(4);
        exception.LineNumber.ShouldBe(responseReader.LineNumber);
        exception.LinePosition.ShouldBeGreaterThan(0);
        exception.LinePosition.ShouldBe(responseReader.LinePosition);
        exception.InnerException.ShouldBeOfType<JsonSerializationException>();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("\"1\"")]
    [InlineData("\"\"")]
    public void EnumKeyedDictionariesRejectInvalidDirectEnumValues(string token)
    {
        string json = $"{{\"primary-state\":{token}}}";
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<Dictionary<PermissiveWireEnum, PermissiveWireEnum>>(json, Settings));
        Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<Dictionary<PermissiveWireEnum, PermissiveWireEnum?>>(json, Settings));
    }

    [Fact]
    public void EnumKeyedDictionariesPreserveDirectNamedAndNullableValues()
    {
        const string namedJson = """{"primary-state":"secondary-state"}""";
        Dictionary<PermissiveWireEnum, PermissiveWireEnum> named = JsonConvert
            .DeserializeObject<Dictionary<PermissiveWireEnum, PermissiveWireEnum>>(namedJson, Settings).ShouldNotBeNull();
        named[PermissiveWireEnum.Primary].ShouldBe(PermissiveWireEnum.Secondary);
        JsonConvert.SerializeObject(named, Settings).ShouldBe(namedJson);

        const string nullableJson = """{"primary-state":"secondary-state","secondary-state":null}""";
        Dictionary<PermissiveWireEnum, PermissiveWireEnum?> nullable = JsonConvert
            .DeserializeObject<Dictionary<PermissiveWireEnum, PermissiveWireEnum?>>(nullableJson, Settings).ShouldNotBeNull();
        nullable[PermissiveWireEnum.Primary].ShouldBe(PermissiveWireEnum.Secondary);
        nullable[PermissiveWireEnum.Secondary].ShouldBeNull();
        JsonConvert.SerializeObject(nullable, Settings).ShouldBe(nullableJson);
    }

    [Theory]
    [InlineData("getOnlyStates")]
    [InlineData("writableStates")]
    public void InitializedDictionariesMergeNamedValuesWithoutReplacingTheirInstance(string propertyName)
    {
        InitializedEnumDictionaryCarrier carrier = new();
        IDictionary<PermissiveWireEnum, string> existing = propertyName == "getOnlyStates" ? carrier.GetOnlyStates : carrier.WritableStates;
        string json = $"{{{JsonConvert.SerializeObject(propertyName)}:{{\"secondary-state\":\"added\"}}}}";
        JsonConvert.PopulateObject(json, carrier, Settings);

        (propertyName == "getOnlyStates" ? carrier.GetOnlyStates : carrier.WritableStates).ShouldBeSameAs(existing);
        existing.Count.ShouldBe(2);
        existing[PermissiveWireEnum.Primary].ShouldBe("retained");
        existing[PermissiveWireEnum.Secondary].ShouldBe("added");

        InitializedEnumDictionaryCarrier deserialized = JsonConvert
            .DeserializeObject<InitializedEnumDictionaryCarrier>(json, Settings).ShouldNotBeNull();
        IDictionary<PermissiveWireEnum, string> merged = propertyName == "getOnlyStates" ? deserialized.GetOnlyStates : deserialized.WritableStates;
        merged.Count.ShouldBe(2);
        merged[PermissiveWireEnum.Primary].ShouldBe("retained");
        merged[PermissiveWireEnum.Secondary].ShouldBe("added");
    }

    [Theory]
    [InlineData("getOnlyStates")]
    [InlineData("writableStates")]
    public void InitializedDictionariesRejectInvalidKeysBeforeAddingTheirValue(string propertyName)
    {
        InitializedEnumDictionaryCarrier carrier = new();
        IDictionary<PermissiveWireEnum, string> existing = propertyName == "getOnlyStates" ? carrier.GetOnlyStates : carrier.WritableStates;
        string json = $"{{{JsonConvert.SerializeObject(propertyName)}:{{\"1\":\"untrusted\"}}}}";

        Should.Throw<JsonSerializationException>(() => JsonConvert.PopulateObject(json, carrier, Settings));
        existing.Count.ShouldBe(1);
        existing[PermissiveWireEnum.Primary].ShouldBe("retained");
        existing.ContainsKey(PermissiveWireEnum.Secondary).ShouldBeFalse();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("true")]
    [InlineData("\"value\"")]
    [InlineData("{")]
    [InlineData("{\"primary-state\":")]
    [InlineData("{\"primary-state\":\"value\"")]
    [InlineData("{\"primary-state\" \"value\"}")]
    [InlineData("{\"primary-state\":}")]
    public void MalformedTruncatedAndNonObjectDictionaryInputsFailWithinJsonExceptionFamily(string json)
        => Should.Throw<JsonException>(() => JsonConvert.DeserializeObject<Dictionary<PermissiveWireEnum, string>>(json, Settings));

    [Fact]
    public void EnumKeyedDictionaryAllowsCommentsAroundNamesAndValues()
    {
        const string json = """/* before */ { /* key */ "primary-state": /* value */ "value" /* end */ } /* after */""";
        Dictionary<PermissiveWireEnum, string> dictionary = JsonConvert
            .DeserializeObject<Dictionary<PermissiveWireEnum, string>>(json, Settings).ShouldNotBeNull();

        dictionary.Count.ShouldBe(1);
        dictionary[PermissiveWireEnum.Primary].ShouldBe("value");
        JsonConvert.SerializeObject(dictionary, Settings).ShouldBe("""{"primary-state":"value"}""");
    }

    [Fact]
    public void EnumKeyNamesAcceptExistingCaseAndClrAliasesThenWriteCanonicalWireNames()
    {
        Dictionary<PermissiveWireEnum, string> dictionary = JsonConvert
            .DeserializeObject<Dictionary<PermissiveWireEnum, string>>("""{"PRIMARY-STATE":"first","Secondary":"second"}""", Settings).ShouldNotBeNull();

        dictionary[PermissiveWireEnum.Primary].ShouldBe("first");
        dictionary[PermissiveWireEnum.Secondary].ShouldBe("second");
        JsonConvert.SerializeObject(dictionary, Settings).ShouldBe("""{"primary-state":"first","secondary-state":"second"}""");
    }

    [Fact]
    public void ReusedResolverKeepsRecursiveDictionaryValidationIsolatedAcrossThreads()
    {
        Parallel.For(0, 32, _ =>
        {
            const string json = """{"primary-state":{"secondary-state":["primary-state",null]}}""";
            Dictionary<PermissiveWireEnum, Dictionary<PermissiveWireEnum, PermissiveWireEnum?[]>> value = JsonConvert
                .DeserializeObject<Dictionary<PermissiveWireEnum, Dictionary<PermissiveWireEnum, PermissiveWireEnum?[]>>>(json, Settings).ShouldNotBeNull();
            JsonConvert.SerializeObject(value, Settings).ShouldBe(json);
            Should.Throw<JsonSerializationException>(() => JsonConvert.DeserializeObject<Dictionary<PermissiveWireEnum, Dictionary<PermissiveWireEnum, PermissiveWireEnum?[]>>>(
                """{"primary-state":{"1":["primary-state"]}}""", Settings));
        });
    }
}
