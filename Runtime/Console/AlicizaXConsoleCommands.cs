using AlicizaX.Console.Suggestors.Tags;
using AlicizaX.Console.Utilities;
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AlicizaX.Console
{
    public static partial class AlicizaXConsoleProcessor
    {

        private const string helpStr = "欢迎使用AlicizaXConsole控制台！为了查看有关任何特定命令的特定帮助，" +
                        "请使用‘man’命令。使用‘man man’查看有关man命令的详细信息。查看所有命令的完整列表" +
"命令，使用'all-commands'。\n\n" +
"单目标\n各种命令可能在其命令签名中显示单目标。\n" +
"这意味着它们不是静态命令，而是需要类的实例才能调用命令" +
"\n每个单声道目标的工作方式不同，如下所示：" +
"\n-single：使用在场景中找到的类型的第一个实例" +
"\n-all:使用在场景中找到的类型的所有实例" +
"\n-registra:使用在注册表中找到的类型的所有实例" +
"\n-signlet:自动创建和管理单个实例" +
"\n\n注册表是AlicizaXConsole注册表的一部分，允许您决定类的哪些特定实例" +
"应在调用命令时使用。若要将对象添加到注册表，请使用" +
"AlicizaX.Console.AlicizaXConsoleRegistry.RegisterObject＜T＞或运行时命令‘register object＜T＞’。";

        [Command("help", "显示控制台的基本帮助指南")]
        private static string GetHelp()
        {
            return helpStr;
        }

        [Command("manual")]
        [Command("man")]
        private static string ManualHelp()
        {
            return "要使用man命令，只需将所需的命令名称放在它前面。例如, 'man-my-command' 将为 'my command' 生成手册";
        }

        [CommandDescription("Generates a user manual for any given command, including built in ones. To use the man command, simply put the desired command name infront of it. For example, 'man my-command' will generate the manual for 'my-command'")]
        [Command("help")]
        [Command("manual")]
        [Command("man")]
        private static string GenerateCommandManual([CommandName] string commandName)
        {
            string[] matchingCommands =
                _commandTable
                    .Keys
                    .Where(key => key.Split('(')[0] == commandName)
                    .OrderBy(key => key)
                    .ToArray();

            if (matchingCommands.Length == 0)
            {
                throw new ArgumentException(ZString.Format("No command with the name {0} was found.", commandName));
            }

            Dictionary<string, ParameterInfo> foundParams = new Dictionary<string, ParameterInfo>();
            Dictionary<string, Type> foundGenericArguments = new Dictionary<string, Type>();
            Dictionary<string, CommandParameterDescriptionAttribute> foundParamDescriptions = new Dictionary<string, CommandParameterDescriptionAttribute>();
            List<Type> declaringTypes = new List<Type>(1);

            Utf16ValueStringBuilder manual = ZString.CreateStringBuilder();
            manual.AppendFormat("Generated user manual for {0}\nAvailable command signatures:", commandName);

            for (int i = 0; i < matchingCommands.Length; i++)
            {
                CommandData currentCommand = _commandTable[matchingCommands[i]];
                declaringTypes.Add(currentCommand.MethodData.DeclaringType);

                manual.AppendFormat("\n   - {0}", currentCommand.CommandSignature);
                if (!currentCommand.IsStatic) { manual.AppendFormat(" (mono-target = {0})", GetMonoTargetDisplayName(currentCommand.MonoTarget)); }
                for (int j = 0; j < currentCommand.ParamCount; j++)
                {
                    ParameterInfo param = currentCommand.MethodParamData[j];
                    if (!foundParams.ContainsKey(param.Name)) { foundParams.Add(param.Name, param); }
                    if (!foundParamDescriptions.ContainsKey(param.Name))
                    {
                        CommandParameterDescriptionAttribute descriptionAttribute = param.GetCustomAttribute<CommandParameterDescriptionAttribute>();
                        if (descriptionAttribute != null && descriptionAttribute.Valid) { foundParamDescriptions.Add(param.Name, descriptionAttribute); }
                    }
                }

                if (currentCommand.IsGeneric)
                {
                    Type[] genericArgs = currentCommand.GenericParamTypes;
                    for (int j = 0; j < genericArgs.Length; j++)
                    {
                        Type arg = genericArgs[j];
                        if (!foundGenericArguments.ContainsKey(arg.Name)) { foundGenericArguments.Add(arg.Name, arg); }
                    }
                }
            }

            if (foundParams.Count > 0)
            {
                manual.Append("\nParameter info:");
                ParameterInfo[] commandParams = foundParams.Values.ToArray();
                for (int i = 0; i < commandParams.Length; i++)
                {
                    ParameterInfo currentParam = commandParams[i];
                    manual.AppendFormat("\n   - {0}: {1}", currentParam.Name, currentParam.ParameterType.GetDisplayName());
                }
            }

            bool hasGenericConstraintInformation = false;
            if (foundGenericArguments.Count > 0)
            {
                Type[] genericArgs = foundGenericArguments.Values.ToArray();
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    Type arg = genericArgs[i];
                    Type[] typeConstraints = arg.GetGenericParameterConstraints();
                    GenericParameterAttributes attributes = arg.GenericParameterAttributes;

                    List<string> formattedConstraints = new List<string>();
                    if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)) { formattedConstraints.Add("struct"); }
                    if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)) { formattedConstraints.Add("class"); }
                    for (int j = 0; j < typeConstraints.Length; j++) { formattedConstraints.Add(typeConstraints[j].GetDisplayName()); }
                    if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)) { formattedConstraints.Add("new()"); }

                    if (formattedConstraints.Count > 0)
                    {
                        if (!hasGenericConstraintInformation)
                        {
                            manual.Append("\nGeneric constraints:");
                            hasGenericConstraintInformation = true;
                        }

                        manual.AppendFormat("\n   - {0}: {1}", arg.Name, ZString.Join(", ", formattedConstraints));
                    }
                }
            }

            for (int i = 0; i < matchingCommands.Length; i++)
            {
                CommandData currentCommand = _commandTable[matchingCommands[i]];
                if (currentCommand.HasDescription)
                {
                    manual.AppendFormat("\n\nCommand description:\n{0}", currentCommand.CommandDescription);
                    i = matchingCommands.Length;
                }
            }

            if (foundParamDescriptions.Count > 0)
            {
                manual.Append("\n\nParameter descriptions:");
                ParameterInfo[] commandParams = foundParams.Values.ToArray();
                for (int i = 0; i < commandParams.Length; i++)
                {
                    ParameterInfo currentParam = commandParams[i];
                    if (foundParamDescriptions.ContainsKey(currentParam.Name))
                    {
                        manual.AppendFormat("\n - {0}: {1}", currentParam.Name, foundParamDescriptions[currentParam.Name].Description);
                    }
                }
            }

            declaringTypes = declaringTypes.Distinct().ToList();
            manual.Append("\n\nDeclared in");
            if (declaringTypes.Count == 1) { manual.AppendFormat(" {0}", declaringTypes[0].GetDisplayName(true)); }
            else
            {
                manual.Append(':');
                foreach (Type type in declaringTypes)
                {
                    manual.AppendFormat("\n   - {0}", type.GetDisplayName(true));
                }
            }

            return manual.ToStringAndDispose();
        }

        private static string GetMonoTargetDisplayName(MonoTargetType monoTarget)
        {
            return monoTarget switch
            {
                MonoTargetType.Single => "single",
                MonoTargetType.All => "all",
                MonoTargetType.Registry => "registry",
                MonoTargetType.Singleton => "singleton",
                MonoTargetType.SingleInactive => "singleinactive",
                MonoTargetType.AllInactive => "allinactive",
                MonoTargetType.Argument => "argument",
                MonoTargetType.ArgumentMulti => "argumentmulti",
                _ => monoTarget.ToString()
            };
        }

        /// <summary>
        /// 获取所有已加载且去重后的命令；同名重载只显示一次。
        /// </summary>
        /// <returns>所有已加载且去重后的命令。</returns>
        public static IEnumerable<CommandData> GetUniqueCommands()
        {
            return GetAllCommands()
                .DistinctBy(x => x.CommandName)
                .OrderBy(x => x.CommandName);
        }

        [CommandDescription("Generates a list of all commands currently loaded by the AlicizaX Console Processor")]
        [Command("commands")]
        [Command("all-commands")]
        private static string GenerateCommandList()
        {
            Utf16ValueStringBuilder output = ZString.CreateStringBuilder();
            output.Append("List of all commands loaded by the AlicizaXConsole Processor. Use 'man' on any command to see more:");
            foreach (CommandData command in GetUniqueCommands())
            {
                output.AppendFormat("\n   - {0}", command.CommandName);
            }

            return output.ToStringAndDispose();
        }

        [Command("user-commands", "Generates a list of all commands added by the user")]
        private static IEnumerable<string> GenerateUserCommandList()
        {
            return GetUniqueCommands()
                .Where(x => !x.MethodData.DeclaringType.Assembly.FullName.StartsWith("AlicizaX.Debugger"))
                .Select(x => ZString.Format("   - {0}", x.CommandName));
        }
    }
}
