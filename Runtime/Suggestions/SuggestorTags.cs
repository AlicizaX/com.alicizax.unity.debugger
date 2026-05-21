using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console.Suggestors.Tags
{
    public struct CommandNameTag : IAlicizaXConsoleSuggestorTag
    {

    }

    public sealed class CommandNameAttribute : SuggestorTagAttribute
    {
        private readonly IAlicizaXConsoleSuggestorTag[] _tags = { new CommandNameTag() };

        public override IAlicizaXConsoleSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }

    public struct InlineSuggestionsTag : IAlicizaXConsoleSuggestorTag
    {
        public readonly IEnumerable<string> Suggestions;

        public InlineSuggestionsTag(IEnumerable<string> suggestions)
        {
            Suggestions = suggestions;
        }
    }

    public sealed class SuggestionsAttribute : SuggestorTagAttribute
    {
        private readonly IAlicizaXConsoleSuggestorTag[] _tags;

        public SuggestionsAttribute(params object[] suggestions)
        {
            InlineSuggestionsTag tag = new InlineSuggestionsTag(suggestions.Select(o => o.ToString()));
            _tags = new IAlicizaXConsoleSuggestorTag[] { tag };
        }

        public override IAlicizaXConsoleSuggestorTag[] GetSuggestorTags() => _tags;
    }

    public struct SceneNameTag : IAlicizaXConsoleSuggestorTag
    {
        public bool LoadedOnly;
    }

    public sealed class SceneNameAttribute : SuggestorTagAttribute
    {
        public bool LoadedOnly
        {
            get => _tag.LoadedOnly;
            set => _tag.LoadedOnly = value;
        }

        private SceneNameTag _tag;

        public override IAlicizaXConsoleSuggestorTag[] GetSuggestorTags()
        {
            return new IAlicizaXConsoleSuggestorTag[] { _tag };
        }
    }
}
