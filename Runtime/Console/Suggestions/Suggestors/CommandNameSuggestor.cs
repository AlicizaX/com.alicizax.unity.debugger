using System.Collections.Generic;
using System.Linq;

namespace AlicizaX.Console.Suggestors
{
    public class CommandNameSuggestor : BasicCachedAlicizaXConsoleSuggestor<string>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.HasTag<Tags.CommandNameTag>()
                && !string.IsNullOrWhiteSpace(context.Prompt);
        }

        protected override IAlicizaXConsoleSuggestion ItemToSuggestion(string commandName)
        {
            return new RawSuggestion(commandName);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            string incompleteCommandName =
                context.Prompt
                    .SplitScopedFirst(' ')
                    .SplitFirst('<');

            return AlicizaXConsoleProcessor.GetUniqueCommands()
                .Select(command => command.CommandName);
        }
    }
}