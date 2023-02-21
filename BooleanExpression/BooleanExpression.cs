using CSL.Trees;

namespace CSL.BooleanExpression
{
    public class BooleanExpression
    {
        private string m_Raw;
        private NTree<string> m_ExpressionTree;

        public BooleanExpression(string expression)
        {
            if (!BooleanExpressionParser.CheckBooleanExpression(expression)) throw new Exception("Expresión no válida.");
            m_Raw = BooleanExpressionParser.AddParenthesis(expression);
            m_ExpressionTree = BuildExpressionTree(m_Raw);
        }

        public void PreorderTraverse(Action<NTree<string>> func)
        {
            m_ExpressionTree.PreorderTraverse(func);
        }

        public void PostorderTraverse(Action<NTree<string>> func)
        {
            m_ExpressionTree.PostorderTraverse(func);
        }

        public string GetSqlCondition()
        {
            return DeepGetSqlCondition(m_ExpressionTree);
        }

        private static string DeepGetSqlCondition(NTree<string> root)
        {
            string partial = "";

            string data = root.GetData();
            if (data != "&" && data != "|")
            {
                // Tenemos un operando o una hoja...
                if (root.IsLeaf()) return data;
                // Tenemos un operando, los operandos solo pueden tener dos hijos, por definición.
                switch (data)
                {
                    case "!":
                        partial += $" (NOT {DeepGetSqlCondition(root.GetChild(0))}) ";
                        break;
                    case ">":
                        partial += $" ({DeepGetSqlCondition(root.GetChild(0))} > {DeepGetSqlCondition(root.GetChild(1))}) ";
                        break;
                    case "<":
                        partial += $" ({DeepGetSqlCondition(root.GetChild(0))} < {DeepGetSqlCondition(root.GetChild(1))}) ";
                        break;
                    case "<=":
                        partial += $" ({DeepGetSqlCondition(root.GetChild(0))} <= {DeepGetSqlCondition(root.GetChild(1))}) ";
                        break;
                    case ">=":
                        partial += $" ({DeepGetSqlCondition(root.GetChild(0))} >= {DeepGetSqlCondition(root.GetChild(1))}) ";
                        break;
                    case "!=":
                        partial += $" ({DeepGetSqlCondition(root.GetChild(0))} <> {DeepGetSqlCondition(root.GetChild(1))}) ";
                        break;
                    case "==":
                        partial += $" ({DeepGetSqlCondition(root.GetChild(0))} = {DeepGetSqlCondition(root.GetChild(1))}) ";
                        break;
                    default:
                        throw new Exception("Not supported!");
                }
            }
            else
            {
                // Tenemos & o |
                // Por defecto, debe tener hijos... Sino es un error.
                if (root.IsLeaf()) throw new Exception("Error, los operandos booleanos no pueden ser hojas");
                int nChilds = root.GetChilds().Count;
                partial += "(";
                for (int i = 0; i < nChilds - 1; i++)
                {
                    partial += $"{DeepGetSqlCondition(root.GetChild(i))} {(root.GetData() == "&" ? "AND" : "OR")} ";
                }
                partial += $"{DeepGetSqlCondition(root.GetChild(nChilds - 1))})";
            }

            return partial;
        }

        private static NTree<string> BuildExpressionTree(string expression)
        {
            // Primero eliminamos paréntesis exteriores...
            while (expression.StartsWith('(') && expression.EndsWith(')'))
            {
                if (BooleanExpressionParser.GetClosingParenthesisIndex(0, expression) == expression.Length - 1)
                {
                    expression = expression.Substring(1, expression.Length - 2);
                }
                else
                {
                    break;
                }
            }

            // Los tipos de raices son >=, ==, >, <=, !=, !, | y &. Donde !, | y & son prioritarios.
            NTree<string> root = new NTree<string>(GetStringLevelType(expression));

            if (root.GetData() == "!")
            {
                // Implica que empieza por !, único nodo seguro... 
                root.AddChild(BuildExpressionTree(expression.Substring(1)));
                return root;
            }

            if (root.GetData() != "&" && root.GetData() != "|")
            {
                // Si no tenemos estos dos operandos y se ha descartado !...
                // Tenemos una raíz o operandos dobles...
                switch(root.GetData())
                {
                    case ">=":
                    case "<=":
                    case "==":
                    case "!=":
                    case "<":
                    case ">":
                        string[] elements = expression.Split(root.GetData());
                        foreach (string element in elements)
                            root.AddChild(BuildExpressionTree(element.Trim()));
                        break;
                    default:
                        break;
                }

                return root;
            }

            List<string> children = new List<string>();
            string temp = "";
            int i = 0;
            while (i < expression.Length)
            {
                string currentSubstring = expression.Substring(i);
                if (currentSubstring.StartsWith("(") || currentSubstring.StartsWith("!("))
                {
                    int pstart = i + (currentSubstring.StartsWith("!(") ? 1 : 0);
                    int pend = BooleanExpressionParser.GetClosingParenthesisIndex(pstart, expression);
                    children.Add(expression.Substring(i, pend - i + 1));
                    i = pend + 1;
                    temp = "";
                    continue;
                }
                if (currentSubstring.StartsWith(root.GetData()))
                {
                    if (!string.IsNullOrWhiteSpace(temp)) children.Add(temp);
                    temp = "";
                    i += root.GetData().Length;
                    continue;
                }

                temp += expression[i];
                ++i;
            }

            if (!string.IsNullOrEmpty(temp)) children.Add(temp);

            // Para cada hijo, evaluamos su sitación
            foreach (string child in children)
                root.AddChild(BuildExpressionTree(child.Trim()));

            return root;
        }

        private static string GetStringLevelType(string expression)
        {
            // Los tipos de raices son >=, ==, >, <=, !=, !, | y &. Donde !, | y & son prioritarios, es decir, sabemos que el string
            // de entrada no puede tener &, |, ! a la vez en un mismo string, miramos si contienen esos, si no lo contienen, deberá ser de tipo
            // >=, >, ...
            int check = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                if (expression[i] == '(') ++check;
                if (expression[i] == ')') --check;
                if (check != 0) continue;
                if (expression[i] == '&') return "&";
                if (expression[i] == '|') return "|";
            }

            if (expression.StartsWith("!"))
            {
                // Si empieza por ! no tiene espacios
                if (expression.StartsWith("!("))
                {
                    if (BooleanExpressionParser.GetClosingParenthesisIndex(1, expression) == expression.Length - 1)
                        return "!";
                }
                if (!expression.Contains(" ")) return "!";

            }

            // Si no es ninguno de esos...
            if (expression.Contains(">=")) return ">=";
            if (expression.Contains("<=")) return "<=";
            if (expression.Contains("==")) return "==";
            if (expression.Contains("!=")) return "!=";
            if (expression.Contains("<")) return "<";
            if (expression.Contains(">")) return ">";

            // Tenemos una hoja, en este caso...
            return expression;
            throw new Exception($"La string {expression} es inválida");
        }
    }

    public class BooleanExpressionParser
    {
        private static List<char> m_GroupCharacters = new List<char> { '|', '&' };

        public static bool CheckBooleanExpression(string expression)
        {
            // La expresión booleana debe tener todos los paréntesis que abran y cierren.
            if (!CheckParenthesis(expression)) return false;
            if (!CheckLogicSymbols(expression)) return false;
            return true;
        }
        public static string AddParenthesis(string expression)
        {
            // Añadimos paréntesis a una expresión booleana, la expresion se ejecuta en orden de precedencia !, & y |.
            string partial = "";
            for (int i = 0; i < expression.Length; i++)
            {
                if (expression[i] == '(')
                {
                    int j = GetClosingParenthesisIndex(i, expression);
                    partial += "(" + AddParenthesis(expression.Substring(i + 1, j - i - 1)) + ")";
                    i = j;
                    continue;
                }
                partial += expression[i];
            }

            return GroupByParenthesis(GroupByParenthesis(partial, '|'), '&');
        }
        private static string GroupByParenthesis(string expression, char match)
        {
            List<int> matches = new List<int>();
            int lastNotGroupCharacter = -1;
            for (int i = 0; i < expression.Length; i++)
            {
                // Si encontamos un paréntesis no buscamos...
                if (expression[i] == '(')
                {
                    i = GetClosingParenthesisIndex(i, expression);
                    continue;
                }
                if (expression[i] == match)
                {
                    // Primero buscamos hacia la izq el último carácter...
                    int j = i - 1;
                    while (j >= 0)
                    {
                        if (expression[j] != match)
                        {
                            if (m_GroupCharacters.Contains(expression[j]))
                            {
                                j++;
                                break;
                            }
                            else if (expression[j] == ')') j = GetOpeningParenthesisIndex(j, expression);
                        }
                        --j;
                    }
                    if (j < 0) j = 0;
                    matches.Add(j);
                    // Debemos buscar el punto en el que encontramos un carácter no soportado...
                    j = i + 1;
                    while (j < expression.Length)
                    {
                        if (expression[j] != match)
                        {
                            if (m_GroupCharacters.Contains(expression[j]))
                            {
                                j--;
                                break;
                            }
                            else if (expression[j] == '(') j = GetClosingParenthesisIndex(j, expression);
                            
                        }
                        ++j;
                    }
                    if (j >= expression.Length) j = expression.Length - 1;
                    i = j; // El carácter seguro que no es match si la expresión es correcta, puede ser otro carácter.
                    matches.Add(j); // El carácter de finalización del grupo...
                }
            }

            if (matches.Count == 0) return expression;
            string result = "";
            int previous = 0;
            // Matches contiene todos los matches para cada grupo... Para cada grupo debemos retornar la string pero con los paréntesis incorporados.
            for (int i = 0; i < matches.Count && previous < expression.Length; i += 2)
            {
                int start = matches[i];
                int end = matches[i + 1];

                result += expression.Substring(previous, start - previous) + " (" + expression.Substring(start, 1 + end - start).Trim() + ") ";
                previous = end + 1;
            }
            // Si falta algo de texto, lo añadimos...
            result += expression.Substring(previous, expression.Length - previous);
            return result.Trim();
        }
        public static int GetClosingParenthesisIndex(int current, string expression)
        {
            int count = 1;

            if (expression[current] != '(') throw new Exception("Not a parenthesis!");
            for (int i = current + 1; i < expression.Length; i++)
            {
                if (expression[i] == '(') ++count;
                if (expression[i] == ')')
                {
                    --count;
                    if (count == 0) return i;
                }
            }

            return -1;
        }
        public static int GetOpeningParenthesisIndex(int current, string expression)
        {
            int count = 1;

            if (expression[current] != ')') throw new Exception("Not a parenthesis!");
            for (int i = current - 1; i >= 0; i--)
            {
                if (expression[i] == ')') ++count;
                if (expression[i] == '(')
                {
                    --count;
                    if (count == 0) return i;
                }
            }

            return -1;
        }
        
        private static bool CheckParenthesis(string expression)
        {
            int count = 0;
            for (int i = 0; i < expression.Length; ++i)
            {
                if (expression[i] == '(') ++count;
                if (expression[i] == ')') --count;
            }
            if (count == 0) return true;
            return false;
        }
        private static bool CheckLogicSymbols(string expression)
        {
            // Miramos si contiene &, |, ! seguidos...
            for (int i = 1; i < expression.Length; ++i)
            {
                if (expression[i - 1] == '!' && expression[i] == '!') return false;
                if (expression[i - 1] == '&' && expression[i] == '&') return false;
                if (expression[i - 1] == '|' && expression[i] == '|') return false;
            }

            // Miramos si los simbolos estan separados por espacios...
            for (int i = 1; i < expression.Length - 1; ++i)
            {
                char c = expression[i];
                if (c == '!' || c == '&' || c == '|')
                {
                    if (expression[i - 1] != ' ' && expression[i + 1] != ' ') return false;
                }
            }

            // Deberiamos revisar que los operadores son correctos... por ahora no.

            return true;
        }
    }
}
