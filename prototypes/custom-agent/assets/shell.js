// ============================================================
// Inkwell · 自定义 Agent 功能 · H1 原型 · 共享 AppShell + 工具
// ============================================================
//
// 用法：每个页面在 <body> 第一行写
//   <div id="app-shell" data-active="agents"></div>
// 然后 import 本脚本。renderShell() 会自动把 ProLayout 注入进去。
//
// data-active 取值：agents（"我的 Agent"）/ none（不高亮）

(function () {
    // 布尔属性：true 时设上、false/null/undefined 时不设；统一走 IDL 而非 setAttribute
    const BOOL_ATTRS = new Set(['disabled', 'checked', 'readonly', 'required', 'autofocus', 'multiple', 'selected', 'hidden']);

    function el(tag, attrs, children) {
        const node = document.createElement(tag);
        if (attrs) {
            for (const k in attrs) {
                const v = attrs[k];
                // 跳过 null/undefined：避免 setAttribute(k, null) 把属性写成字符串 "null"
                if (v == null || v === false) continue;
                if (k === 'class') node.className = v;
                else if (k === 'html') node.innerHTML = v;
                else if (k.startsWith('on') && typeof v === 'function') {
                    node.addEventListener(k.slice(2).toLowerCase(), v);
                } else if (BOOL_ATTRS.has(k)) {
                    // 布尔属性走 IDL 赋值（true 才置位）
                    node[k] = !!v;
                } else {
                    node.setAttribute(k, v);
                }
            }
        }
        (children || []).forEach((c) => {
            if (c == null) return;
            if (typeof c === 'string') node.appendChild(document.createTextNode(c));
            else node.appendChild(c);
        });
        return node;
    }

    function svg(viewBox, path) {
        const ns = 'http://www.w3.org/2000/svg';
        const s = document.createElementNS(ns, 'svg');
        s.setAttribute('viewBox', viewBox);
        s.setAttribute('width', '16');
        s.setAttribute('height', '16');
        s.setAttribute('fill', 'currentColor');
        const p = document.createElementNS(ns, 'path');
        p.setAttribute('d', path);
        s.appendChild(p);
        return s;
    }

    // 简易 SVG 库（Heroicons 风格）
    const ICONS = {
        robot: 'M9 2a1 1 0 0 1 2 0v1h2V2a1 1 0 1 1 2 0v1h1a3 3 0 0 1 3 3v8a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3V6a3 3 0 0 1 3-3h1V2a1 1 0 0 1 2 0v1H9V2zm-1 8a1 1 0 1 0 0 2 1 1 0 0 0 0-2zm6 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2zm-6 6h6v1a1 1 0 0 1-2 0h-2a1 1 0 0 1-2 0v-1z',
        home: 'M12 2 2 12h3v8h6v-6h2v6h6v-8h3L12 2z',
        book: 'M5 4a2 2 0 0 1 2-2h11v18H7a2 2 0 0 1-2-2V4zm2 0v14h9V4H7z',
        chart: 'M3 3h2v18H3V3zm4 8h2v10H7V11zm4-6h2v16h-2V5zm4 10h2v6h-2v-6zm4-4h2v10h-2V11z',
        setting: 'M11 1h2l1 3 2 1 3-1 1 2-2 2v2l2 2-1 2-3-1-2 1-1 3h-2l-1-3-2-1-3 1-1-2 2-2v-2l-2-2 1-2 3 1 2-1 1-3zm1 7a4 4 0 1 0 0 8 4 4 0 0 0 0-8z',
        more: 'M5 10a2 2 0 1 1 0 4 2 2 0 0 1 0-4zm7 0a2 2 0 1 1 0 4 2 2 0 0 1 0-4zm7 0a2 2 0 1 1 0 4 2 2 0 0 1 0-4z',
        close: 'M6 6l12 12M18 6L6 18',
        plus: 'M12 5v14M5 12h14',
        chevron: 'M6 9l6 6 6-6',
        inbox: 'M3 5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v9h-6l-2 3h-4l-2-3H3V5z',
        check: 'M5 13l4 4 10-10',
    };

    function icon(name, opts) {
        const ns = 'http://www.w3.org/2000/svg';
        const s = document.createElementNS(ns, 'svg');
        s.setAttribute('viewBox', '0 0 24 24');
        s.setAttribute('width', (opts && opts.size) || '16');
        s.setAttribute('height', (opts && opts.size) || '16');
        s.setAttribute('fill', (opts && opts.fill) || 'none');
        s.setAttribute('stroke', (opts && opts.stroke) || 'currentColor');
        s.setAttribute('stroke-width', (opts && opts.strokeWidth) || '2');
        s.setAttribute('stroke-linecap', 'round');
        s.setAttribute('stroke-linejoin', 'round');
        const p = document.createElementNS(ns, 'path');
        p.setAttribute('d', ICONS[name] || '');
        s.appendChild(p);
        return s;
    }

    function renderShell() {
        const mount = document.getElementById('app-shell');
        if (!mount) return;
        const active = mount.dataset.active || 'agents';
        const userName = mount.dataset.user || '张三';

        const layout = el('div', { class: 'pro-layout' }, [
            // Sidebar
            el('aside', { class: 'pro-sidebar' }, [
                el('div', { class: 'pro-sidebar__logo' }, [
                    el('span', { class: 'pro-sidebar__logo-mark' }, ['Ik']),
                    el('span', null, ['Inkwell']),
                ]),
                el('nav', { class: 'pro-sidebar__menu' }, [
                    el('div', { class: 'pro-menu-group' }, ['工作台']),
                    menuItem('home', '首页', false),
                    el('div', { class: 'pro-menu-group' }, ['Agent 中心']),
                    menuItem('robot', '我的 Agent', active === 'agents', 'agents.html'),
                    menuItem('book', '知识库', false, null, true /* 灰显占位 */),
                    menuItem('chart', '运行统计', false, null, true),
                    el('div', { class: 'pro-menu-group' }, ['账户']),
                    menuItem('setting', '设置', false),
                ]),
            ]),
            // Header
            el('header', { class: 'pro-header' }, [
                el('div', { class: 'pro-header__title' }, [mount.dataset.title || '我的 Agent']),
                el('div', { class: 'pro-header__right' }, [
                    el('span', { class: 'pro-header__lang', title: '切换语言' }, [
                        '中 / EN',
                    ]),
                    el('span', { class: 'pro-header__avatar' }, [
                        el('span', { class: 'avatar-circle' }, [userName.slice(0, 1)]),
                        el('span', null, [userName]),
                        icon('chevron'),
                    ]),
                ]),
            ]),
            // Main wrapper
            el('main', { class: 'pro-main', id: 'pro-main' }, []),
        ]);

        mount.replaceWith(layout);

        function menuItem(iconName, text, isActive, href, disabled) {
            const cls = 'pro-menu-item' + (isActive ? ' pro-menu-item--active' : '');
            const node = el(href && !disabled ? 'a' : 'div',
                Object.assign(
                    { class: cls },
                    href && !disabled ? { href, style: 'color: inherit' } : {},
                    disabled ? { style: 'opacity:0.4; cursor:not-allowed' } : {}
                ),
                [
                    el('span', { class: 'pro-menu-item__icon' }, [icon(iconName)]),
                    el('span', null, [text]),
                ]
            );
            return node;
        }
    }

    // ---------- 通用 helpers 暴露给页面用 ----------
    window.UI = {
        el,
        icon,
        renderShell,
        toast(msg, type = 'success') {
            let stack = document.querySelector('.toast-stack');
            if (!stack) {
                stack = el('div', { class: 'toast-stack' }, []);
                document.body.appendChild(stack);
            }
            const t = el('div', { class: `toast toast--${type}` }, [
                el('span', { class: 'toast__icon' }, [type === 'success' ? '✓' : '!']),
                el('span', null, [msg]),
            ]);
            stack.appendChild(t);
            setTimeout(() => {
                t.style.transition = 'opacity 0.2s';
                t.style.opacity = '0';
                setTimeout(() => t.remove(), 200);
            }, 2400);
        },
        showModal(opts) {
            // opts: { title, body (HTMLElement|string), onOk, onCancel, okText, cancelText, danger }
            const closeMask = () => mask.remove();
            const mask = el('div', { class: 'modal-mask', onClick: (e) => { if (e.target === mask) closeMask(); } }, [
                el('div', { class: 'modal' }, [
                    el('div', { class: 'modal__header' }, [
                        el('div', { class: 'modal__title' }, [opts.title || '']),
                        el('span', { class: 'modal__close', onClick: closeMask }, ['×']),
                    ]),
                    el('div', { class: 'modal__body' },
                        typeof opts.body === 'string'
                            ? [el('div', { html: opts.body }, [])]
                            : (opts.body ? [opts.body] : [])
                    ),
                    el('div', { class: 'modal__footer' }, [
                        opts.cancelText !== null
                            ? el('button', { class: 'btn', onClick: () => { closeMask(); opts.onCancel && opts.onCancel(); } }, [opts.cancelText || '取消'])
                            : null,
                        el('button', {
                            class: 'btn ' + (opts.danger ? 'btn--danger' : 'btn--primary'),
                            onClick: () => { closeMask(); opts.onOk && opts.onOk(); },
                        }, [opts.okText || '确认']),
                    ]),
                ]),
            ]);
            document.body.appendChild(mask);
            return mask;
        },
        showDrawer(opts) {
            const closeMask = () => mask.remove();
            const body = el('div', { class: 'drawer__body' },
                opts.body ? [opts.body] : []
            );
            const footer = opts.footer
                ? el('div', { class: 'drawer__footer' }, opts.footer)
                : null;
            const drawer = el('div', { class: 'drawer' }, [
                el('div', { class: 'drawer__header' }, [
                    el('div', { class: 'drawer__title' }, [opts.title || '']),
                    el('span', { class: 'modal__close', onClick: closeMask }, ['×']),
                ]),
                body,
                footer,
            ]);
            const mask = el('div', { class: 'drawer-mask', onClick: (e) => { if (e.target === mask) closeMask(); } }, [drawer]);
            document.body.appendChild(mask);
            return { close: closeMask, root: drawer };
        },
    };

    // 立即同步渲染：#app-shell 在本脚本之前已被解析进 DOM；同步注入 #pro-main
    // 是为了让随后串行执行的页面 inline 脚本（agents.html / agent-edit.html）
    // 能直接 document.getElementById('pro-main') 拿到节点，否则会得到 null 报错。
    // 若 mount 节点暂未就绪，回退到 DOMContentLoaded。
    if (document.getElementById('app-shell')) {
        renderShell();
    } else if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', renderShell);
    } else {
        renderShell();
    }
})();
