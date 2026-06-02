using System.Collections.Generic;
using System.Linq;
using AlicizaX.Console.Utilities;

namespace AlicizaX.Console.Suggestors
{
    public struct CollapsedCommand
    {
        public CommandData Command;
        public int NumOptionalParams;

        public CollapsedCommand(CommandData command)
        {
            Command = command;
            NumOptionalParams = 0;
        }
    }

    public class CommandSuggestor : BasicCachedAlicizaXConsoleSuggestor<CollapsedCommand>
    {
        private readonly Dictionary<string, List<CommandData>> _commandGroups = new Dictionary<string, List<CommandData>>();
        private readonly Stack<CollapsedCommand> _commandCollector = new Stack<CollapsedCommand>();

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.Depth == 0;
        }

        protected override IAlicizaXConsoleSuggestion ItemToSuggestion(CollapsedCommand collapsedCommand)
        {
            return new CommandSuggestion(collapsedCommand.Command, collapsedCommand.NumOptionalParams);
        }

        protected override IEnumerable<CollapsedCommand> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            string incompleteCommandName =
                context.Prompt
                    .SplitScopedFirst(' ')
                    .SplitFirst('<');

            IEnumerable<CommandData> commands = GetCommands(incompleteCommandName, options);
            return options.CollapseOverloads
                ? CollapseCommands(commands)
                : commands.Select(x => new CollapsedCommand(x));
        }

        public IEnumerable<CommandData> GetCommands(string incompleteCommandName, SuggestorOptions options)
        {
            if (string.IsNullOrWhiteSpace(incompleteCommandName))
            {
                return Enumerable.Empty<CommandData>();
            }

            return AlicizaXConsoleProcessor.GetAllCommands()
                .Where(command => SuggestorUtilities.IsCompatible(incompleteCommandName, command.CommandName, options));
        }

        protected override bool IsMatch(SuggestionContext context, IAlicizaXConsoleSuggestion suggestion, SuggestorOptions options)
        {
            // 在 GetCommands 中执行过滤。
            return true;
        }

        private IEnumerable<CollapsedCommand> CollapseCommands(IEnumerable<CommandData> commands)
        {
            // 重置命令分组，但保留列表对象以减少内存分配。
            foreach (List<CommandData> commandGroup in _commandGroups.Values)
            {
                commandGroup.Clear();
            }

            // 把命令分到各自的组里。
            foreach (CommandData command in commands)
            {
                if (!_commandGroups.TryGetValue(command.CommandName, out List<CommandData> commandGroup))
                {
                    commandGroup = new List<CommandData>();
                    _commandGroups[command.CommandName] = commandGroup;
                }

                commandGroup.Add(command);
            }

            // 每个分组内按参数数量从少到多遍历命令。
            // 如果新候选命令等于上一个候选命令再多一个参数。
            // 然后把前一个命令吸收为可选参数，否则两个都保留。
            foreach (List<CommandData> commandGroup in _commandGroups.Values)
            {
                commandGroup.InsertionSortBy(x => x.ParamCount);
                _commandCollector.Clear();

                foreach (CommandData command in commandGroup)
                {
                    CollapsedCommand newCandidate = new CollapsedCommand(command);
                    if (_commandCollector.Count > 0)
                    {
                        CollapsedCommand prevCandidate = _commandCollector.Peek();
                        CommandData newCommand = newCandidate.Command;
                        CommandData prevCommand = prevCandidate.Command;

                        if (newCommand.ParamCount == prevCommand.ParamCount + 1)
                        {
                            if (newCommand.ParameterSignature.StartsWith(prevCommand.ParameterSignature))
                            {
                                _commandCollector.Pop();
                                newCandidate.NumOptionalParams += 1 + prevCandidate.NumOptionalParams;
                            }
                        }
                    }

                    _commandCollector.Push(newCandidate);
                }

                foreach (CollapsedCommand collapsedCommand in _commandCollector)
                {
                    yield return collapsedCommand;
                }
            }
        }
    }
}