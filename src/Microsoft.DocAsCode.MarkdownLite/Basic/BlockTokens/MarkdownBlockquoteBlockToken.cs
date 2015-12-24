﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownBlockquoteBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownBlockquoteBlockToken>
    {
        public MarkdownBlockquoteBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> tokens, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Tokens = tokens;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Tokens { get; }

        public string RawMarkdown { get; set; }

        public MarkdownBlockquoteBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Tokens);
            if (tokens == Tokens)
            {
                return this;
            }
            return new MarkdownBlockquoteBlockToken(Rule, Context, tokens, RawMarkdown);
        }
    }
}
