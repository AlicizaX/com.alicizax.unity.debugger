using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AlicizaX.Console.Suggestors
{
    public class CommandSuggestion : IAlicizaXConsoleSuggestion
    {
        private readonly CommandData _command;
        private readonly string[] _paramNames;
        private readonly int _numOptionalParams;

        private readonly Dictionary<string, Type[]> _genericSignatureCache = new Dictionary<string, Type[]>();
        private readonly Dictionary<ParameterInfo, IAlicizaXConsoleSuggestorTag[]> _parameterTagsCache = new Dictionary<ParameterInfo, IAlicizaXConsoleSuggestorTag[]>();
        private struct ParsedCommandNameInfo
        {
            public string RawName;
            public string CommandName;
            public string GenericSignature;
            public string[] GenericArgNames;
        }

        private ParsedCommandNameInfo _currentCommandNameCache;

        public string FullSignature => _command.CommandSignature;
        public string PrimarySignature => _command.CommandName;
        public string SecondarySignature { get; }
        public CommandData Command => _command;

        public CommandSuggestion(CommandData command, int numOptionalParams = 0)
        {
            _command = command;
            _paramNames = _command.ParameterSignature.Split(' ');

            _numOptionalParams = numOptionalParams;
            for (int i = _paramNames.Length - _numOptionalParams; i < _paramNames.Length; i++)
            {
                _paramNames[i] = $"[{_paramNames[i]}]";
            }

            SecondarySignature = $"{_command.GenericSignature} {string.Join(" ", _paramNames)}";
        }

        public bool MatchesPrompt(string prompt)
        {
            UpdateCurrentCache(prompt);
            return _currentCommandNameCache.CommandName == _command.CommandName;
        }

        public string GetCompletion(string prompt)
        {
            return _command.CommandName;
        }

        public string GetCompletionTail(string prompt)
        {
            UpdateCurrentCache(prompt);
            Utf16ValueStringBuilder stringBuilder = ZString.CreateStringBuilder();

            int numParamsInPrompt = prompt
                .SplitScoped(' ')
                .Count(x => !string.IsNullOrWhiteSpace(x)) - 1;

            // 提示文本里的参数数量不能小于 0。
            numParamsInPrompt = Mathf.Max(numParamsInPrompt, 0);

            int numParamsToPrint = _command.ParamCount - numParamsInPrompt;

            if (prompt == _currentCommandNameCache.CommandName)
            {
                stringBuilder.Append(_command.GenericSignature);
            }

            // 输出提示文本中还没有填写的参数。
            for (int i = 0; i < numParamsToPrint; i++)
            {
                // 末尾还没有空白字符时才补一个空格。
                if (i > 0 || !prompt.EndsWith(" "))
                {
                    stringBuilder.Append(' ');
                }

                int paramIdx = i + numParamsInPrompt;
                stringBuilder.Append(_paramNames[paramIdx]);
            }

            return stringBuilder.ToStringAndDispose();
        }

        public SuggestionContext? GetInnerSuggestionContext(SuggestionContext context)
        {
            UpdateCurrentCache(context.Prompt);

            // 如果遇到空白字符且不在未闭合作用域内，就把它视为新的提示片段。
            bool emptyPromptEnd = context.Prompt.EndsWith(" ") && context.Prompt.GetMaxScopeDepthAtEnd() == 0;
            string[] promptParts = context.Prompt
                .SplitScoped(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            int promptArgs = promptParts.Length - 1;
            if (emptyPromptEnd)
            {
                promptArgs++;
            }

            if (promptArgs <= 0 || promptArgs > _command.ParamCount)
            {
                return null;
            }

            int paramIndex = promptArgs - 1;
            SuggestionContext newContext = context;
            newContext.Depth++;
            newContext.TargetType = GetParameterType(paramIndex);
            newContext.Tags = GetParameterTags(paramIndex);
            newContext.Prompt = emptyPromptEnd
                ? string.Empty
                : promptParts.LastOrDefault();

            return newContext;
        }

        private void UpdateCurrentCache(string prompt)
        {
            string rawName = prompt.SplitScopedFirst(' ');
            if (rawName != _currentCommandNameCache.RawName)
            {
                _currentCommandNameCache = ParseCommandNameInfo(rawName);
            }
        }

        private ParsedCommandNameInfo ParseCommandNameInfo(string rawName)
        {
            string[] commandNameParts = rawName.Split(new[] { '<' }, 2);

            ParsedCommandNameInfo info = new ParsedCommandNameInfo();
            info.RawName = rawName;
            info.CommandName = commandNameParts[0];

            if (_command.IsGeneric)
            {
                info.GenericSignature = commandNameParts.Length > 1 ? $"<{commandNameParts[1]}" : "";
                info.GenericArgNames = info.GenericSignature
                    .ReduceScope('<', '>')
                    .SplitScoped(',');
            }

            return info;
        }

        private Type[] ParseGenericTypes(ParsedCommandNameInfo commandNameInfo)
        {
            return commandNameInfo
                .GenericArgNames
                .Select(AlicizaXConsoleParser.ParseType)
                .ToArray();
        }

        private Type[] GetParameterTypes(ParsedCommandNameInfo commandNameInfo)
        {
            // 不是泛型时返回普通类型。
            if (!_command.IsGeneric)
            {
                return _command.ParamTypes;
            }

            // 如果缓存可用就直接返回。
            if (_genericSignatureCache.TryGetValue(commandNameInfo.GenericSignature, out Type[] paramTypes))
            {
                return paramTypes;
            }

            try
            {
                // 根据泛型类型构建实际类型。
                Type[] genericTypes = ParseGenericTypes(_currentCommandNameCache);
                paramTypes = _command.MakeGenericArguments(genericTypes);
            }
            catch
            {
                // 无法处理泛型时使用普通类型。
                paramTypes = _command.ParamTypes;
            }

            return _genericSignatureCache[commandNameInfo.GenericSignature] = paramTypes;
        }

        private Type GetParameterType(int paramIndex)
        {
            Type[] paramTypes = GetParameterTypes(_currentCommandNameCache);
            return paramTypes[paramIndex];
        }

        private IAlicizaXConsoleSuggestorTag[] GetParameterTags(int paramIndex)
        {
            ParameterInfo parameter = _command.MethodParamData[paramIndex];
            if (_parameterTagsCache.TryGetValue(parameter, out IAlicizaXConsoleSuggestorTag[] tags))
            {
                return tags;
            }

            return _parameterTagsCache[parameter] =
                parameter
                    .GetCustomAttributes<SuggestorTagAttribute>()
                    .SelectMany(x => x.GetSuggestorTags())
                    .ToArray();
        }
    }
}
