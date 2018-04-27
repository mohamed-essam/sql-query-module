﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryModule.QueryParser
{
    enum NodeType
    {
        ID, FUNC_CALL, BINARY, NUMBER, SELECT, FROM
    }
    class Node
    {
        internal NodeType nodeType;
        internal List<Node> Children;
        internal Token originalToken;

        internal Node(NodeType type, Token token, List<Node> childs)
        {
            nodeType = type;
            Children = childs;
            originalToken = token;
        }

        internal Node(NodeType type, Token token, Node child)
        {
            nodeType = type;
            Children = new List<Node>();
            Children.Add(child);
            originalToken = token;
        }

        internal Node(NodeType type, Token token)
        {
            nodeType = type;
            Children = new List<Node>();
            originalToken = token;
        }
    }
    class ParserResult
    {
        internal Node selectNode;
        internal Node fromNode;
        internal Node whereNode;

        internal ParserResult(Node selectNode, Node fromNode, Node whereNode)
        {
            this.selectNode = selectNode;
            this.fromNode = fromNode;
            this.whereNode = whereNode;
        }
    }
    class ParserException : Exception
    {
        public ParserException(string message) : base(message) { }
    }
    class Parser
    {
        private static List<Token> tokens;
        private static int curToken;

        private static Token nextToken()
        {
            return tokens[++curToken];
        }

        private static Token peekToken()
        {
            return tokens[curToken + 1];
        }

        private static Token currentToken()
        {
            return tokens[curToken];
        }

        private static void skipTokens(int n)
        {
            curToken += n;
        }
        private static void assertCurrentToken(TokenType type)
        {
            if (currentToken().tokenType != type)
            {
                throw new ParserException("Error parsing query, expected: " + type.ToString() + ", found: " + currentToken().tokenType.ToString());
            }
        }

        private static void assertNextToken(TokenType type)
        {
            if (peekToken().tokenType != type)
            {
                throw new ParserException("Error parsing query, expected: " + type.ToString() + ", found: " + currentToken().tokenType.ToString());
            }
        }

        private static bool checkSequence(params TokenType[] types)
        {
            if (curToken + types.Length > tokens.Count) 
                return false;
            for (int i = 0; i < types.Length; i++)
            {
                if(tokens[curToken + i].tokenType != types[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static ParserResult parse(List<Token> tokens)
        {
            return null;
        }

        private static Node selectStatement()
        {
            assertCurrentToken(TokenType.SELECT);
            Token selectToken = currentToken();
            skipTokens(1);
            Node retNode = new Node(NodeType.SELECT, selectToken, expression());
            if (retNode.Children[0] == null)
            {
                throw new ParserException("Expected comma separated expression list after SELECT directive");
            }
            while (currentToken().tokenType == TokenType.COMMA)
            {
                skipTokens(1);
                Node expressionNode = expression();
                if (expressionNode == null)
                {
                    throw new ParserException("Expected comma separated expression list after SELECT directive");
                }
                retNode.Children.Add(expressionNode);
            }
            return retNode;
        }

        private static Node fromStatement()
        {
            assertCurrentToken(TokenType.FROM);
            Token fromToken = currentToken();
            skipTokens(1);
            Node retNode = new Node(NodeType.FROM, fromToken, id());
            if(retNode.Children[0] == null)
            {
                throw new ParserException("Expected identifier after FROM directive");
            }
            return retNode;
        }

        private static Node whereStatement()
        {
            if(curToken >= tokens.Count)
            {
                return null;
            }
            assertCurrentToken(TokenType.WHERE);
            Token whereToken = currentToken();
            skipTokens(1);
            Node retNode = new Node(NodeType.FROM, whereToken, expression());

            return retNode;
        }

        private static Node id()
        {
            Node ret = func_call();
            if(ret != null)
            {
                return ret;
            }
            if(currentToken().tokenType == TokenType.ID)
            {
                return new Node(NodeType.ID, currentToken()); 
            }
            return null;
        }

        private static Node func_call()
        {
            if(!checkSequence(TokenType.ID, TokenType.L_PARA))
            {
                return null;
            }
            Token funcNameToken = currentToken();
            skipTokens(2);
            Node expressionNode = expression();
            assertCurrentToken(TokenType.R_PARA);
            skipTokens(1);

            return new Node(NodeType.FUNC_CALL, funcNameToken, expressionNode);
        }

        private static Node expression()
        {
            return logic_expression();
        }

        private static Node logic_expression()
        {
            Node left = add_expression();
            if(currentToken().tokenType == TokenType.OP && currentToken().isComparison())
            {
                Node compNode = new Node(NodeType.BINARY, nextToken(), left);
                Node right = add_expression();
                compNode.Children.Add(right);
                return compNode;
            }
            return left;
        }

        private static Node add_expression()
        {
            Node left = mul_expression();
            if (currentToken().tokenType == TokenType.OP && currentToken().isAddition())
            {
                Node compNode = new Node(NodeType.BINARY, nextToken(), left);
                Node right = mul_expression();
                compNode.Children.Add(right);
                return compNode;
            }
            return left;
        }

        private static Node mul_expression()
        {
            Node left = factor();
            if (currentToken().tokenType == TokenType.OP && currentToken().isMultiplication())
            {
                Node compNode = new Node(NodeType.BINARY, nextToken(), left);
                Node right = factor();
                compNode.Children.Add(right);
                return compNode;
            }
            return left;
        }

        private static Node factor()
        {
            if(currentToken().tokenType == TokenType.NUM)
            {
                Node ret = new Node(NodeType.NUMBER, currentToken());
                skipTokens(1);
                return ret;
            }
            Node idNode = id();
            if (idNode != null) return idNode;
            if(currentToken().tokenType == TokenType.OP && currentToken().lexeme == "-")
            {
                Token negativeToken = currentToken();
                skipTokens(1);
                Node factorNode = factor();
                Node retNode = new Node(NodeType.BINARY, negativeToken, new Node(NodeType.NUMBER, new Token(TokenType.OP, "-")));
                retNode.Children.Add(factorNode);
                return retNode;
            }
            if(currentToken().tokenType == TokenType.L_PARA)
            {
                skipTokens(1);
                Node retNode = expression();
                assertCurrentToken(TokenType.R_PARA);
                return retNode;
            }
            return null;
        }
    }
}
