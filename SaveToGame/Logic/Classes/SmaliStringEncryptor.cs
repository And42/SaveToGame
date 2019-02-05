using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SaveToGameWpf.Logic.Utils;
using SmaliParser.Logic;

namespace SaveToGameWpf.Logic.Classes
{
    internal class SmaliStringEncryptor
    {
        [NotNull]
        private static readonly Regex MessageRegex = new Regex(@"const-string v([\d]*), ""(.*?[^\\])""");
        [NotNull]
        private static readonly Random Random = new Random();
        [NotNull]
        private static readonly string NewLine = Environment.NewLine;

        public static int EncryptMethod(
            [NotNull] SmaliMethod method,
            [NotNull] string className,
            [NotNull] Dictionary<string, SmaliMethod> methods
        )
        {
            Guard.NotNullArgument(method, nameof(method));
            Guard.NotNullArgument(className, nameof(className));
            Guard.NotNullArgument(methods, nameof(methods));

            int plusCount = 0;

            int tempRegister = method.RegistersType == SmaliMethod.RegistersTypes.Locals
                ? method.Registers.Value
                : method.Registers.Value - method.Parameters.Count;

            if (method.Registers < 15)
                method.Registers++;
            else
                tempRegister = 14;

            List<string> lines = method.Body;

            for (int j = 0; j < lines.Count; j++)
            {
                string str = lines[j];

                var matches = MessageRegex.Matches(str);
                
                if (matches.Count == 0)
                    continue;

                int resultRegister = int.Parse(matches[0].Groups[1].Value);

                string text = matches[0].Groups[2].Value;

                int plus;

                text = EncodeString(text, className, methods, tempRegister, resultRegister, out plus, 
                    method.Name == "wPdauIdcaW" && method.Parameters.Count == 2 ? "PdsjdolaSd" : null);

                lines[j] = text;

                plusCount += plus;
            }

            return plusCount;
        }

        [NotNull]
        private static string EncodeString(
            [NotNull] string input,
            [NotNull] string className,
            [NotNull] Dictionary<string, SmaliMethod> methods,
            int tempRegister,
            int resultRegister,
            out int plusCount,
            [CanBeNull] string plusName = null
        )
        {
            Guard.NotNullArgument(input, nameof(input));
            Guard.NotNullArgument(className, nameof(className));
            Guard.NotNullArgument(methods, nameof(methods));

            plusCount = 0;

            string nl = NewLine;

            var result = new StringBuilder(
                $"    new-instance v{resultRegister}, Ljava/lang/StringBuilder;{nl}" +
                $"{nl}" +
                $"    invoke-direct {{v{resultRegister}}}, Ljava/lang/StringBuilder;-><init>()V{nl}" +
                $"{nl}");

            for (int i = 0; i < input.Length; i++)
            {
                string ch = input[i].ToString();

                string methodChar;
                if (ch == "\\")
                {
                    methodChar = ch + input[i + 1] + (plusName != null ? "+" : "");
                    i++;
                }
                else
                    methodChar = ch + (plusName != null ? "+" : "");

                if (plusName != null)
                    plusCount++;

                SmaliMethod method;

                if (!methods.TryGetValue(methodChar, out method))
                {
                    string[] existingNames = methods.Select(m => m.Value.Name).ToArray();

                    method = GenerateStringCall(methodChar.TrimEnd('+'), className, existingNames, plusName);

                    methods.Add(methodChar, method);
                }

                result.Append(
                    $"    invoke-static {{}}, {className};->{method.Name}()Ljava/lang/String;{nl}" +
                    $"{nl}" +
                    $"    move-result-object v{tempRegister}{nl}" +
                    $"{nl}" +
                    $"    invoke-virtual {{v{resultRegister},v{tempRegister}}}, Ljava/lang/StringBuilder;->append(Ljava/lang/String;)Ljava/lang/StringBuilder;{nl}" +
                    $"{nl}" +
                    $"    move-result-object v{resultRegister}{nl}" +
                    $"{nl}");
            }

            result.Append(
                $"    invoke-virtual {{v{resultRegister}}}, Ljava/lang/StringBuilder;->toString()Ljava/lang/String;{nl}" +
                $"{nl}" +
                $"    move-result-object v{resultRegister}");

            return result.ToString();
        }

        /// <summary>
        /// Возвращает smali метод, возвращающий одну букву
        /// </summary>
        /// <param name="str">Буква в юникоде</param>
        /// <param name="className">Имя класса</param>
        /// <param name="existingMethodNames">Существующие методы</param>
        /// <param name="plusName">Название поля для инкремента</param>
        [NotNull]
        private static SmaliMethod GenerateStringCall(
            [NotNull] string str,
            [NotNull] string className,
            [NotNull] string[] existingMethodNames,
            [CanBeNull] string plusName = null
        )
        {
            Guard.NotNullArgument(str, nameof(str));
            Guard.NotNullArgument(className, nameof(className));
            Guard.NotNullArgument(existingMethodNames, nameof(existingMethodNames));

            string name = GenerateRandomString();

            string nl = NewLine;

            while (existingMethodNames.Contains(name))
                name = GenerateRandomString();

            var method = new SmaliMethod
            {
                Name = name,
                RegistersType = SmaliMethod.RegistersTypes.Locals,
                Registers = 1,
                ReturnType = "Ljava/lang/String",
                Modifers = { "private", "static" }
            };

            if (plusName != null)
            {
                method.Body.Add(
                    $"    sget v0, {className};->{plusName}:I{nl}" +
                    $"{nl}" +
                    $"    add-int/lit8 v0, v0, 0x1{nl}" +
                    $"{nl}" +
                    $"    sput v0, {className};->{plusName}:I{nl}" +
                    $"{nl}");
            }

            method.Body.Add(
                $"    const-string v0, \"{str}\"{nl}" +
                $"{nl}" +
                $"    return-object v0");

            return method;
        }

        /// <summary>
        /// Возвращает случайно сгенерированную строку указанной длины
        /// </summary>
        /// <param name="startLength">Минимальная длина</param>
        /// <param name="endLength">Максимальная длина</param>
        /// <returns>Случайная строка</returns>
        [NotNull]
        private static string GenerateRandomString(int startLength = 8, int endLength = 16)
        {
            if (endLength < startLength)
                throw new ArgumentOutOfRangeException(nameof(startLength) + " - " + nameof(endLength));

            int length = Random.Next(startLength, endLength);

            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
                result.Append(Random.Next(1, 3) == 2 ? (char) Random.Next('A', 'Z') : (char) Random.Next('a', 'z'));

            return result.ToString();
        }
    }
}
