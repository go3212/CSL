using BenchmarkDotNet.Attributes;
using CSL.Trees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CSL.BooleanExpression
{
    public delegate Task<dynamic> Delegate(int argc, dynamic[] argv);

    // Esta clase basicamente se encarga de proporcionar funcionalidad de "ejecución" a la expresión booleana.
    // Profundiza y hace un TOKENIZADO "avanzado". Tiene la capacidad de ejecutar algunas funciones especiales...
    public class ExecutableExpression : BooleanExpression
    {
        private Dictionary<string, dynamic?> m_Variables;
        private Dictionary<string, Delegate?> m_FunctionTokens;
        private Dictionary<string, BooleanExpressionTokenType> m_TokenTypeCache;

        public ExecutableExpression(string expression) 
            : base(expression)
        {
            // Tenemos un árbol con todos los tokens bien separados.
            // Debemos registrar los "parámetros" de entrada...
            // Tambien debemos detectar las funciones y expandir el arbol acordemente.
            m_Variables = new Dictionary<string, dynamic>();
            m_FunctionTokens = new Dictionary<string, Delegate?>();
            m_TokenTypeCache = new Dictionary<string, BooleanExpressionTokenType>();
            ExpandFunctions(m_ExpressionTree);
        }

        private void ExpandFunctions(NTree<string> nTree)
        {
            var data = nTree.GetData().Trim();
            // Solo tenemos que expandir las funciones... 
            // Todas las funciones son del estilo f(x), f(g(x), k, l(s(k)))... Si hay un parentesis ( debe acabar con otro parentesis ).
            // Inv: seguro que si es una función, es una hoja... Asumimos que la expresión es correcta.
            string token;
            if (data.Contains('('))
            {
                int pStartIndex = 0;
                for (; pStartIndex < data.Length && data[pStartIndex] != '('; ++pStartIndex);
                int pEndIndex = data.Length - 1;

                token = data.Substring(0, pStartIndex);

                var argsSubst = data.Substring(pStartIndex + 1, pEndIndex - pStartIndex - 1);
                string[] args = BooleanExpressionParser.SplitOutsideOfParenthesis(argsSubst, ',');
                nTree.SetData(token);
                for (int i = 0; i < args.Length; ++i) nTree.AddChild(args[i].Trim());

                // Es un token de funcion...
                if (!m_FunctionTokens.ContainsKey(token))
                    m_FunctionTokens.Add(token, null);
            }
            else
            {
                if (IsVariable(data) && !m_Variables.ContainsKey(data))
                    m_Variables.Add(data, null);
            }

            // Para cada hijo del token actual, expandimos.
            int childs = nTree.GetChilds().Count;
            for (int i = 0; i < childs; ++i)
                ExpandFunctions(nTree.GetChild(i));
        }

        public string[] GetFunctions()
        {
            string[] functions = new string[m_FunctionTokens.Count];
            List<string> keys = new List<string>(m_FunctionTokens.Keys);
            for (int i = 0; i < functions.Length; ++i)
                functions[i] = keys[i];
            return functions;
        }

        public void SetFunction(string functionToken, Delegate function)
        {
            m_FunctionTokens[functionToken] = function;
        }

        public string[] GetVariables()
        {
            string[] vars = new string[m_Variables.Count];
            List<string> keys = new List<string>(m_Variables.Keys);
            for (int i = 0; i < vars.Length; ++i)
                vars[i] = keys[i];
            return vars;
        }

        public void SetVariable(string variable, dynamic value)
        {
            m_Variables[variable] = value;
        }

        public bool IsVariable(string str)
        {
            return GetTokenType(str) == BooleanExpressionTokenType.VARIABLE;
        }

        public Task<dynamic> ExecuteAsync()
        {
            return ExecuteDeepAsync(m_ExpressionTree);
        }

        private async Task<dynamic> ExecuteDeepAsync(NTree<string> nTree)
        {
            var token = nTree.GetData();
            var tokenType = GetTokenTypeCached(token);

            switch (tokenType)
            {
                case BooleanExpressionTokenType.LESS_THAN:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) < await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.LESS_THAN_OR_EQUAL:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) <= await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.GREATER_THAN:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) > await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.GREATER_THAN_OR_EQUAL:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) >= await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.EQUAL:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) == await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.NOT_EQUAL:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) != await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.AND:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) && await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.OR:
                    return await ExecuteDeepAsync(nTree.GetChild(0)) || await ExecuteDeepAsync(nTree.GetChild(1));
                case BooleanExpressionTokenType.NOT:
                    return !(await ExecuteDeepAsync(nTree.GetChild(0)));
                case BooleanExpressionTokenType.FUNCTION:
                    {
                        // Primero resolvemos los hijos...

                        int numberOfChildren = nTree.GetChilds().Count;
                        dynamic[] args = new dynamic[numberOfChildren];
                        for (int i = 0; i < numberOfChildren; i++)
                            args[i] = await ExecuteDeepAsync(nTree.GetChild(i));
                        return await m_FunctionTokens[token].Invoke(numberOfChildren, args);
                    }
                    break;
                case BooleanExpressionTokenType.STRING:
                    return token.Substring(1, token.Length - 2);
                case BooleanExpressionTokenType.BOOLEAN:
                    return Boolean.Parse(token);
                case BooleanExpressionTokenType.INTEGER:
                    return int.Parse(token);
                case BooleanExpressionTokenType.FLOAT:
                    return float.Parse(token);
                case BooleanExpressionTokenType.VARIABLE:
                    return m_Variables[token];
            }
            return "false";
        }

        private BooleanExpressionTokenType GetTokenTypeCached(string token)
        {
            if (!m_TokenTypeCache.ContainsKey(token))
                m_TokenTypeCache.Add(token, GetTokenType(token));
            return m_TokenTypeCache[token];
        }

        protected override BooleanExpressionTokenType GetTokenType(string token)
        {
            var parsedToken = token.Trim();
            var baseTokenType = base.GetTokenType(parsedToken);
            // Si no es un STRING, devolvemos.
            if (baseTokenType != BooleanExpressionTokenType.UNKNOWN) return baseTokenType;

            // Miramos si es una funcion...
            if (m_FunctionTokens.ContainsKey(parsedToken)) return BooleanExpressionTokenType.FUNCTION;

            // De momento si no es ni Funcion ni operador logico, es variable...
            if (parsedToken.Contains('"')) return BooleanExpressionTokenType.STRING;
            if (parsedToken.Equals("true") || parsedToken.Equals("false")) return BooleanExpressionTokenType.BOOLEAN;
            int iP;
            if (int.TryParse(parsedToken, out iP)) return BooleanExpressionTokenType.INTEGER;
            float fP;
            if (float.TryParse(parsedToken, out fP)) return BooleanExpressionTokenType.FLOAT;
            return BooleanExpressionTokenType.VARIABLE;
        }
    }
}
