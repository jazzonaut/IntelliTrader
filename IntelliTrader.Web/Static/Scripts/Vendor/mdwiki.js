(function () {

    /**
     * Block-Level Grammar
     */

    var block = {
        newline: /^\n+/,
        code: /^( {4}[^\n]+\n*)+/,
        fences: noop,
        hr: /^( *[-*_]){3,} *(?:\n+|$)/,
        heading: /^ *(#{1,6}) *([^\n]+?) *#* *(?:\n+|$)/,
        nptable: noop,
        lheading: /^([^\n]+)\n *(=|-){3,} *\n*/,
        blockquote: /^( *>[^\n]+(\n[^\n]+)*\n*)+/,
        list: /^( *)(bull) [\s\S]+?(?:hr|\n{2,}(?! )(?!\1bull )\n*|\s*$)/,
        html: /^ *(?:comment|closed|closing) *(?:\n{2,}|\s*$)/,
        def: /^ *\[([^\]]+)\]: *<?([^\s>]+)>?(?: +["(]([^\n]+)[")])? *(?:\n+|$)/,
        table: noop,
        paragraph: /^((?:[^\n]+\n?(?!hr|heading|lheading|blockquote|tag|def))+)\n*/,
        text: /^[^\n]+/
    };

    block.bullet = /(?:[*+-]|\d+\.)/;
    block.item = /^( *)(bull) [^\n]*(?:\n(?!\1bull )[^\n]*)*/;
    block.item = replace(block.item, 'gm')
        (/bull/g, block.bullet)
        ();

    block.list = replace(block.list)
        (/bull/g, block.bullet)
        ('hr', /\n+(?=(?: *[-*_]){3,} *(?:\n+|$))/)
        ();

    block._tag = '(?!(?:'
        + 'a|em|strong|small|s|cite|q|dfn|abbr|data|time|code'
        + '|var|samp|kbd|sub|sup|i|b|u|mark|ruby|rt|rp|bdi|bdo'
        + '|span|br|wbr|ins|del|img)\\b)\\w+(?!:/|@)\\b';

    block.html = replace(block.html)
        ('comment', /<!--[\s\S]*?-->/)
        ('closed', /<(tag)[\s\S]+?<\/\1>/)
        ('closing', /<tag(?:"[^"]*"|'[^']*'|[^'">])*?>/)
        (/tag/g, block._tag)
        ();

    block.paragraph = replace(block.paragraph)
        ('hr', block.hr)
        ('heading', block.heading)
        ('lheading', block.lheading)
        ('blockquote', block.blockquote)
        ('tag', '<' + block._tag)
        ('def', block.def)
        ();

    /**
     * Normal Block Grammar
     */

    block.normal = merge({}, block);

    /**
     * GFM Block Grammar
     */

    block.gfm = merge({}, block.normal, {
        fences: /^ *(`{3,}|~{3,}) *(\S+)? *\n([\s\S]+?)\s*\1 *(?:\n+|$)/,
        paragraph: /^/
    });

    block.gfm.paragraph = replace(block.paragraph)
        ('(?!', '(?!' + block.gfm.fences.source.replace('\\1', '\\2') + '|')
        ();

    /**
     * GFM + Tables Block Grammar
     */

    block.tables = merge({}, block.gfm, {
        nptable: /^ *(\S.*\|.*)\n *([-:]+ *\|[-| :]*)\n((?:.*\|.*(?:\n|$))*)\n*/,
        table: /^ *\|(.+)\n *\|( *[-:]+[-| :]*)\n((?: *\|.*(?:\n|$))*)\n*/
    });

    /**
     * Block Lexer
     */

    function Lexer(options) {
        this.tokens = [];
        this.tokens.links = {};
        this.options = options || marked.defaults;
        this.rules = block.normal;

        if (this.options.gfm) {
            if (this.options.tables) {
                this.rules = block.tables;
            } else {
                this.rules = block.gfm;
            }
        }
    }

    /**
     * Expose Block Rules
     */

    Lexer.rules = block;

    /**
     * Static Lex Method
     */

    Lexer.lex = function (src, options) {
        var lexer = new Lexer(options);
        return lexer.lex(src);
    };

    /**
     * Preprocessing
     */

    Lexer.prototype.lex = function (src) {
        src = src
            .replace(/\r\n|\r/g, '\n')
            .replace(/\t/g, '    ')
            .replace(/\u00a0/g, ' ')
            .replace(/\u2424/g, '\n');

        return this.token(src, true);
    };

    /**
     * Lexing
     */

    Lexer.prototype.token = function (src, top) {
        var src = src.replace(/^ +$/gm, '')
            , next
            , loose
            , cap
            , bull
            , b
            , item
            , space
            , i
            , l;

        while (src) {
            // newline
            if (cap = this.rules.newline.exec(src)) {
                src = src.substring(cap[0].length);
                if (cap[0].length > 1) {
                    this.tokens.push({
                        type: 'space'
                    });
                }
            }

            // code
            if (cap = this.rules.code.exec(src)) {
                src = src.substring(cap[0].length);
                cap = cap[0].replace(/^ {4}/gm, '');
                this.tokens.push({
                    type: 'code',
                    text: !this.options.pedantic
                        ? cap.replace(/\n+$/, '')
                        : cap
                });
                continue;
            }

            // fences (gfm)
            if (cap = this.rules.fences.exec(src)) {
                src = src.substring(cap[0].length);
                this.tokens.push({
                    type: 'code',
                    lang: cap[2],
                    text: cap[3]
                });
                continue;
            }

            // heading
            if (cap = this.rules.heading.exec(src)) {
                src = src.substring(cap[0].length);
                this.tokens.push({
                    type: 'heading',
                    depth: cap[1].length,
                    text: cap[2]
                });
                continue;
            }

            // table no leading pipe (gfm)
            if (top && (cap = this.rules.nptable.exec(src))) {
                src = src.substring(cap[0].length);

                item = {
                    type: 'table',
                    header: cap[1].replace(/^ *| *\| *$/g, '').split(/ *\| */),
                    align: cap[2].replace(/^ *|\| *$/g, '').split(/ *\| */),
                    cells: cap[3].replace(/\n$/, '').split('\n')
                };

                for (i = 0; i < item.align.length; i++) {
                    if (/^ *-+: *$/.test(item.align[i])) {
                        item.align[i] = 'right';
                    } else if (/^ *:-+: *$/.test(item.align[i])) {
                        item.align[i] = 'center';
                    } else if (/^ *:-+ *$/.test(item.align[i])) {
                        item.align[i] = 'left';
                    } else {
                        item.align[i] = null;
                    }
                }

                for (i = 0; i < item.cells.length; i++) {
                    item.cells[i] = item.cells[i].split(/ *\| */);
                }

                this.tokens.push(item);

                continue;
            }

            // lheading
            if (cap = this.rules.lheading.exec(src)) {
                src = src.substring(cap[0].length);
                this.tokens.push({
                    type: 'heading',
                    depth: cap[2] === '=' ? 1 : 2,
                    text: cap[1]
                });
                continue;
            }

            // hr
            if (cap = this.rules.hr.exec(src)) {
                src = src.substring(cap[0].length);
                this.tokens.push({
                    type: 'hr'
                });
                continue;
            }

            // blockquote
            if (cap = this.rules.blockquote.exec(src)) {
                src = src.substring(cap[0].length);

                this.tokens.push({
                    type: 'blockquote_start'
                });

                cap = cap[0].replace(/^ *> ?/gm, '');

                // Pass `top` to keep the current
                // "toplevel" state. This is exactly
                // how markdown.pl works.
                this.token(cap, top);

                this.tokens.push({
                    type: 'blockquote_end'
                });

                continue;
            }

            // list
            if (cap = this.rules.list.exec(src)) {
                src = src.substring(cap[0].length);
                bull = cap[2];

                this.tokens.push({
                    type: 'list_start',
                    ordered: bull.length > 1
                });

                // Get each top-level item.
                cap = cap[0].match(this.rules.item);

                next = false;
                l = cap.length;
                i = 0;

                for (; i < l; i++) {
                    item = cap[i];

                    // Remove the list item's bullet
                    // so it is seen as the next token.
                    space = item.length;
                    item = item.replace(/^ *([*+-]|\d+\.) +/, '');

                    // Outdent whatever the
                    // list item contains. Hacky.
                    if (~item.indexOf('\n ')) {
                        space -= item.length;
                        item = !this.options.pedantic
                            ? item.replace(new RegExp('^ {1,' + space + '}', 'gm'), '')
                            : item.replace(/^ {1,4}/gm, '');
                    }

                    // Determine whether the next list item belongs here.
                    // Backpedal if it does not belong in this list.
                    if (this.options.smartLists && i !== l - 1) {
                        b = block.bullet.exec(cap[i + 1])[0];
                        if (bull !== b && !(bull.length > 1 && b.length > 1)) {
                            src = cap.slice(i + 1).join('\n') + src;
                            i = l - 1;
                        }
                    }

                    // Determine whether item is loose or not.
                    // Use: /(^|\n)(?! )[^\n]+\n\n(?!\s*$)/
                    // for discount behavior.
                    loose = next || /\n\n(?!\s*$)/.test(item);
                    if (i !== l - 1) {
                        next = item[item.length - 1] === '\n';
                        if (!loose) loose = next;
                    }

                    this.tokens.push({
                        type: loose
                            ? 'loose_item_start'
                            : 'list_item_start'
                    });

                    // Recurse.
                    this.token(item, false);

                    this.tokens.push({
                        type: 'list_item_end'
                    });
                }

                this.tokens.push({
                    type: 'list_end'
                });

                continue;
            }

            // html
            if (cap = this.rules.html.exec(src)) {
                src = src.substring(cap[0].length);
                this.tokens.push({
                    type: this.options.sanitize
                        ? 'paragraph'
                        : 'html',
                    pre: cap[1] === 'pre' || cap[1] === 'script',
                    text: cap[0]
                });
                continue;
            }

            // def
            if (top && (cap = this.rules.def.exec(src))) {
                src = src.substring(cap[0].length);
                this.tokens.links[cap[1].toLowerCase()] = {
                    href: cap[2],
                    title: cap[3]
                };
                continue;
            }

            // table (gfm)
            if (top && (cap = this.rules.table.exec(src))) {
                src = src.substring(cap[0].length);

                item = {
                    type: 'table',
                    header: cap[1].replace(/^ *| *\| *$/g, '').split(/ *\| */),
                    align: cap[2].replace(/^ *|\| *$/g, '').split(/ *\| */),
                    cells: cap[3].replace(/(?: *\| *)?\n$/, '').split('\n')
                };

                for (i = 0; i < item.align.length; i++) {
                    if (/^ *-+: *$/.test(item.align[i])) {
                        item.align[i] = 'right';
                    } else if (/^ *:-+: *$/.test(item.align[i])) {
                        item.align[i] = 'center';
                    } else if (/^ *:-+ *$/.test(item.align[i])) {
                        item.align[i] = 'left';
                    } else {
                        item.align[i] = null;
                    }
                }

                for (i = 0; i < item.cells.length; i++) {
                    item.cells[i] = item.cells[i]
                        .replace(/^ *\| *| *\| *$/g, '')
                        .split(/ *\| */);
                }

                this.tokens.push(item);

                continue;
            }

            // top-level paragraph
            if (top && (cap = this.rules.paragraph.exec(src))) {
                src = src.substring(cap[0].length);
                this.tokens.push({
                    type: 'paragraph',
                    text: cap[1][cap[1].length - 1] === '\n'
                        ? cap[1].slice(0, -1)
                        : cap[1]
                });
                continue;
            }

            // text
            if (cap = this.rules.text.exec(src)) {
                // Top-level should never reach here.
                src = src.substring(cap[0].length);
                this.tokens.push({
                    type: 'text',
                    text: cap[0]
                });
                continue;
            }

            if (src) {
                throw new
                    Error('Infinite loop on byte: ' + src.charCodeAt(0));
            }
        }

        return this.tokens;
    };

    /**
     * Inline-Level Grammar
     */

    var inline = {
        escape: /^\\([\\`*{}\[\]()#+\-.!_>])/,
        autolink: /^<([^ >]+(@|:\/)[^ >]+)>/,
        url: noop,
        tag: /^<!--[\s\S]*?-->|^<\/?\w+(?:"[^"]*"|'[^']*'|[^'">])*?>/,
        link: /^!?\[(inside)\]\(href\)/,
        reflink: /^!?\[(inside)\]\s*\[([^\]]*)\]/,
        nolink: /^!?\[((?:\[[^\]]*\]|[^\[\]])*)\]/,
        strong: /^__([\s\S]+?)__(?!_)|^\*\*([\s\S]+?)\*\*(?!\*)/,
        em: /^\b_((?:__|[\s\S])+?)_\b|^\*((?:\*\*|[\s\S])+?)\*(?!\*)/,
        code: /^(`+)\s*([\s\S]*?[^`])\s*\1(?!`)/,
        br: /^ {2,}\n(?!\s*$)/,
        del: noop,
        text: /^[\s\S]+?(?=[\\<!\[_*`]| {2,}\n|$)/
    };

    inline._inside = /(?:\[[^\]]*\]|[^\]]|\](?=[^\[]*\]))*/;
    inline._href = /\s*<?(.*?)>?(?:\s+['"]([\s\S]*?)['"])?\s*/;

    inline.link = replace(inline.link)
        ('inside', inline._inside)
        ('href', inline._href)
        ();

    inline.reflink = replace(inline.reflink)
        ('inside', inline._inside)
        ();

    /**
     * Normal Inline Grammar
     */

    inline.normal = merge({}, inline);

    /**
     * Pedantic Inline Grammar
     */

    inline.pedantic = merge({}, inline.normal, {
        strong: /^__(?=\S)([\s\S]*?\S)__(?!_)|^\*\*(?=\S)([\s\S]*?\S)\*\*(?!\*)/,
        em: /^_(?=\S)([\s\S]*?\S)_(?!_)|^\*(?=\S)([\s\S]*?\S)\*(?!\*)/
    });

    /**
     * GFM Inline Grammar
     */

    inline.gfm = merge({}, inline.normal, {
        escape: replace(inline.escape)('])', '~|])')(),
        url: /^(https?:\/\/[^\s<]+[^<.,:;"')\]\s])/,
        del: /^~~(?=\S)([\s\S]*?\S)~~/,
        text: replace(inline.text)
            (']|', '~]|')
            ('|', '|https?://|')
            ()
    });

    /**
     * GFM + Line Breaks Inline Grammar
     */

    inline.breaks = merge({}, inline.gfm, {
        br: replace(inline.br)('{2,}', '*')(),
        text: replace(inline.gfm.text)('{2,}', '*')()
    });

    /**
     * Inline Lexer & Compiler
     */

    function InlineLexer(links, options) {
        this.options = options || marked.defaults;
        this.links = links;
        this.rules = inline.normal;

        if (!this.links) {
            throw new
                Error('Tokens array requires a `links` property.');
        }

        if (this.options.gfm) {
            if (this.options.breaks) {
                this.rules = inline.breaks;
            } else {
                this.rules = inline.gfm;
            }
        } else if (this.options.pedantic) {
            this.rules = inline.pedantic;
        }
    }

    /**
     * Expose Inline Rules
     */

    InlineLexer.rules = inline;

    /**
     * Static Lexing/Compiling Method
     */

    InlineLexer.output = function (src, links, options) {
        var inline = new InlineLexer(links, options);
        return inline.output(src);
    };

    /**
     * Lexing/Compiling
     */

    InlineLexer.prototype.output = function (src) {
        var out = ''
            , link
            , text
            , href
            , cap;

        while (src) {
            // escape
            if (cap = this.rules.escape.exec(src)) {
                src = src.substring(cap[0].length);
                out += cap[1];
                continue;
            }

            // autolink
            if (cap = this.rules.autolink.exec(src)) {
                src = src.substring(cap[0].length);
                if (cap[2] === '@') {
                    text = cap[1][6] === ':'
                        ? this.mangle(cap[1].substring(7))
                        : this.mangle(cap[1]);
                    href = this.mangle('mailto:') + text;
                } else {
                    text = escape(cap[1]);
                    href = text;
                }
                out += '<a href="'
                    + href
                    + '">'
                    + text
                    + '</a>';
                continue;
            }

            // url (gfm)
            if (cap = this.rules.url.exec(src)) {
                src = src.substring(cap[0].length);
                text = escape(cap[1]);
                href = text;
                out += '<a href="'
                    + href
                    + '">'
                    + text
                    + '</a>';
                continue;
            }

            // tag
            if (cap = this.rules.tag.exec(src)) {
                src = src.substring(cap[0].length);
                out += this.options.sanitize
                    ? escape(cap[0])
                    : cap[0];
                continue;
            }

            // link
            if (cap = this.rules.link.exec(src)) {
                src = src.substring(cap[0].length);
                out += this.outputLink(cap, {
                    href: cap[2],
                    title: cap[3]
                });
                continue;
            }

            // reflink, nolink
            if ((cap = this.rules.reflink.exec(src))
                || (cap = this.rules.nolink.exec(src))) {
                src = src.substring(cap[0].length);
                link = (cap[2] || cap[1]).replace(/\s+/g, ' ');
                link = this.links[link.toLowerCase()];
                if (!link || !link.href) {
                    out += cap[0][0];
                    src = cap[0].substring(1) + src;
                    continue;
                }
                out += this.outputLink(cap, link);
                continue;
            }

            // strong
            if (cap = this.rules.strong.exec(src)) {
                src = src.substring(cap[0].length);
                out += '<strong>'
                    + this.output(cap[2] || cap[1])
                    + '</strong>';
                continue;
            }

            // em
            if (cap = this.rules.em.exec(src)) {
                src = src.substring(cap[0].length);
                out += '<em>'
                    + this.output(cap[2] || cap[1])
                    + '</em>';
                continue;
            }

            // code
            if (cap = this.rules.code.exec(src)) {
                src = src.substring(cap[0].length);
                out += '<code>'
                    + escape(cap[2], true)
                    + '</code>';
                continue;
            }

            // br
            if (cap = this.rules.br.exec(src)) {
                src = src.substring(cap[0].length);
                out += '<br>';
                continue;
            }

            // del (gfm)
            if (cap = this.rules.del.exec(src)) {
                src = src.substring(cap[0].length);
                out += '<del>'
                    + this.output(cap[1])
                    + '</del>';
                continue;
            }

            // text
            if (cap = this.rules.text.exec(src)) {
                src = src.substring(cap[0].length);
                out += escape(cap[0]);
                continue;
            }

            if (src) {
                throw new
                    Error('Infinite loop on byte: ' + src.charCodeAt(0));
            }
        }

        return out;
    };

    /**
     * Compile Link
     */

    InlineLexer.prototype.outputLink = function (cap, link) {
        if (cap[0][0] !== '!') {
            return '<a href="'
                + escape(link.href)
                + '"'
                + (link.title
                    ? ' title="'
                    + escape(link.title)
                    + '"'
                    : '')
                + '>'
                + this.output(cap[1])
                + '</a>';
        } else {
            return '<img src="'
                + escape(link.href)
                + '" alt="'
                + escape(cap[1])
                + '"'
                + (link.title
                    ? ' title="'
                    + escape(link.title)
                    + '"'
                    : '')
                + '>';
        }
    };

    /**
     * Smartypants Transformations
     */

    InlineLexer.prototype.smartypants = function (text) {
        if (!this.options.smartypants) return text;
        return text
            .replace(/--/g, '—')
            .replace(/'([^']*)'/g, '‘$1’')
            .replace(/"([^"]*)"/g, '“$1”')
            .replace(/\.{3}/g, '…');
    };

    /**
     * Mangle Links
     */

    InlineLexer.prototype.mangle = function (text) {
        var out = ''
            , l = text.length
            , i = 0
            , ch;

        for (; i < l; i++) {
            ch = text.charCodeAt(i);
            if (Math.random() > 0.5) {
                ch = 'x' + ch.toString(16);
            }
            out += '&#' + ch + ';';
        }

        return out;
    };

    /**
     * Parsing & Compiling
     */

    function Parser(options) {
        this.tokens = [];
        this.token = null;
        this.options = options || marked.defaults;
    }

    /**
     * Static Parse Method
     */

    Parser.parse = function (src, options) {
        var parser = new Parser(options);
        return parser.parse(src);
    };

    /**
     * Parse Loop
     */

    Parser.prototype.parse = function (src) {
        this.inline = new InlineLexer(src.links, this.options);
        this.tokens = src.reverse();

        var out = '';
        while (this.next()) {
            out += this.tok();
        }

        return out;
    };

    /**
     * Next Token
     */

    Parser.prototype.next = function () {
        return this.token = this.tokens.pop();
    };

    /**
     * Preview Next Token
     */

    Parser.prototype.peek = function () {
        return this.tokens[this.tokens.length - 1] || 0;
    };

    /**
     * Parse Text Tokens
     */

    Parser.prototype.parseText = function () {
        var body = this.token.text;

        while (this.peek().type === 'text') {
            body += '\n' + this.next().text;
        }

        return this.inline.output(body);
    };

    /**
     * Parse Current Token
     */

    Parser.prototype.tok = function () {
        switch (this.token.type) {
            case 'space': {
                return '';
            }
            case 'hr': {
                return '<hr>\n';
            }
            case 'heading': {
                return '<h'
                    + this.token.depth
                    + '>'
                    + this.inline.output(this.token.text)
                    + '</h'
                    + this.token.depth
                    + '>\n';
            }
            case 'code': {
                if (this.options.highlight) {
                    var code = this.options.highlight(this.token.text, this.token.lang);
                    if (code != null && code !== this.token.text) {
                        this.token.escaped = true;
                        this.token.text = code;
                    }
                }

                if (!this.token.escaped) {
                    this.token.text = escape(this.token.text, true);
                }

                return '<pre><code'
                    + (this.token.lang
                        ? ' class="'
                        + this.options.langPrefix
                        + this.token.lang
                        + '"'
                        : '')
                    + '>'
                    + this.token.text
                    + '</code></pre>\n';
            }
            case 'table': {
                var body = ''
                    , heading
                    , i
                    , row
                    , cell
                    , j;

                // header
                body += '<thead>\n<tr>\n';
                for (i = 0; i < this.token.header.length; i++) {
                    heading = this.inline.output(this.token.header[i]);
                    body += this.token.align[i]
                        ? '<th align="' + this.token.align[i] + '">' + heading + '</th>\n'
                        : '<th>' + heading + '</th>\n';
                }
                body += '</tr>\n</thead>\n';

                // body
                body += '<tbody>\n'
                for (i = 0; i < this.token.cells.length; i++) {
                    row = this.token.cells[i];
                    body += '<tr>\n';
                    for (j = 0; j < row.length; j++) {
                        cell = this.inline.output(row[j]);
                        body += this.token.align[j]
                            ? '<td align="' + this.token.align[j] + '">' + cell + '</td>\n'
                            : '<td>' + cell + '</td>\n';
                    }
                    body += '</tr>\n';
                }
                body += '</tbody>\n';

                return '<table>\n'
                    + body
                    + '</table>\n';
            }
            case 'blockquote_start': {
                var body = '';

                while (this.next().type !== 'blockquote_end') {
                    body += this.tok();
                }

                return '<blockquote>\n'
                    + body
                    + '</blockquote>\n';
            }
            case 'list_start': {
                var type = this.token.ordered ? 'ol' : 'ul'
                    , body = '';

                while (this.next().type !== 'list_end') {
                    body += this.tok();
                }

                return '<'
                    + type
                    + '>\n'
                    + body
                    + '</'
                    + type
                    + '>\n';
            }
            case 'list_item_start': {
                var body = '';

                while (this.next().type !== 'list_item_end') {
                    body += this.token.type === 'text'
                        ? this.parseText()
                        : this.tok();
                }

                return '<li>'
                    + body
                    + '</li>\n';
            }
            case 'loose_item_start': {
                var body = '';

                while (this.next().type !== 'list_item_end') {
                    body += this.tok();
                }

                return '<li>'
                    + body
                    + '</li>\n';
            }
            case 'html': {
                return !this.token.pre && !this.options.pedantic
                    ? this.inline.output(this.token.text)
                    : this.token.text;
            }
            case 'paragraph': {
                return '<p>'
                    + this.inline.output(this.token.text)
                    + '</p>\n';
            }
            case 'text': {
                return '<p>'
                    + this.parseText()
                    + '</p>\n';
            }
        }
    };

    /**
     * Helpers
     */

    function escape(html, encode) {
        return html
            .replace(!encode ? /&(?!#?\w+;)/g : /&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function replace(regex, opt) {
        regex = regex.source;
        opt = opt || '';
        return function self(name, val) {
            if (!name) return new RegExp(regex, opt);
            val = val.source || val;
            val = val.replace(/(^|[^\[])\^/g, '$1');
            regex = regex.replace(name, val);
            return self;
        };
    }

    function noop() { }
    noop.exec = noop;

    function merge(obj) {
        var i = 1
            , target
            , key;

        for (; i < arguments.length; i++) {
            target = arguments[i];
            for (key in target) {
                if (Object.prototype.hasOwnProperty.call(target, key)) {
                    obj[key] = target[key];
                }
            }
        }

        return obj;
    }

    /**
     * Marked
     */

    function marked(src, opt, callback) {
        if (callback || typeof opt === 'function') {
            if (!callback) {
                callback = opt;
                opt = null;
            }

            if (opt) opt = merge({}, marked.defaults, opt);

            var tokens = Lexer.lex(tokens, opt)
                , highlight = opt.highlight
                , pending = 0
                , l = tokens.length
                , i = 0;

            if (!highlight || highlight.length < 3) {
                return callback(null, Parser.parse(tokens, opt));
            }

            var done = function () {
                delete opt.highlight;
                var out = Parser.parse(tokens, opt);
                opt.highlight = highlight;
                return callback(null, out);
            };

            for (; i < l; i++) {
                (function (token) {
                    if (token.type !== 'code') return;
                    pending++;
                    return highlight(token.text, token.lang, function (err, code) {
                        if (code == null || code === token.text) {
                            return --pending || done();
                        }
                        token.text = code;
                        token.escaped = true;
                        --pending || done();
                    });
                })(tokens[i]);
            }

            return;
        }
        try {
            if (opt) opt = merge({}, marked.defaults, opt);
            return Parser.parse(Lexer.lex(src, opt), opt);
        } catch (e) {
            e.message += '\nPlease report this to https://github.com/chjj/marked.';
            if ((opt || marked.defaults).silent) {
                return '<p>An error occured:</p><pre>'
                    + escape(e.message + '', true)
                    + '</pre>';
            }
            throw e;
        }
    }

    /**
     * Options
     */

    marked.options =
        marked.setOptions = function (opt) {
            merge(marked.defaults, opt);
            return marked;
        };

    marked.defaults = {
        gfm: true,
        tables: true,
        breaks: false,
        pedantic: false,
        sanitize: false,
        smartLists: false,
        silent: false,
        highlight: null,
        langPrefix: 'lang-'
    };

    /**
     * Expose
     */

    marked.Parser = Parser;
    marked.parser = Parser.parse;

    marked.Lexer = Lexer;
    marked.lexer = Lexer.lex;

    marked.InlineLexer = InlineLexer;
    marked.inlineLexer = InlineLexer.output;

    marked.parse = marked;

    if (typeof exports === 'object') {
        module.exports = marked;
    } else if (typeof define === 'function' && define.amd) {
        define(function () { return marked; });
    } else {
        this.marked = marked;
    }

}).call(function () {
    return this || (typeof window !== 'undefined' ? window : global);
}());

(function ($) {
    'use strict';

    // hide the whole page so we dont see the DOM flickering
    // will be shown upon page load complete or error
    $('html').addClass('md-hidden-load');

    // register our $.md object
    $.md = function (method) {
        if ($.md.publicMethods[method]) {
            return $.md.publicMethods[method].apply(this,
                Array.prototype.slice.call(arguments, 1)
            );
        } else {
            $.error('Method ' + method + ' does not exist on jquery.md');
        }
    };
    // default config
    $.md.config = {
        title: null,
        useSideMenu: true,
        lineBreaks: 'gfm',
        additionalFooterText: '',
        anchorCharacter: '&para;',
        tocAnchor: '[ &uarr; ]'
    };

    if (!$.mdContentRoot) {
        $.mdContentRoot = '';
    }

    $.md.gimmicks = [];
    $.md.stages = [];

    // the location of the main markdown file we display
    $.md.mainHref = '';

    // the in-page anchor that is specified after the !
    $.md.inPageAnchor = '';


    $.md.loglevel = {
        TRACE: 10,
        DEBUG: 20,
        INFO: 30,
        WARN: 40,
        ERROR: 50,
        FATAL: 60
    };
    // $.md.logThreshold = $.md.loglevel.DEBUG;
    $.md.logThreshold = $.md.loglevel.WARN;

}(jQuery));

(function ($) {
    'use strict';
    $.md.getLogger = function () {

        var loglevel = $.md.loglevel;

        var log = function (logtarget) {
            var self = this;
            var level = loglevel[logtarget];
            return function (msg) {
                if ($.md.logThreshold <= level) {
                    console.log('[' + logtarget + '] ' + msg);
                }
            };
        };

        var logger = {};
        logger.trace = log('TRACE');
        logger.debug = log('DEBUG');
        logger.info = log('INFO');
        logger.warn = log('WARN');
        logger.error = log('ERROR');
        logger.fatal = log('FATAL');

        return logger;
    };
}(jQuery));

(function ($) {
    'use strict';
    var log = $.md.getLogger();

    $.Stage = function (name) {
        var self = $.extend($.Deferred(), {});
        self.name = name;
        self.events = [];
        self.started = false;

        self.reset = function () {
            self.complete = $.Deferred();
            self.outstanding = [];
        };

        self.reset();

        self.subscribe = function (fn) {
            if (self.started) {
                $.error('Subscribing to stage which already started!');
            }
            self.events.push(fn);
        };
        self.unsubscribe = function (fn) {
            self.events.remove(fn);
        };

        self.executeSubscribedFn = function (fn) {
            var d = $.Deferred();
            self.outstanding.push(d);

            // display an error if our done() callback is not called
            $.md.util.wait(2500).done(function () {
                if (d.state() !== 'resolved') {
                    log.fatal('Timeout reached for done callback in stage: ' + self.name +
                        '. Did you forget a done() call in a .subscribe() ?');
                    log.fatal('stage ' + name + ' failed running subscribed function: ' + fn);
                }
            });

            var done = function () {
                d.resolve();
            };
            fn(done);
        };

        self.run = function () {
            self.started = true;
            $(self.events).each(function (i, fn) {
                self.executeSubscribedFn(fn);
            });

            // if no events are in our queue, we resolve immediately
            if (self.outstanding.length === 0) {
                self.resolve();
            }

            // we resolve when all our registered events have completed
            $.when.apply($, self.outstanding)
                .done(function () {
                    self.resolve();
                })
                .fail(function () {
                    self.resolve();
                });
        };

        self.done(function () {
            log.debug('stage ' + self.name + ' completed successfully.');
        });
        self.fail(function () {
            log.debug('stage ' + self.name + ' completed with errors!');
        });
        return self;
    };
}(jQuery));

(function ($) {
    'use strict';

    var log = $.md.getLogger();

    function init() {
        $.md.stages = [
            $.Stage('init'),

            // loads config, initial markdown and navigation
            $.Stage('load'),

            // will transform the markdown to html
            $.Stage('transform'),

            // HTML transformation finished
            $.Stage('ready'),

            // after we have a polished html skeleton
            $.Stage('skel_ready'),

            // will bootstrapify the skeleton
            $.Stage('bootstrap'),

            // before we run any gimmicks
            $.Stage('pregimmick'),

            // after we have bootstrapified the skeleton
            $.Stage('gimmick'),

            // postprocess
            $.Stage('postgimmick'),

            $.Stage('all_ready'),

            // used for integration tests, not intended to use in MDwiki itself
            $.Stage('final_tests')
        ];

        $.md.stage = function (name) {
            var m = $.grep($.md.stages, function (e, i) {
                return e.name === name;
            });
            if (m.length === 0) {
                $.error('A stage by name ' + name + '  does not exist');
            } else {
                return m[0];
            }
        };
    }
    init();

    function resetStages() {
        var old_stages = $.md.stages;
        $.md.stages = [];
        $(old_stages).each(function (i, e) {
            $.md.stages.push($.Stage(e.name));
        });
    }

    var publicMethods = {};
    $.md.publicMethods = $.extend({}, $.md.publicMethods, publicMethods);

    function transformMarkdown(markdown) {
        var options = {
            gfm: true,
            tables: true,
            breaks: true
        };
        if ($.md.config.lineBreaks === 'original')
            options.breaks = false;
        else if ($.md.config.lineBreaks === 'gfm')
            options.breaks = true;

        marked.setOptions(options);

        // get sample markdown
        var uglyHtml = marked(markdown);
        return uglyHtml;
    }

    function registerFetchMarkdown() {

        var md = '';

        $.md.stage('init').subscribe(function (done) {
            var ajaxReq = {
                url: $.mdContentRoot + $.md.mainHref,
                dataType: 'text'
            };
            $.ajax(ajaxReq).done(function (data) {
                // TODO do this elsewhere
                md = data;
                done();
            }).fail(function () {
                var log = $.md.getLogger();
                log.fatal('Could not get ' + $.md.mainHref);
                done();
            });
        });

        // find baseUrl
        $.md.stage('transform').subscribe(function (done) {
            var len = $.md.mainHref.lastIndexOf('/');
            var baseUrl = $.md.mainHref.substring(0, len + 1);
            $.md.baseUrl = baseUrl;
            done();
        });

        $.md.stage('transform').subscribe(function (done) {
            var uglyHtml = transformMarkdown(md);
            $('#md-content').html(uglyHtml);
            md = '';
            var dfd = $.Deferred();
            loadExternalIncludes(dfd);
            dfd.always(function () {
                done();
            });
        });
    }

    // load [include](/foo/bar.md) external links
    function loadExternalIncludes(parent_dfd) {

        function findExternalIncludes() {
            return $('a').filter(function () {
                var href = $(this).attr('href');
                var text = $(this).toptext();
                var isMarkdown = $.md.util.hasMarkdownFileExtension(href);
                var isInclude = text === 'include';
                var isPreview = text.startsWith('preview:');
                return (isInclude || isPreview) && isMarkdown;
            });
        }

        function selectPreviewElements($jqcol, num_elements) {
            function isTextNode(node) {
                return node.nodeType === 3;
            }
            var count = 0;
            var elements = [];
            $jqcol.each(function (i, e) {
                if (count < num_elements) {
                    elements.push(e);
                    if (!isTextNode(e)) count++;
                }
            });
            return $(elements);
        }

        var external_links = findExternalIncludes();
        // continue execution when all external resources are fully loaded
        var latch = $.md.util.countDownLatch(external_links.length);
        latch.always(function () {
            parent_dfd.resolve();
        });

        external_links.each(function (i, e) {
            var $el = $(e);
            var href = $el.attr('href');
            var text = $el.toptext();

            $.ajax({
                url: $.mdContentRoot + href,
                dataType: 'text'
            })
                .done(function (data) {
                    var $html = $(transformMarkdown(data));
                    if (text.startsWith('preview:')) {
                        // only insert the selected number of paragraphs; default 3
                        var num_preview_elements = parseInt(text.substring(8), 10) || 3;
                        var $preview = selectPreviewElements($html, num_preview_elements);
                        $preview.last().append('<a href="' + href + '"> ...read more &#10140;</a>');
                        $preview.insertBefore($el.parent('p').eq(0));
                        $el.remove();
                    } else {
                        $html.insertAfter($el.parents('p'));
                        $el.remove();
                    }
                }).always(function () {
                    latch.countDown();
                });
        });
    }

    function isSpecialLink(href) {
        if (!href) return false;

        if (href.lastIndexOf('data:') >= 0)
            return true;

        if (href.startsWith('mailto:'))
            return true;

        if (href.startsWith('file:'))
            return true;

        if (href.startsWith('ftp:'))
            return true;

        // TODO capture more special links: every non-http link with : like
        // torrent:// etc.
    }

    // modify internal links so we load them through our engine
    function processPageLinks(domElement, baseUrl) {
        var html = $(domElement);
        if (baseUrl === undefined) {
            baseUrl = '';
        }
        // HACK against marked: empty links will have empy href attribute
        // we remove the href attribute from the a tag
        html.find('a').not('#md-menu a').filter(function () {
            var $this = $(this);
            var attr = $this.attr('href');
            if (!attr || attr.length === 0)
                $this.removeAttr('href');
        });

        html.find('a, img').each(function (i, e) {
            var link = $(e);
            // link must be jquery collection
            var isImage = false;
            var hrefAttribute = 'href';

            if (!link.attr(hrefAttribute)) {
                isImage = true;
                hrefAttribute = 'src';
            }
            var href = link.attr(hrefAttribute);

            if (href && href.lastIndexOf('#!') >= 0)
                return;

            if (isSpecialLink(href))
                return;

            if (!isImage && href.startsWith('#') && !href.startsWith('#!')) {
                // in-page link
                link.click(function (ev) {
                    ev.preventDefault();
                    $.md.scrollToInPageAnchor(href);
                });
            }

            if (!$.md.util.isRelativeUrl(href))
                return;

            if (isImage && !$.md.util.isRelativePath(href))
                return;

            if (!isImage && $.md.util.isGimmickLink(link))
                return;

            function build_link(url) {
                if ($.md.util.hasMarkdownFileExtension(url))
                    return '#!' + url;
                else
                    return url;
            }

            var newHref = baseUrl + href;
            if (isImage)
                link.attr(hrefAttribute, newHref);
            else if ($.md.util.isRelativePath(href))
                link.attr(hrefAttribute, build_link(newHref));
            else
                link.attr(hrefAttribute, build_link(href));
        });
    }

    var navMD = '';
    $.md.NavigationDfd = $.Deferred();
    var ajaxReq = {
        url: $.mdContentRoot + 'navigation.md',
        dataType: 'text'
    };
    $.ajax(ajaxReq).done(function (data) {
        navMD = data;
        $.md.NavigationDfd.resolve();
    }).fail(function () {
        $.md.NavigationDfd.reject();
    });

    function registerBuildNavigation() {

        $.md.stage('init').subscribe(function (done) {
            $.md.NavigationDfd.done(function () {
                done();
            })
                .fail(function () {
                    done();
                });
        });

        $.md.stage('transform').subscribe(function (done) {
            if (navMD === '') {
                var log = $.md.getLogger();
                log.info('no navgiation.md found, not using a navbar');
                done();
                return;
            }

            var navHtml = marked(navMD);
            // TODO why are <script> tags from navHtml APPENDED to the jqcol?
            var $h = $('<div>' + navHtml + '</div>');

            // insert <scripts> from navigation.md into the DOM
            $h.each(function (i, e) {
                if (e.tagName === 'SCRIPT') {
                    $('script').first().before(e);
                }
            });

            // TODO .html() is evil!!!
            var $navContent = $h.eq(0);
            $navContent.find('p').each(function (i, e) {
                var $el = $(e);
                $el.replaceWith($el.html());
            });
            $('#md-menu').append($navContent.html());
            done();
        });

        $.md.stage('bootstrap').subscribe(function (done) {
            processPageLinks($('#md-menu'));
            done();
        });

        $.md.stage('postgimmick').subscribe(function (done) {
            var num_links = $('#md-menu a').length;
            var has_header = $('#md-menu .navbar-brand').eq(0).toptext().trim().length > 0;
            if (!has_header && num_links <= 1)
                $('#md-menu').hide();

            done();
        });
    }

    $.md.ConfigDfd = $.Deferred();
    $.ajax({ url: $.mdContentRoot + 'config.json', dataType: 'text' }).done(function (data) {
        try {
            var data_json = JSON.parse(data);
            $.md.config = $.extend($.md.config, data_json);
            log.info('Found a valid config.json file, using configuration');
        } catch (err) {
            log.error('config.json was not JSON parsable: ' + err);
        }
        $.md.ConfigDfd.resolve();
    }).fail(function (err, textStatus) {
        log.error('unable to retrieve config.json: ' + textStatus);
        $.md.ConfigDfd.reject();
    });
    function registerFetchConfig() {

        $.md.stage('init').subscribe(function (done) {
            // TODO 404 won't get cached, requesting it every reload is not good
            // maybe use cookies? or disable re-loading of the page
            //$.ajax('config.json').done(function(data){
            $.md.ConfigDfd.done(function () {
                done();
            }).fail(function () {
                var log = $.md.getLogger();
                log.info('No config.json found, using default settings');
                done();
            });
        });
    }

    function registerClearContent() {

        $.md.stage('init').subscribe(function (done) {
            $('#md-all').empty();
            var skel = '<div id="md-body"><div id="md-title"></div><div id="md-menu">' +
                '</div><div id="md-content"></div></div>';
            $('#md-all').prepend($(skel));
            done();
        });

    }
    function loadContent(href) {
        $.md.mainHref = href;

        registerFetchMarkdown();
        registerClearContent();

        // find out which link gimmicks we need
        $.md.stage('ready').subscribe(function (done) {
            $.md.initializeGimmicks();
            $.md.registerLinkGimmicks();
            done();
        });

        // wire up the load method of the modules
        $.each($.md.gimmicks, function (i, module) {
            if (module.load === undefined) {
                return;
            }
            $.md.stage('load').subscribe(function (done) {
                module.load();
                done();
            });
        });

        $.md.stage('ready').subscribe(function (done) {
            $.md('createBasicSkeleton');
            done();
        });

        $.md.stage('bootstrap').subscribe(function (done) {
            $.mdbootstrap('bootstrapify');
            processPageLinks($('#md-content'), $.md.baseUrl);
            done();
        });
        runStages();
    }

    function runStages() {

        // wire the stages up
        $.md.stage('init').done(function () {
            $.md.stage('load').run();
        });
        $.md.stage('load').done(function () {
            $.md.stage('transform').run();
        });
        $.md.stage('transform').done(function () {
            $.md.stage('ready').run();
        });
        $.md.stage('ready').done(function () {
            $.md.stage('skel_ready').run();
        });
        $.md.stage('skel_ready').done(function () {
            $.md.stage('bootstrap').run();
        });
        $.md.stage('bootstrap').done(function () {
            $.md.stage('pregimmick').run();
        });
        $.md.stage('pregimmick').done(function () {
            $.md.stage('gimmick').run();
        });
        $.md.stage('gimmick').done(function () {
            $.md.stage('postgimmick').run();
        });
        $.md.stage('postgimmick').done(function () {
            $.md.stage('all_ready').run();
        });
        $.md.stage('all_ready').done(function () {
            $('html').removeClass('md-hidden-load');

            // phantomjs hook when we are done
            if (typeof window.callPhantom === 'function') {
                window.callPhantom({});
            }

            $.md.stage('final_tests').run();
        });
        $.md.stage('final_tests').done(function () {
            // reset the stages for next iteration
            resetStages();

            // required by dalekjs so we can wait the element to appear
            $('body').append('<span id="start-tests"></span>');
            $('#start-tests').hide();
        });

        // trigger the whole process by runing the init stage
        $.md.stage('init').run();
        return;
    }

    function extractHashData() {
        // first char is the # or #!
        var href;
        if (window.location.hash.startsWith('#!')) {
            href = window.location.hash.substring(2);
        } else {
            href = window.location.hash.substring(1);
        }
        href = decodeURIComponent(href);

        // extract possible in-page anchor
        var ex_pos = href.indexOf('#');
        if (ex_pos !== -1) {
            $.md.inPageAnchor = href.substring(ex_pos + 1);
            $.md.mainHref = href.substring(0, ex_pos);
        } else {
            $.md.mainHref = href;
        }
    }

    function appendDefaultFilenameToHash() {
        var newHashString = '';
        var currentHashString = window.location.hash || '';
        if (currentHashString === '' ||
            currentHashString === '#' ||
            currentHashString === '#!') {
            newHashString = '#!index.md';
        }
        else if (currentHashString.startsWith('#!') &&
            currentHashString.endsWith('/')
        ) {
            newHashString = currentHashString + 'index.md';
        }
        if (newHashString)
            window.location.hash = newHashString;
    }

    $(document).ready(function () {

        // stage init stuff
        registerFetchConfig();
        registerBuildNavigation();
        extractHashData();

        appendDefaultFilenameToHash();

        $(window).bind('hashchange', function () {
            window.location.reload(false);
        });

        loadContent($.md.mainHref);
    });
}(jQuery));

(function ($) {
    var publicMethods = {
        isRelativeUrl: function (url) {
            if (url === undefined) {
                return false;
            }
            // if there is :// in it, its considered absolute
            // else its relative
            if (url.indexOf('://') === -1) {
                return true;
            } else {
                return false;
            }
        },
        isRelativePath: function (path) {
            if (path === undefined)
                return false;
            if (path.startsWith('/'))
                return false;
            return true;
        },
        isGimmickLink: function (domAnchor) {
            if (domAnchor.toptext().indexOf('gimmick:') !== -1) {
                return true;
            } else {
                return false;
            }
        },
        hasMarkdownFileExtension: function (str) {
            var markdownExtensions = ['.md', '.markdown', '.mdown'];
            var result = false;
            var value = str.toLowerCase().split('#')[0];
            $(markdownExtensions).each(function (i, ext) {
                if (value.toLowerCase().endsWith(ext)) {
                    result = true;
                }
            });
            return result;
        },
        wait: function (time) {
            return $.Deferred(function (dfd) {
                setTimeout(dfd.resolve, time);
            });
        }
    };
    $.md.util = $.extend({}, $.md.util, publicMethods);

    if (typeof String.prototype.startsWith !== 'function') {
        String.prototype.startsWith = function (str) {
            return this.slice(0, str.length) === str;
        };
    }
    if (typeof String.prototype.endsWith !== 'function') {
        String.prototype.endsWith = function (str) {
            return this.slice(this.length - str.length, this.length) === str;
        };
    }

    $.fn.extend({
        toptext: function () {
            return this.clone().children().remove().end().text();
        }
    });

    // adds a :icontains selector to jQuery that is case insensitive
    $.expr[':'].icontains = $.expr.createPseudo(function (arg) {
        return function (elem) {
            return $(elem).toptext().toUpperCase().indexOf(arg.toUpperCase()) >= 0;
        };
    });

    $.md.util.getInpageAnchorText = function (text) {
        var subhash = text.replace(/ /g, '_');
        // TODO remove more unwanted characters like ?/,- etc.
        return subhash;

    };
    $.md.util.getInpageAnchorHref = function (text, href) {
        href = href || $.md.mainHref;
        var subhash = $.md.util.getInpageAnchorText(text);
        return '#!' + href + '#' + subhash;
    };

    $.md.util.repeatUntil = function (interval, predicate, maxRepeats) {
        maxRepeats = maxRepeats || 10;
        var dfd = $.Deferred();
        function recursive_repeat(interval, predicate, maxRepeats) {
            if (maxRepeats === 0) {
                dfd.reject();
                return;
            }
            if (predicate()) {
                dfd.resolve();
                return;
            } else {
                $.md.util.wait(interval).always(function () {
                    recursive_repeat(interval, predicate, maxRepeats - 1);
                });
            }
        }
        recursive_repeat(interval, predicate, maxRepeats);
        return dfd;
    };

    // a count-down latch as in Java7.
    $.md.util.countDownLatch = function (capacity, min) {
        min = min || 0;
        var dfd = $.Deferred();
        if (capacity <= min) dfd.resolve();
        dfd.capacity = capacity;
        dfd.countDown = function () {
            dfd.capacity--;
            if (dfd.capacity <= min)
                dfd.resolve();
        };
        return dfd;
    };

}(jQuery));

(function ($) {
    'use strict';

    // PUBLIC API
    $.md.registerGimmick = function (module) {
        $.md.gimmicks.push(module);
        return;
    };

    // registers a script for a gimmick, that is later dynamically loaded
    // by the core.
    // src may be an URL or direct javascript sourcecode. When options.callback
    // is provided, the done() function is passed to the function and needs to
    // be called.
    $.md.registerScript = function (module, src, options) {
        var scriptinfo = new ScriptInfo({
            module: module,
            src: src,
            options: options
        });
        registeredScripts.push(scriptinfo);
    };

    // same as registerScript but for css. Note that we do not provide a
    // callback when the load finishes
    $.md.registerCss = function (module, url, options) {
        var license = options.license,
            stage = options.stage || 'skel_ready',
            callback = options.callback;

        checkLicense(license, module);
        var tag = '<link rel="stylesheet" href="' + url + '" type="text/css"></link>';
        $.md.stage(stage).subscribe(function (done) {
            $('head').append(tag);
            if (callback !== undefined) {
                callback(done);
            } else {
                done();
            }
        });
    };

    // turns hostname/path links into http://hostname/path links
    // we need to do this because if accessed by file:///, we need a different
    // transport scheme for external resources (like http://)
    $.md.prepareLink = function (link, options) {
        options = options || {};
        var ownProtocol = window.location.protocol;

        if (options.forceSSL)
            return 'https://' + link;
        if (options.forceHTTP)
            return 'http://' + link;

        if (ownProtocol === 'file:') {
            return 'http://' + link;
        }
        // default: use the same as origin resource
        return '//' + link;
    };

    // associate a link trigger for a gimmick. i.e. [gimmick:foo]() then
    // foo is the trigger and will invoke the corresponding gimmick
    $.md.linkGimmick = function (module, trigger, callback, stage) {
        if (stage === undefined) {
            stage = 'gimmick';
        }
        var linktrigger = new LinkTrigger({
            trigger: trigger,
            module: module,
            stage: stage,
            callback: callback
        });
        linkTriggers.push(linktrigger);
    };

    $.md.triggerIsActive = function (trigger) {
        if (activeLinkTriggers.indexOf(trigger) === -1) {
            return false;
        } else {
            return true;
        }
    };

    var initialized = false;
    // TODO combine main.js and modules.js closure
    $.md.initializeGimmicks = function () {
        findActiveLinkTrigger();
        runGimmicksOnce();
        loadRequiredScripts();
    };

    // END PUBLIC API


    var log = $.md.getLogger();

    // triggers that we actually found on the page
    // array of string
    var activeLinkTriggers = [];


    // array of ScriptInfo
    var registeredScripts = [];
    function ScriptInfo(initial) {
        this.module = undefined;
        this.options = {};

        // can ba an URL or javascript sourcecode
        this.src = '';

        $.extend(this, initial);
    }

    // array of linkTriggers
    var linkTriggers = [];
    function LinkTrigger(initial) {
        this.trigger = undefined;
        this.module = undefined;
        this.callback = undefined;

        $.extend(this, initial);
    }

    // jQuery does some magic when inserting inline scripts, so better
    // use vanilla JS. See:
    // http://stackoverflow.com/questions/610995/jquery-cant-append-script-element
    function insertInlineScript(src) {
        // scripts always need to go directly into the DOM
        var script = document.createElement('script');
        script.type = 'text/javascript';
        script.text = src;
        document.body.appendChild(script);
    }

    // since we are GPL, we have to be cautious what other scripts we load
    // as delivering to the browser is considered delivering a derived work
    var licenses = ['MIT', 'BSD', 'GPL', 'GPL2', 'GPL3', 'LGPL', 'LGPL2',
        'APACHE2', 'PUBLICDOMAIN', 'EXCEPTION', 'OTHER'
    ];
    function checkLicense(license, module) {
        if ($.inArray(license, licenses) === -1) {
            var availLicenses = JSON.stringify(licenses);
            log.warn('license ' + license + ' is not known.');
            log.warn('Known licenses:' + availLicenses);

        } else if (license === 'OTHER') {
            log.warn('WARNING: Module ' + module.name + ' uses a script' +
                ' with unknown license. This may be a GPL license violation if' +
                ' this website is publically available!');
        }
    }

    // will actually schedule the script load into the DOM.
    function loadScript(scriptinfo) {

        var module = scriptinfo.module,
            src = scriptinfo.src,
            options = scriptinfo.options;

        var license = options.license || 'OTHER',
            loadstage = options.loadstage || 'skel_ready',
            finishstage = options.finishstage || 'pregimmick',
            callback = options.callback;

        var loadDone = $.Deferred();

        checkLicense(license, module);
        // start script loading
        log.debug('subscribing ' + module.name + ' to start: ' + loadstage + ' end in: ' + finishstage);
        $.md.stage(loadstage).subscribe(function (done) {
            if (src.startsWith('//') || src.startsWith('http')) {
                $.getScript(src, function () {
                    if (callback !== undefined) {
                        callback(done);
                    } else {
                        log.debug('module' + module.name + ' script load done: ' + src);
                        done();
                    }
                    loadDone.resolve();
                });
            } else {
                // inline script that we directly insert
                insertInlineScript(src);
                log.debug('module' + module.name + ' script inject done');
                loadDone.resolve();
                done();
            }
        });
        // if loading is not yet finished in stage finishstage, wait
        // for the loading to complete
        $.md.stage(finishstage).subscribe(function (done) {
            loadDone.done(function () {
                done();
            });
        });
    }

    // finds out that kind of trigger words are acutally used on a given page
    // this is most likely a very small subset of all available gimmicks
    function findActiveLinkTrigger() {
        var $gimmicks = $('a:icontains(gimmick:)');
        $gimmicks.each(function (i, e) {
            var parts = getGimmickLinkParts($(e));
            if (activeLinkTriggers.indexOf(parts.trigger) === -1) {
                activeLinkTriggers.push(parts.trigger);
            }
        });
        log.debug('Scanning for required gimmick links: ' + JSON.stringify(activeLinkTriggers));
    }

    function loadRequiredScripts() {
        // find each module responsible for the link trigger
        $.each(activeLinkTriggers, function (i, trigger) {
            var module = findModuleByTrigger(trigger);
            if (module === undefined) {
                log.error('Gimmick link: "' + trigger + '" found but no suitable gimmick loaded');
                return;
            }
            var scriptinfo = registeredScripts.filter(function (info) {
                return info.module.name === module.name;
            })[0];
            // register to load the script
            if (scriptinfo !== undefined) {
                loadScript(scriptinfo);
            }
        });
    }

    function findModuleByTrigger(trigger) {
        var ret;
        $.each(linkTriggers, function (i, e) {
            if (e.trigger === trigger) {
                ret = e.module;
            }
        });
        return ret;
    }

    function getGimmickLinkParts($link) {
        var link_text = $.trim($link.toptext());
        // returns linkTrigger, options, linkText
        if (link_text.match(/gimmick:/i) === null) {
            return null;
        }
        var href = $.trim($link.attr('href'));
        var r = new RegExp(/gimmick:\s*([^(\s]*)\s*(\(\s*{?(.*)\s*}?\s*\))*/i);
        var matches = r.exec(link_text);
        if (matches === null || matches[1] === undefined) {
            $.error('Error matching a gimmick: ' + link_text);
            return null;
        }
        var trigger = matches[1].toLowerCase();
        var args = null;
        // getting the parameters
        if (matches[2] !== undefined) {
            // remove whitespaces
            var params = $.trim(matches[3].toString());
            // remove the closing } if present
            if (params.charAt(params.length - 1) === '}') {
                params = params.substring(0, params.length - 1);
            }
            // add surrounding braces and paranthese
            params = '({' + params + '})';
            // replace any single quotes by double quotes
            params = params.replace(/'/g, '"');
            // finally, try if the json object is valid
            try {
                /*jshint -W061 */
                args = eval(params);
            } catch (err) {
                log.error('error parsing argument of gimmick: ' + link_text + 'giving error: ' + err);
            }
        }
        return { trigger: trigger, options: args, href: href };
    }

    function runGimmicksOnce() {
        // runs the once: callback for each gimmick within the init stage
        $.each($.md.gimmicks, function (i, module) {
            if (module.once === undefined) {
                return;
            }
            module.once();
        });
    }

    // activate all gimmicks on a page, that are contain the text gimmick:
    // TODO make private / merge closures
    $.md.registerLinkGimmicks = function () {
        var $gimmick_links = $('a:icontains(gimmick:)');
        $gimmick_links.each(function (i, e) {
            var $link = $(e);
            var gimmick_arguments = getGimmickLinkParts($link);

            $.each(linkTriggers, function (i, linktrigger) {
                if (gimmick_arguments.trigger === linktrigger.trigger) {
                    subscribeLinkTrigger($link, gimmick_arguments, linktrigger);
                }
            });
        });
    };

    function subscribeLinkTrigger($link, args, linktrigger) {
        log.debug('Subscribing gimmick ' + linktrigger.module.name + ' to stage: ' + linktrigger.stage);

        $.md.stage(linktrigger.stage).subscribe(function (done) {
            args.options = args.options || {};

            // it is possible that broken modules or any other transformation removed the $link
            // from the dom in the meantime
            if (!jQuery.contains(document.documentElement, $link[0])) {
                log.error('LINK IS NOT IN THE DOM ANYMORE: ');
                console.log($link);
            }

            log.debug('Running gimmick ' + linktrigger.module.name);
            linktrigger.callback($link, args.options, args.href, done);

            // if the gimmick didn't call done, we trigger it here
            done();
        });
    }
}(jQuery));

(function ($) {
    var publicMethods = {
        createBasicSkeleton: function () {

            setPageTitle();
            wrapParagraphText();
            linkImagesToSelf();
            groupImages();
            removeBreaks();
            addInpageAnchors();

            $.md.stage('all_ready').subscribe(function (done) {
                if ($.md.inPageAnchor !== '') {
                    $.md.util.wait(500).then(function () {
                        $.md.scrollToInPageAnchor($.md.inPageAnchor);
                    });
                }
                done();
            });
            return;

        }
    };
    $.md.publicMethods = $.extend({}, $.md.publicMethods, publicMethods);

    // set the page title to the browser document title, optionally picking
    // the first h1 element as title if no title is given
    function setPageTitle() {
        var $pageTitle;
        if ($.md.config.title)
            $('title').text($.md.config.title);

        $pageTitle = $('#md-content h1').eq(0);
        if ($.trim($pageTitle.toptext()).length > 0) {
            $('#md-title').prepend($pageTitle);
            var title = $pageTitle.toptext();
            // document.title = title;
        } else {
            $('#md-title').remove();
        }
    }
    function wrapParagraphText() {
        // TODO is this true for marked.js?

        // markdown gives us sometime paragraph that contain child tags (like img),
        // but the containing text is not wrapped. Make sure to wrap the text in the
        // paragraph into a <div>

        // this also moves ANY child tags to the front of the paragraph!
        $('#md-content p').each(function () {
            var $p = $(this);
            // nothing to do for paragraphs without text
            if ($.trim($p.text()).length === 0) {
                // make sure no whitespace are in the p and then exit
                //$p.text ('');
                return;
            }
            // children elements of the p
            var children = $p.contents().filter(function () {
                var $child = $(this);
                // we extract images and hyperlinks with images out of the paragraph
                if (this.tagName === 'A' && $child.find('img').length > 0) {
                    return true;
                }
                if (this.tagName === 'IMG') {
                    return true;
                }
                // else
                return false;
            });
            var floatClass = getFloatClass($p);
            $p.wrapInner('<div class="md-text" />');

            // if there are no children, we are done
            if (children.length === 0) {
                return;
            }
            // move the children out of the wrapped div into the original p
            children.prependTo($p);

            // at this point, we now have a paragraph that holds text AND images
            // we mark that paragraph to be a floating environment
            // TODO determine floatenv left/right
            $p.addClass('md-floatenv').addClass(floatClass);
        });
    }
    function removeBreaks() {
        // since we use non-markdown-standard line wrapping, we get lots of
        // <br> elements we don't want.

        // remove a leading <br> from floatclasses, that happen to
        // get insertet after an image
        $('.md-floatenv').find('.md-text').each(function () {
            var $first = $(this).find('*').eq(0);
            if ($first.is('br')) {
                $first.remove();
            }
        });

        // remove any breaks from image groups
        $('.md-image-group').find('br').remove();
    }
    function getFloatClass(par) {
        var $p = $(par);
        var floatClass = '';

        // reduce content of the paragraph to images
        var nonTextContents = $p.contents().filter(function () {
            if (this.tagName === 'IMG' || this.tagName === 'IFRAME') {
                return true;
            }
            else if (this.tagName === 'A') {
                return $(this).find('img').length > 0;
            }
            else {
                return $.trim($(this).text()).length > 0;
            }
        });
        // check the first element - if its an image or a link with image, we go left
        var elem = nonTextContents[0];
        if (elem !== undefined && elem !== null) {
            if (elem.tagName === 'IMG' || elem.tagName === 'IFRAME') {
                floatClass = 'md-float-left';
            }
            else if (elem.tagName === 'A' && $(elem).find('img').length > 0) {
                floatClass = 'md-float-left';
            }
            else {
                floatClass = 'md-float-right';
            }
        }
        return floatClass;
    }
    // images are put in the same image group as long as there is
    // not separating paragraph between them
    function groupImages() {
        var par = $('p img').parents('p');
        // add an .md-image-group class to the p
        par.addClass('md-image-group');
    }

    // takes a standard <img> tag and adds a hyperlink to the image source
    // needed since we scale down images via css and want them to be accessible
    // in original format
    function linkImagesToSelf() {
        function selectNonLinkedImages() {
            // only select images that do not have a non-empty parent link
            $images = $('img').filter(function (index) {
                var $parent_link = $(this).parents('a').eq(0);
                if ($parent_link.length === 0) return true;
                var attr = $parent_link.attr('href');
                return (attr && attr.length === 0);
            });
            return $images;
        }
        var $images = selectNonLinkedImages();
        return $images.each(function () {
            var $this = $(this);
            var img_src = $this.attr('src');
            var img_title = $this.attr('title');
            if (img_title === undefined) {
                img_title = '';
            }
            // wrap the <img> tag in an anchor and copy the title of the image
            $this.wrap('<a class="md-image-selfref" href="' + img_src + '" title="' + img_title + '"/> ');
        });
    }

    function addInpageAnchors() {
        // adds a pilcrow (paragraph) character to heading with a link for the
        // inpage anchor
        function addPilcrow($heading, href) {
            var c = $.md.config.anchorCharacter;
            var $pilcrow = $('<span class="anchor-highlight"><a>' + c + '</a></span>');
            $pilcrow.find('a').attr('href', href);
            $pilcrow.hide();

            var mouse_entered = false;
            $heading.mouseenter(function () {
                mouse_entered = true;
                $.md.util.wait(300).then(function () {
                    if (!mouse_entered) return;
                    $pilcrow.fadeIn(200);
                });
            });
            $heading.mouseleave(function () {
                mouse_entered = false;
                $pilcrow.fadeOut(200);
            });
            $pilcrow.appendTo($heading);
        }

        // adds a link to the navigation at the top of the page
        function addJumpLinkToTOC($heading) {
            if ($.md.config.useSideMenu === false) return;
            if ($heading.prop("tagName") !== 'H2') return;

            var c = $.md.config.tocAnchor;
            if (c === '')
                return;

            var $jumpLink = $('<a class="visible-xs visible-sm jumplink" href="#md-page-menu">' + c + '</a>');
            $jumpLink.click(function (ev) {
                ev.preventDefault();

                $('body').scrollTop($('#md-page-menu').position().top);
            });

            if ($heading.parents('#md-menu').length === 0) {
                $jumpLink.insertAfter($heading);
            }
        }

        // adds a page inline anchor to each h1,h2,h3,h4,h5,h6 element
        // which can be accessed by the headings text
        $('h1,h2,h3,h4,h5,h6').not('#md-title h1').each(function () {
            var $heading = $(this);
            $heading.addClass('md-inpage-anchor');
            var text = $heading.clone().children('.anchor-highlight').remove().end().text();
            var href = $.md.util.getInpageAnchorHref(text);
            addPilcrow($heading, href);

            //add jumplink to table of contents
            //addJumpLinkToTOC($heading);
        });
    }

    $.md.scrollToInPageAnchor = function (anchortext) {
        if (anchortext.startsWith('#'))
            anchortext = anchortext.substring(1, anchortext.length);
        // we match case insensitive
        var doBreak = false;
        $('.md-inpage-anchor').each(function () {
            if (doBreak) { return; }
            var $this = $(this);
            // don't use the text of any subnode
            var text = $this.toptext();
            var match = $.md.util.getInpageAnchorText(text);
            if (anchortext === match) {
                this.scrollIntoView(true);
                var navbar_offset = $('.navbar-collapse').height() + 15;
                window.scrollBy(0, -navbar_offset + 5);
                doBreak = true;
            }
        });
    };

}(jQuery));

(function ($) {
    'use strict';
    // call the gimmick
    $.mdbootstrap = function (method) {
        if ($.mdbootstrap.publicMethods[method]) {
            return $.mdbootstrap.publicMethods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else {
            $.error('Method ' + method + ' does not exist on jquery.mdbootstrap');
        }
    };
    // simple wrapper around $().bind
    $.mdbootstrap.events = [];
    $.mdbootstrap.bind = function (ev, func) {
        $(document).bind(ev, func);
        $.mdbootstrap.events.push(ev);
    };
    $.mdbootstrap.trigger = function (ev) {
        $(document).trigger(ev);
    };

    var navStyle = '';

    // PUBLIC API functions that are exposed
    var publicMethods = {
        bootstrapify: function () {
            createPageSkeleton();
            buildMenu();
            changeHeading();
            replaceImageParagraphs();

            $('table').addClass('table').addClass('table-bordered');
            //pullRightBumper ();

            // remove the margin for headings h1 and h2 that are the first
            // on page
            //if (navStyle == "sub" || (navStyle == "top" && $('#md-title').text ().trim ().length === 0))
            //    $(".md-first-heading").css ("margin-top", "0");

            // external content should run after gimmicks were run
            $.md.stage('pregimmick').subscribe(function (done) {
                if ($.md.config.useSideMenu !== false) {
                    createPageContentMenu();
                }
                addFooter();
                addAdditionalFooterText();
                done();
            });
            $.md.stage('postgimmick').subscribe(function (done) {
                adjustExternalContent();
                highlightActiveLink();

                done();
            });
        }
    };
    // register the public API functions
    $.mdbootstrap.publicMethods = $.extend({}, $.mdbootstrap.publicMethods, publicMethods);

    // PRIVATE FUNCTIONS:

    function buildTopNav() {
        // replace with the navbar skeleton
        if ($('#md-menu').length <= 0) {
            return;
        }
        navStyle = 'top';
        var $menuContent = $('#md-menu').children();

        // $('#md-menu').addClass ('navbar navbar-default navbar-fixed-top');
        // var menusrc = '';
        // menusrc += '<div id="md-menu-inner" class="container">';
        // menusrc += '<ul id="md-menu-ul" class="nav navbar-nav">';
        // menusrc += '</ul></div>';

        var navbar = '';
        navbar += '<div id="md-main-navbar" class="navbar navbar-default navbar-fixed-top" role="navigation">';
        navbar += '<div class="navbar-header">';
        navbar += '<button type="button" class="navbar-toggle" data-toggle="collapse" data-target=".navbar-ex1-collapse">';
        navbar += '<span class="sr-only">Toggle navigation</span>';
        navbar += '<span class="icon-bar"></span>';
        navbar += '<span class="icon-bar"></span>';
        navbar += '<span class="icon-bar"></span>';
        navbar += '</button>';
        navbar += '<a class="navbar-brand" href="#"></a>';
        navbar += '</div>';

        navbar += '<div class="collapse navbar-collapse navbar-ex1-collapse">';
        navbar += '<ul class="nav navbar-nav" />';
        navbar += '<ul class="nav navbar-nav navbar-right" />';
        navbar += '</div>';
        navbar += '</div>';
        var $navbar = $(navbar);

        $navbar.appendTo('#md-menu');
        // .eq(0) becase we dont want navbar-right to be appended to
        $('#md-menu ul.nav').eq(0).append($menuContent);

        // the menu should be the first element in the body
        $('#md-menu').prependTo('#md-all');

        var brand_text = $('#md-menu h1').toptext();
        $('#md-menu h1').remove();
        $('a.navbar-brand').text(brand_text);

        // initial offset
        $('#md-body').css('margin-top', '70px');
        $.md.stage('pregimmick').subscribe(function (done) {
            check_offset_to_navbar();
            done();
        });
    }
    // the navbar has different height depending on theme, number of navbar entries,
    // and window/device width. Therefore recalculate on start and upon window resize
    function set_offset_to_navbar() {
        var height = $('#md-main-navbar').height() + 10;
        $('#md-body').css('margin-top', height + 'px');
    }
    function check_offset_to_navbar() {
        // HACK this is VERY UGLY. When an external theme is used, we don't know when the
        // css style will be finished loading - and we can only correctly calculate
        // the height AFTER it has completely loaded.
        var navbar_height = 0;

        var dfd1 = $.md.util.repeatUntil(40, function () {
            navbar_height = $('#md-main-navbar').height();
            return (navbar_height > 35) && (navbar_height < 481);
        }, 25);

        dfd1.done(function () {
            navbar_height = $('#md-main-navbar').height();
            set_offset_to_navbar();
            // now bootstrap changes this maybe after a while, again watch for changes
            var dfd2 = $.md.util.repeatUntil(20, function () {
                return navbar_height !== $('#md-main-navbar').height();
            }, 25);
            dfd2.done(function () {
                // it changed, so we need to change it again
                set_offset_to_navbar();
            });
            // and finally, for real slow computers, make sure it is changed if changin very late
            $.md.util.wait(2000).done(function () {
                set_offset_to_navbar();
            });
        });
    }
    function buildSubNav() {
        // replace with the navbar skeleton
        /* BROKEN CODE
        if ($('#md-menu').length <= 0) {
            return;
        }
        navStyle = 'sub';
        var $menuContent = $('#md-menu').html ();

        var menusrc = '';
        menusrc += '<div id="md-menu-inner" class="subnav">';
        menusrc += '<ul id="md-menu-ul" class="nav nav-pills">';
        menusrc += $menuContent;
        menusrc += '</ul></div>';
        $('#md-menu').empty();
        $('#md-menu').wrapInner($(menusrc));
        $('#md-menu').addClass ('col-md-12');

        $('#md-menu-container').insertAfter ($('#md-title-container'));
        */
    }

    function buildMenu() {
        if ($('#md-menu a').length === 0) {
            return;
        }
        var h = $('#md-menu');

        // make toplevel <a> a dropdown
        h.find('> a[href=""]')
            .attr('data-toggle', 'dropdown')
            .addClass('dropdown-toggle')
            .attr('href', '')
            .append('<b class="caret"/>');
        h.find('ul').addClass('dropdown-menu');
        h.find('ul li').addClass('dropdown');

        // replace hr with dividers
        $('#md-menu hr').each(function (i, e) {
            var hr = $(e);
            var prev = hr.prev();
            var next = hr.next();
            if (prev.is('ul') && prev.length >= 0) {
                prev.append($('<li class="divider"/>'));
                hr.remove();
                if (next.is('ul')) {
                    next.find('li').appendTo(prev);
                    next.remove();
                }
                // next ul should now be empty
            }
            return;
        });

        // remove empty uls
        $('#md-menu ul').each(function (i, e) {
            var ul = $(e);
            if (ul.find('li').length === 0) {
                ul.remove();
            }
        });

        $('#md-menu hr').replaceWith($('<li class="divider-vertical"/>'));


        // wrap the toplevel links in <li>
        $('#md-menu > a').wrap('<li />');
        $('#md-menu ul').each(function (i, e) {
            var ul = $(e);
            ul.appendTo(ul.prev());
            ul.parent('li').addClass('dropdown');
        });

        // submenu headers
        $('#md-menu li.dropdown').find('h1, h2, h3').each(function (i, e) {
            var $e = $(e);
            var text = $e.toptext();
            var header = $('<li class="dropdown-header" />');
            header.text(text);
            $e.replaceWith(header);
        });

        // call the user specifed menu function
        buildTopNav();
    }
    function isVisibleInViewport(e) {
        var el = $(e);
        var top = $(window).scrollTop();
        var bottom = top + $(window).height();

        var eltop = el.offset().top;
        var elbottom = eltop + el.height();

        return (elbottom <= bottom) && (eltop >= top);
    }

    function createPageContentMenu() {

        // assemble the menu
        var $headings = $('#md-content').find('h2').clone();
        // we dont want the text of any child nodes
        $headings.children().remove();

        if ($headings.length <= 1) {
            return;
        }

        $('#md-content').removeClass('col-md-12');
        $('#md-content').addClass('col-md-9');
        $('#md-content-row').prepend('<div class="col-md-3" id="md-left-column"/>');

        $(window).scroll(function () {
            var $first;
            $('*.md-inpage-anchor').each(function (i, e) {
                if ($first === undefined) {
                    var h = $(e);
                    if (isVisibleInViewport(h)) {
                        $first = h;
                    }
                }
            });
            // highlight in the right menu
            $('#md-page-menu a').each(function (i, e) {
                var $a = $(e);
                if ($first && $a.toptext() === $first.toptext()) {
                    $('#md-page-menu a.active').removeClass('active');
                    //$a.parent('a').addClass('active');
                    $a.addClass('active');
                }
            });
        });


        var affixDiv = $('<div id="md-page-menu" />');

        var top_spacing = 70;
        affixDiv.css('top', top_spacing);
        affixDiv.css('position', 'sticky');

        var $pannel = $('<div class="panel panel-default"><ul class="list-group"/></div>');
        var $ul = $pannel.find("ul");
        affixDiv.append($pannel);

        $headings.each(function (i, e) {
            var $heading = $(e);
            var $li = $('<li class="list-group-item" />');
            var $a = $('<a />');
            $a.attr('href', $.md.util.getInpageAnchorHref($heading.toptext()));
            $a.click(function (ev) {
                ev.preventDefault();

                var $this = $(this);
                var anchortext = $.md.util.getInpageAnchorText($this.toptext());
                $.md.scrollToInPageAnchor(anchortext);
            });
            $a.text($heading.toptext());
            $li.append($a);
            $ul.append($li);
        });

        $(window).resize(function () {
            check_offset_to_navbar();
        });
        $.md.stage('postgimmick').subscribe(function (done) {
            done();
        });

        //menu.css('width','100%');
        $('#md-left-column').append(affixDiv);

    }

    function createPageSkeleton() {

        $('#md-title').wrap('<div class="container" id="md-title-container"/>');
        $('#md-title').wrap('<div class="row" id="md-title-row"/>');

        $('#md-menu').wrap('<div class="container" id="md-menu-container"/>');
        $('#md-menu').wrap('<div class="row" id="md-menu-row"/>');

        $('#md-content').wrap('<div class="container" id="md-content-container"/>');
        $('#md-content').wrap('<div class="row" id="md-content-row"/>');

        $('#md-body').wrap('<div class="container" id="md-body-container"/>');
        $('#md-body').wrap('<div class="row" id="md-body-row"/>');

        $('#md-title').addClass('col-md-12');
        $('#md-content').addClass('col-md-12');

    }
    function pullRightBumper() {
        /*     $("span.bumper").each (function () {
                   $this = $(this);
                   $this.prev().addClass ("pull-right");
               });
               $('span.bumper').addClass ('pull-right');
       */
    }

    function changeHeading() {

        // HEADING
        var jumbo = $('<div class="page-header" />');
        $('#md-title').wrapInner(jumbo);
    }

    function highlightActiveLink() {
        // when no menu is used, return
        if ($('#md-menu').find('li').length === 0) {
            return;
        }
        var filename = window.location.hash;

        if (filename.length === 0) {
            filename = '#!index.md';
        }
        var selector = 'li:has(a[href="' + filename + '"])';
        $('#md-menu').find(selector).addClass('active');
    }

    // replace all <p> around images with a <div class="thumbnail" >
    function replaceImageParagraphs() {

        // only select those paragraphs that have images in them
        var $pars = $('p img').parents('p');
        $pars.each(function () {
            var $p = $(this);
            var $images = $(this).find('img')
                .filter(function () {
                    // only select those images that have no parent anchor
                    return $(this).parents('a').length === 0;
                })
                // add those anchors including images
                .add($(this).find('img'))
                .addClass('img-responsive')
                .addClass('img-thumbnail');

            // create a new url group at the fron of the paragraph
            //$p.prepend($('<ul class="thumbnails" />'));
            // move the images to the newly created ul
            //$p.find('ul').eq(0).append($images);

            // wrap each image with a <li> that limits their space
            // the number of images in a paragraphs determines thei width / span

            // if the image is a link, wrap around the link to avoid
            function wrapImage($imgages, wrapElement) {
                return $images.each(function (i, img) {
                    var $img = $(img);
                    var $parent_img = $img.parent('a');
                    if ($parent_img.length > 0)
                        $parent_img.wrap(wrapElement);
                    else
                        $img.wrap(wrapElement);
                });
            }

            if ($p.hasClass('md-floatenv')) {
                if ($images.length === 1) {
                    wrapImage($images, '<div class="col-sm-8" />');
                } else if ($images.length === 2) {
                    wrapImage($images, '<div class="col-sm-4" />');
                } else {
                    wrapImage($images, '<div class="col-sm-2" />');
                }
            } else {

                // non-float => images are on their own single paragraph, make em larger
                // but remember, our image resizing will make them only as large as they are
                // but do no upscaling
                // TODO replace by calculation

                if ($images.length === 1) {
                    wrapImage($images, '<div class="col-sm-12" />');
                } else if ($images.length === 2) {
                    wrapImage($images, '<div class="col-sm-6" />');
                } else if ($images.length === 3) {
                    wrapImage($images, '<div class="col-sm-4" />');
                } else if ($images.length === 4) {
                    wrapImage($images, '<div class="col-sm-3" />');
                } else {
                    wrapImage($images, '<div class="col-sm-2" />');
                }
            }
            $p.addClass('row');
            // finally, every img gets its own wrapping thumbnail div
            //$images.wrap('<div class="thumbnail" />');
        });

        // apply float to the ul thumbnails
        //$('.md-floatenv.md-float-left ul').addClass ('pull-left');
        //$('.md-floatenv.md-float-right ul').addClass ('pull-right');
    }

    function adjustExternalContent() {
        // external content are usually iframes or divs that are integrated
        // by gimmicks
        // example: youtube iframes, google maps div canvas
        // all external content are in the md-external class

        $('iframe.md-external').not('.md-external-nowidth')
            .attr('width', '450')
            .css('width', '450px');

        $('iframe.md-external').not('.md-external-noheight')
            .attr('height', '280')
            .css('height', '280px');

        // make it appear like an image thumbnal
        //$('.md-external').addClass('img-thumbnail');

        //.wrap($("<ul class='thumbnails' />")).wrap($("<li class='col-md-6' />"));
        $('div.md-external').not('.md-external-noheight')
            .css('height', '280px');
        $('div.md-external').not('.md-external-nowidth')
            .css('width', '450px');

        // // make it appear like an image thumbnal
        // $("div.md-external").addClass("thumbnail").wrap($("<ul class='thumbnails' />")).wrap($("<li class='col-md-10' />"));

        // $("div.md-external-large").css('width', "700px")
    }

    // note: the footer is part of the GPLv3 legal information
    // and may not be removed or hidden to comply with licensing conditions.
    function addFooter() {
        var navbar = '';
        navbar += '<hr><div class="scontainer">';
        navbar += '<div class="pull-right md-copyright-footer"> ';
        navbar += '<span id="md-footer-additional"></span>';
        navbar += 'Website generated with <a href="http://www.mdwiki.info">MDwiki</a> ';
        navbar += '&copy; Timo D&ouml;rr and contributors. ';
        navbar += '</div>';
        navbar += '</div>';
        var $navbar = $(navbar);
        $navbar.css('position', 'relative');
        $navbar.css('margin-top', '1em');
        $('#md-all').append($navbar);
    }

    function addAdditionalFooterText() {
        var text = $.md.config.additionalFooterText;
        if (text) {
            $('.md-copyright-footer #md-footer-additional').html(text);
        }
    }
}(jQuery));

(function ($) {
    $.gimmicks = $.fn.gimmicks = function (method) {
        if (method === undefined) {
            return;
        }
        // call the gimmick
        if ($.fn.gimmicks.methods[method]) {
            return $.fn.gimmicks.methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else {
            $.error('Gimmick ' + method + ' does not exist on jQuery.gimmicks');
        }
    };

    // TODO underscores _ in Markdown links are not allowed! bug in our MD imlemenation


}(jQuery));

(function ($) {
    //'use strict';
    var alertsGimmick = {
        name: 'alerts',
        // TODO
        //version: $.md.version,
        load: function () {
            $.md.stage('bootstrap').subscribe(function (done) {
                createAlerts();
                done();
            });
        }
    };
    $.md.registerGimmick(alertsGimmick);

    // takes a standard <img> tag and adds a hyperlink to the image source
    // needed since we scale down images via css and want them to be accessible
    // in original format
    function createAlerts() {
        var matches = $(select_paragraphs());
        matches.each(function () {
            var $p = $(this.p);
            var type = this.alertType;
            $p.addClass('alert');

            if (type === 'note') {
                $p.addClass('alert-info');
            } else if (type === 'hint') {
                $p.addClass('alert-success');
            } else if (type === 'warning') {
                $p.addClass('alert-warning');
            }
        });
    }

    // picks out the paragraphs that start with a trigger word
    function select_paragraphs() {
        var note = ['note', 'beachte'];
        var warning = ['achtung', 'attention', 'warnung', 'warning', 'atención', 'guarda', 'advertimiento'];
        var hint = ['hint', 'tipp', 'tip', 'hinweis'];
        var exp = note.concat(warning);
        exp = exp.concat(hint);
        var matches = [];

        $('p').filter(function () {
            var $par = $(this);
            // check against each expression
            $(exp).each(function (i, trigger) {
                var txt = $par.text().toLowerCase();
                // we match only paragrachps in which the 'trigger' expression
                // is follow by a ! or :
                var re = new RegExp(trigger + '(:|!)+.*', 'i');
                var alertType = 'none';
                if (txt.match(re) !== null) {
                    if ($.inArray(trigger, note) >= 0) {
                        alertType = 'note';
                    } else if ($.inArray(trigger, warning) >= 0) {
                        alertType = 'warning';
                    } else if ($.inArray(trigger, hint) >= 0) {
                        alertType = 'hint';
                    }
                    matches.push({
                        p: $par,
                        alertType: alertType
                    });
                }
            });
        });
        return matches;
    }
}(jQuery));

(function ($) {
    // makes trouble, find out why
    //'use strict';
    var colorboxGimmick = {
        name: 'colorbox',
        load: function () {
            $.md.stage('gimmick').subscribe(function (done) {
                $.gimmicks('colorbox');
                done();
            });
        }
    };
    $.md.registerGimmick(colorboxGimmick);

    var methods = {
        // takes a standard <img> tag and adds a hyperlink to the image source
        // needed since we scale down images via css and want them to be accessible
        // in original format
        colorbox: function () {
            var $image_groups;
            if (!(this instanceof jQuery)) {
                // select the image groups of the page
                $image_groups = $('.md-image-group');
            } else {
                $image_groups = $(this);
            }
            // operate on md-image-group, which holds one
            // or more images that are to be colorbox'ed
            var counter = 0;
            return $image_groups.each(function () {
                var $this = $(this);

                // each group requires a unique name
                var gal_group = 'gallery-group-' + (counter++);

                // create a hyperlink around the image
                $this.find('a.md-image-selfref img')
                    // filter out images that already are a hyperlink
                    // (so won't be part of the gallery)

                    // apply colorbox on their parent anchors
                    .parents('a').colorbox({
                        rel: gal_group,
                        opacity: 0.75,
                        slideshow: true,
                        maxWidth: '95%',
                        maxHeight: '95%',
                        scalePhotos: true,
                        photo: true,
                        slideshowAuto: false
                    });
            });
        }
    };
    $.gimmicks.methods = $.extend({}, $.fn.gimmicks.methods, methods);
}(jQuery));

(function ($) {
    'use strict';

    var themeChooserGimmick = {
        name: 'Themes',
        version: $.md.version,
        once: function () {
            $.md.linkGimmick(this, 'carousel', carousel);
        }
    };
    $.md.registerGimmick(themeChooserGimmick);

    function carousel($link, opt, href) {

        var $c = $('<div id="myCarousel" class="carousel slide"></div>');
        var $d = $('<div class="carousel-inner"/>');
        $c.append('<ol class="carousel-indicators" />');

        var imageUrls = [];
        var i = 0;
        $.each(href.split(','), function (i, e) {
            imageUrls.push($.trim(e));
            $c.find('ol').append('<li data-target="#myCarousel" data-slide-to="' + i + '" class="active" /');
            var div;
            if (i === 0) {
                div = ('<div class="active item"/>');
            } else {
                div = ('<div class="item"/>');
            }
            $d.append($(div).append('<img src="' + e + '"/>'));
        });
        $c.append($d);
        $c.append('<a class="carousel-control left" href="#myCarousel" data-slide="prev">&lsaquo;</a>');
        $c.append('<a class="carousel-control right" href="#myCarousel" data-slide="next">&rsaquo;</a>');
        $link.replaceWith($c);
    }
}(jQuery));

(function ($) {
    var disqusGimmick = {
        name: 'disqus',
        version: $.md.version,
        once: function () {
            $.md.linkGimmick(this, 'disqus', disqus);
        }
    };
    $.md.registerGimmick(disqusGimmick);

    var alreadyDone = false;
    var disqus = function ($links, opt, text) {
        var default_options = {
            identifier: ''
        };
        var options = $.extend(default_options, opt);
        var disqus_div = $('<div id="disqus_thread" class="md-external md-external-noheight md-external-nowidth" >' + '<a href="http://disqus.com" class="dsq-brlink">comments powered by <span class="logo-disqus">Disqus</span></a></div>');
        disqus_div.css('margin-top', '2em');
        return $links.each(function (i, link) {
            if (alreadyDone === true) {
                return;
            }
            alreadyDone = true;

            var $link = $(link);
            var disqus_shortname = $link.attr('href');

            if (disqus_shortname !== undefined && disqus_shortname.length > 0) {
                // insert the div
                $link.remove();
                // since disqus need lot of height, always but it on the bottom of the page
                $('#md-content').append(disqus_div);
                if ($('#disqus_thread').length > 0) {
                    (function () {
                        // all disqus_ variables are used by the script, they
                        // change the config behavious.
                        // see: http://help.disqus.com/customer/portal/articles/472098-javascript-configuration-variables

                        // set to 1 if developing, or the site is password protected or not
                        // publicaly accessible
                        //var disqus_developer = 1;

                        // by default, disqus will use the current url to determine a thread
                        // since we might have different parameters present, we remove them
                        // disqus_* vars HAVE TO BE IN GLOBAL SCOPE
                        var disqus_url = window.location.href;
                        var disqus_identifier;
                        if (options.identifier.length > 0) {
                            disqus_identifier = options.identifier;
                        } else {
                            disqus_identifier = disqus_url;
                        }

                        // dynamically load the disqus script
                        var dsq = document.createElement('script');
                        dsq.type = 'text/javascript';
                        dsq.async = true;
                        dsq.src = 'http://' + disqus_shortname + '.disqus.com/embed.js';
                        (document.getElementsByTagName('head')[0] || document.getElementsByTagName('body')[0]).appendChild(dsq);
                    })();
                }
            }
        });
    };
}(jQuery));

(function ($) {
    var language = window.navigator.userLanguage || window.navigator.language;
    var code = language + "_" + language.toUpperCase();
    var fbRootDiv = $('<div id="fb-root" />');
    var fbScriptHref = $.md.prepareLink('connect.facebook.net/' + code + '/all.js#xfbml=1', { forceHTTP: true });
    var fbscript = '(function(d, s, id) { var js, fjs = d.getElementsByTagName(s)[0]; if (d.getElementById(id)) return; js = d.createElement(s); js.id = id; js.src = "' + fbScriptHref + '"; fjs.parentNode.insertBefore(js, fjs);}(document, "script", "facebook-jssdk"));';

    var facebookLikeGimmick = {
        name: 'FacebookLike',
        version: $.md.version,
        once: function () {
            $.md.linkGimmick(this, 'facebooklike', facebooklike);
            $.md.registerScript(this, fbscript, {
                license: 'APACHE2',
                loadstage: 'postgimmick',
                finishstage: 'all_ready'
            });
        }
    };
    $.md.registerGimmick(facebookLikeGimmick);

    function facebooklike($link, opt, text) {
        var default_options = {
            layout: 'standard',
            showfaces: true
        };
        var options = $.extend({}, default_options, opt);
        // Due to a bug, we can have underscores _ in a markdown link
        // so we insert the underscores needed by facebook here
        if (options.layout === 'boxcount') {
            options.layout = 'box_count';
        }
        if (options.layout === 'buttoncount') {
            options.layout = 'button_count';
        }

        return $link.each(function (i, e) {
            var $this = $(e);
            var href = $this.attr('href');
            $('body').append(fbRootDiv);

            var $fb_div = $('<div class="fb-like" data-send="false" data-width="450"></div>');
            $fb_div.attr('data-href', href);
            $fb_div.attr('data-layout', options.layout);
            $fb_div.attr('data-show-faces', options.showfaces);

            $this.replaceWith($fb_div);
        });
    }
}(jQuery));

(function ($) {
    'use strict';
    var forkmeongithubGimmick = {
        name: 'forkmeongithub',
        version: $.md.version,
        once: function () {
            $.md.linkGimmick(this, 'forkmeongithub', forkmeongithub);
        }
    };
    $.md.registerGimmick(forkmeongithubGimmick);

    function forkmeongithub($links, opt, text) {
        return $links.each(function (i, link) {
            var $link = $(link);
            // default options
            var default_options = {
                color: 'red',
                position: 'right'
            };
            var options = $.extend({}, default_options, opt);
            var color = options.color;
            var pos = options.position;

            // the filename for the ribbon
            // see: https://github.com/blog/273-github-ribbons
            var base_href = 'https://s3.amazonaws.com/github/ribbons/forkme_';

            if (color === 'red') {
                base_href += pos + '_red_aa0000.png';
            }
            if (color === 'green') {
                base_href += pos + '_green_007200.png';
            }
            if (color === 'darkblue') {
                base_href += pos + '_darkblue_121621.png';
            }
            if (color === 'orange') {
                base_href += pos + '_orange_ff7600.png';
            }
            if (color === 'white') {
                base_href += pos + '_white_ffffff.png';
            }
            if (color === 'gray') {
                base_href += pos + '_gray_6d6d6d.png';
            }

            var href = $link.attr('href');
            //                var body_pos_top = $('#md-body').offset ().top;
            var body_pos_top = 0;
            var github_link = $('<a class="forkmeongithub" href="' + href + '"><img style="position: absolute; top: ' + body_pos_top + ';' + pos + ': 0; border: 0;" src="' + base_href + '" alt="Fork me on GitHub"></a>');
            // to avoid interfering with other div / scripts, we remove the link and prepend it to the body
            // the fork me ribbon is positioned absolute anyways
            $('body').prepend(github_link);
            github_link.find('img').css('z-index', '2000');
            $link.remove();
        });
    }

}(jQuery));

(function ($) {
    'use strict';
    var gistGimmick = {
        name: 'gist',
        once: function () {
            $.md.linkGimmick(this, 'gist', gist);
        }
    };
    $.md.registerGimmick(gistGimmick);

    function gist($links, opt, href) {
        $().lazygist('init');
        return $links.each(function (i, link) {
            var $link = $(link);
            var gistDiv = $('<div class="gist_here" data-id="' + href + '" />');
            $link.replaceWith(gistDiv);
            gistDiv.lazygist({
                // we dont want a specific file so modify the url template
                url_template: 'https://gist.github.com/{id}.js?'
            });
        });
    }
}(jQuery));