using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Telemart.Client.Common.ProductDescription
{
    public class ProductDescriptionBuilder : IProductDescriptionBuilder
    {
        private const char QuoteChar = '"';
        private const char SharpChar = '#';

        private readonly Lazy<Dictionary<string, IFunction>> functions;

        public ProductDescriptionBuilder()
        {
            functions = new Lazy<Dictionary<string, IFunction>>(() => new Dictionary<string, IFunction>
            {
                ["V"] = new V(),
                ["O"] = new O(),
                ["R"] = new R(),
                ["C"] = new C(),
                ["I"] = new I(),
                ["G"] = new G(),
                ["P"] = new P(),
                ["M"] = new M(),
                ["S"] = new S()
            });
        }

        public string Build(string mask, IReadOnlyDictionary<string, string> featureValues)
        {
            if (string.IsNullOrWhiteSpace(mask))
            {
                throw new ArgumentException("Mask is empty", nameof(mask));
            }

            if (featureValues == null)
            {
                throw new ArgumentNullException(nameof(featureValues));
            }

            IReadOnlyCollection<StringBuilder> arguments = ParseArguments(mask);

            foreach (StringBuilder arg in arguments)
            {
                string argument = arg.ToString();

                arg.Clear();

                switch (argument[0])
                {
                    case SharpChar:
                        arg.Append(Build(argument, featureValues));
                        break;
                    default:
                        arg.Append(argument.Trim(QuoteChar));
                        break;
                }
            }

            return Execute(mask.Substring(1, 1), arguments.Select(x => x.ToString()).ToArray(), featureValues);
        }

        private string Execute(string functionName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
        {
            string result = string.Empty;

            if (functions.Value.TryGetValue(functionName.ToUpper(), out IFunction func))
            {
                result = func.Execute(arguments, featureValues);
            }

            return result;
        }

        private IReadOnlyCollection<StringBuilder> ParseArguments(string mask)
        {
            int osc = 0, csc = 0;
            bool quote = false;

            List<StringBuilder> arguments = new List<StringBuilder>();

            char os = char.MinValue;
            char cs = char.MinValue;

            for (int i = 3; i < mask.Length - 1; i++)
            {
                if (cs == char.MinValue)
                {
                    if (mask[i] == QuoteChar || mask[i] == SharpChar)
                    {
                        osc = 0;
                        csc = 0;
                        quote = false;

                        switch (mask[i])
                        {
                            case QuoteChar:
                                os = QuoteChar;
                                cs = QuoteChar;
                                break;
                            case SharpChar:
                                os = '(';
                                cs = ')';
                                break;
                        }

                        string value = new string(new[] { mask[i] });

                        arguments.Add(new StringBuilder(value));
                    }
                }
                else
                {
                    if (mask[i] == QuoteChar)
                    {
                        quote = !quote;
                    }

                    if (os == QuoteChar || quote == false)
                    {
                        if (mask[i] == os)
                        {
                            osc++;
                        }

                        if (mask[i] == cs)
                        {
                            csc++;
                        }
                    }

                    if (osc == csc && osc != 0)
                    {
                        cs = char.MinValue;
                    }

                    arguments[arguments.Count - 1].Append(mask[i]);
                }
            }

            return arguments;
        }

        private interface IFunction
        {
            string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues);
        }

        private sealed class V : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                string result = string.Empty;

                if (featureValues.TryGetValue(arguments[0], out string featureValue) && featureValue != "?")
                {
                    result = featureValue;
                }

                return result;
            }
        }

        private sealed class O : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                return string.Join(string.Empty, arguments.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }

        private sealed class G : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                return string.Join(arguments[0], arguments.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }

        private sealed class R : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                string result = arguments[0];

                for (int i = 1; i <= arguments.Count / 2; i++)
                {
                    result = result.Replace(arguments[(i * 2) - 1], arguments[i * 2]);
                }

                return result;
            }
        }

        private sealed class P : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                string result = string.Empty;

                string value = arguments[0];

                if (!string.IsNullOrEmpty(value))
                {
                    result = $"{arguments[1]}{value}{arguments[2]}";
                }

                return result;
            }
        }

        private sealed class C : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                string result = string.Empty;

                for (int i = 1; i <= arguments.Count / 2; i++)
                {
                    if (arguments[0] == arguments[(i * 2) - 1])
                    {
                        result = arguments[i * 2];
                        break;
                    }
                }

                return result;
            }
        }

        private sealed class I : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                string result = arguments[0];

                for (int i = 1; i <= arguments.Count / 2; i++)
                {
                    if (arguments[0] == arguments[(i * 2) - 1])
                    {
                        result = arguments[i * 2];
                        break;
                    }
                }

                return result;
            }
        }

        private sealed class S : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                string result = string.Empty;

                if (double.TryParse(arguments[0], out double f1) && double.TryParse(arguments[2], out double f2))
                {
                    double f;

                    switch (arguments[1])
                    {
                        case "+":
                            f = f1 + f2;
                            break;
                        case "-":
                            f = f1 - f2;
                            break;
                        case "*":
                            f = f1 * f2;
                            break;
                        case "/":
                            f = f1 / f2;

                            if (arguments.Count > 3 && int.TryParse(arguments[3], out int decimals))
                            {
                                f = Math.Round(f, decimals);
                            }

                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    result = f.ToString(CultureInfo.InvariantCulture);
                }

                return result;
            }
        }

        private sealed class M : IFunction
        {
            public string Execute(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> featureValues)
            {
                string result = string.Empty;

                if (double.TryParse(arguments[0], out double f))
                {
                    string value = arguments[1];

                    if (value == "цветов" && f >= 1000)
                    {
                        f = Math.Truncate(f / 1000);
                        value = "тыс. цветов";
                    }

                    if (value == "тыс. цветов" && f >= 1000)
                    {
                        f = Math.Truncate(f / 100) / 10;
                        value = "млн. цветов";
                    }

                    if (value == "Кб" && f >= 1024)
                    {
                        f = Math.Round(f / 102.4) / 10;
                        value = "Мб";
                    }

                    if (value == "Мб" && f >= 1024)
                    {
                        f = Math.Round(f / 102.4) / 10;
                        value = "Гб";
                    }

                    if (value == "Гб")
                    {
                        if (f > 1000)
                        {
                            f = Math.Round(f / 100) / 10;
                            value = "Тб";
                        }
                        else if (f < 1)
                        {
                            f = Math.Round(f * 10240) / 10;
                            value = "Мб";
                        }
                        else
                        {
                            f = Math.Round(f * 10) / 10;
                        }
                    }

                    if (value == "МГц" && f >= 1000)
                    {
                        f = Math.Round(f / 100) / 10;
                        value = "ГГц";
                    }

                    if (value == "мин" && f >= 60)
                    {
                        f = Math.Round(f / 60);
                        value = "ч";
                    }

                    result = $"{f.ToString(CultureInfo.InvariantCulture)}&nbsp;{value}";
                }

                return result;
            }
        }
    }
}
