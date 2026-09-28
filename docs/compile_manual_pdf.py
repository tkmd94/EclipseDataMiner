#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
compile_manual_pdf.py
EclipseDataMiner ドキュメント群を結合し、洗練された CSS スタイリング、HTML タグのエスケープ防止、
日本語フォント設定、ヘッドレス Chrome / Edge を用いて高品質な PDF マニュアルを生成・検証・配備するスクリプト。
（ContourQA / AutoStructureMaker 共通書式完全準拠）
"""

import os
import sys
import re
import shutil
import subprocess
import time
from datetime import datetime

# Windows コンソールでの文字化け・UnicodeEncodeError防止
if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass
if sys.stderr.encoding != 'utf-8':
    try:
        sys.stderr.reconfigure(encoding='utf-8')
    except Exception:
        pass

try:
    import mistune
except ImportError:
    print("[ERROR] mistune is not installed. Please install via: pip install mistune")
    sys.exit(1)

try:
    import fitz  # PyMuPDF
except ImportError:
    print("[ERROR] PyMuPDF is not installed. Please install via: pip install pymupdf")
    sys.exit(1)


# 対象ドキュメント定義（結合順序と章メタデータ）
DOCUMENTS_JA = [
    {
        "file": "README.md",
        "chapter_num": 1,
        "badge": "第1章",
        "title": "システム概要とクイックスタート",
        "desc": "EclipseDataMiner の目的、基本機能、画面構成、および導入・起動手順",
        "remove_first_h1": True
    },
    {
        "file": "docs/MANUAL.md",
        "chapter_num": 2,
        "badge": "第2章",
        "title": "臨床操作仕様・機能詳細マニュアル",
        "desc": "4つのメインタブの操作手順、検索・絞り込み、輪郭マッピング、DQP指標、および抽出実行",
        "remove_first_h1": True,
        "clean_manual_toc": True
    },
    {
        "file": "docs/ARCHITECTURE.md",
        "chapter_num": 3,
        "badge": "第3章",
        "title": "システムアーキテクチャ・設計仕様書",
        "desc": "レイヤ分離構造、STA ワーカースレッド、ストリーミングパイプライン、およびメモリ管理規約",
        "remove_first_h1": True
    },
    {
        "file": "docs/DESIGN_SPECIFICATION.md",
        "chapter_num": 4,
        "badge": "第4章",
        "title": "詳細設計仕様書 (Detailed Specification)",
        "desc": "入出力データ仕様（CSV / JSONL）、プラン複雑度指標（MCS, Edge Metric, LT, AL）、および匿名化規約",
        "remove_first_h1": True
    },
    {
        "file": "docs/COMMISSIONING.md",
        "chapter_num": 5,
        "badge": "第5章",
        "title": "臨床受入試験（コミッショニング）手順書",
        "desc": "受入試験プロトコル、幾何・線量指標の整合性検証、および臨床承認署名票",
        "remove_first_h1": True
    },
    {
        "file": "docs/TROUBLESHOOTING.md",
        "chapter_num": 6,
        "badge": "第6章",
        "title": "トラブルシューティング・FAQ",
        "desc": "ESAPI 権限エラー、メモリ管理、輪郭表記揺れの解決、およびよくある質問",
        "remove_first_h1": True
    },
    {
        "file": "docs/CONTRIBUTING.md",
        "chapter_num": 7,
        "badge": "第7章",
        "title": "開発者ガイド・コーディング規約",
        "desc": "MSBuild x64 ビルド、単体テスト規約、コレクション同期規約、および PR ガイドライン",
        "remove_first_h1": True
    },
    {
        "file": "docs/STANDARD_DEVELOPMENT_PLAN.md",
        "chapter_num": 8,
        "badge": "第8章",
        "title": "標準開発計画仕様書 (SDLP)",
        "desc": "7大品質原則（測度空間整合性、エプシロン分離等）、4層 DoD ゲート、およびリリース規約",
        "remove_first_h1": True
    },
    {
        "file": "docs/CHANGELOG.md",
        "chapter_num": 9,
        "badge": "付録",
        "title": "更新履歴 (Changelog)",
        "desc": "v3.0.1 / v3.0.0 / v2.4.0 リリースノート、主要マイルストーン、および更新履歴",
        "remove_first_h1": True
    }
]

DOCUMENTS_EN = [
    {
        "file": "README.en.md",
        "chapter_num": 1,
        "badge": "Chapter 1",
        "title": "System Overview & Quick Start",
        "desc": "Objective of EclipseDataMiner, core features, UI layout, and deployment procedures",
        "remove_first_h1": True
    },
    {
        "file": "docs/MANUAL.en.md",
        "chapter_num": 2,
        "badge": "Chapter 2",
        "title": "Clinical Operational Manual & Features",
        "desc": "Four main tabs workflow, plan search & filters, structure mapping, DQP metrics, and extraction execution",
        "remove_first_h1": True,
        "clean_manual_toc": True
    },
    {
        "file": "docs/ARCHITECTURE.en.md",
        "chapter_num": 3,
        "badge": "Chapter 3",
        "title": "System Architecture Specification",
        "desc": "Layered architecture, STA worker thread model, streaming export pipeline, and memory management",
        "remove_first_h1": True
    },
    {
        "file": "docs/DESIGN_SPECIFICATION.en.md",
        "chapter_num": 4,
        "badge": "Chapter 4",
        "title": "Detailed Design Specification",
        "desc": "Input/output data specifications (CSV/JSONL), plan complexity metrics (MCS, Edge Metric, LT, AL), and de-identification",
        "remove_first_h1": True
    },
    {
        "file": "docs/COMMISSIONING.en.md",
        "chapter_num": 5,
        "badge": "Chapter 5",
        "title": "Clinical Commissioning Guide",
        "desc": "Acceptance testing protocols, geometric & dosimetric concordance verification, and clinical sign-off form",
        "remove_first_h1": True
    },
    {
        "file": "docs/TROUBLESHOOTING.en.md",
        "chapter_num": 6,
        "badge": "Chapter 6",
        "title": "Troubleshooting & FAQ Guide",
        "desc": "ESAPI permission errors, memory optimization, nomenclature mapping resolutions, and frequently asked questions",
        "remove_first_h1": True
    },
    {
        "file": "docs/CONTRIBUTING.en.md",
        "chapter_num": 7,
        "badge": "Chapter 7",
        "title": "Developer Guide & Coding Standards",
        "desc": "MSBuild x64 build instructions, unit testing guidelines, threading rules, and pull request workflow",
        "remove_first_h1": True
    },
    {
        "file": "docs/STANDARD_DEVELOPMENT_PLAN.en.md",
        "chapter_num": 8,
        "badge": "Chapter 8",
        "title": "Standard Development Lifecycle Protocol (SDLP)",
        "desc": "7 Core Quality Principles (measure space consistency, epsilon clustering, etc.), 4-layer DoD gates, and release policies",
        "remove_first_h1": True
    },
    {
        "file": "docs/CHANGELOG.en.md",
        "chapter_num": 9,
        "badge": "Appendix",
        "title": "Release History (Changelog)",
        "desc": "v3.0.1 / v3.0.0 / v2.4.0 release notes, key milestones, and historical changelog",
        "remove_first_h1": True
    }
]

# 後方互換エイリアス
DOCUMENTS = DOCUMENTS_JA


def strip_yaml_frontmatter(content):
    """YAML Frontmatter (--- ... ---) を除去"""
    pattern = r'^---\s*\n.*?\n---\s*\n'
    return re.sub(pattern, '', content, flags=re.DOTALL)


def format_inline_markdown(text):
    """Callout 内部のインラインマークダウン (コード、太字、リンク等) を HTML タグへ変換"""
    # Inline code: `code` -> <code>code</code>
    text = re.sub(r'`([^`\n]+)`', r'<code>\1</code>', text)
    # Bold: **bold** -> <strong>bold</strong>
    text = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', text)
    # Italic: *italic* -> <em>italic</em>
    text = re.sub(r'(?<!\*)\*([^*\n]+)\*(?!\*)', r'<em>\1</em>', text)
    # Markdown links: [text](url) -> <a href="\2">\1</a>
    text = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'<a href="\2">\1</a>', text)
    return text


def convert_github_callouts(content):
    """GitHub 形式の Callout (> [!NOTE] 等) を洗練された HTML div に変換"""
    callout_types = {
        "NOTE": ("callout-note", "NOTE"),
        "TIP": ("callout-tip", "TIP"),
        "IMPORTANT": ("callout-important", "IMPORTANT"),
        "WARNING": ("callout-warning", "WARNING"),
        "CAUTION": ("callout-caution", "CAUTION")
    }

    lines = content.split('\n')
    result = []
    i = 0
    while i < len(lines):
        line = lines[i]
        match = re.match(r'^>\s*\[!(NOTE|TIP|IMPORTANT|WARNING|CAUTION)\]\s*(.*)$', line)
        if match:
            c_type = match.group(1).upper()
            first_text = match.group(2).strip()
            css_class, label = callout_types[c_type]

            body_lines = []
            if first_text:
                body_lines.append(format_inline_markdown(first_text))

            i += 1
            while i < len(lines) and lines[i].startswith('>'):
                sub_line = re.sub(r'^>\s?', '', lines[i])
                body_lines.append(format_inline_markdown(sub_line))
                i += 1

            body_text = "<br>".join(body_lines)
            div_html = f'''<div class="callout {css_class}">
  <div class="callout-title">{label}</div>
  <div class="callout-body">{body_text}</div>
</div>'''
            result.append(div_html)
            continue
        else:
            result.append(line)
            i += 1

    return '\n'.join(result)


def resolve_image_paths(content, repo_root):
    """Markdown 内の相対画像パスを絶対パス (file:/// スラッシュ区切り) に解決"""
    def _repl(match):
        alt = match.group(1)
        src = match.group(2)
        if not src.startswith("http") and not src.startswith("file:"):
            # docs/ 配下のパスやルート直下のパスを探索
            possible_paths = [
                os.path.abspath(os.path.join(repo_root, src)),
                os.path.abspath(os.path.join(repo_root, "docs", src)),
                os.path.abspath(os.path.join(repo_root, src.lstrip("/\\"))),
            ]
            for p in possible_paths:
                if os.path.exists(p):
                    src = "file:///" + p.replace("\\", "/")
                    break
        return f"![{alt}]({src})"
    return re.sub(r'!\[(.*?)\]\((.*?)\)', _repl, content)


def preprocess_markdown(file_path, chapter_info, repo_root):
    """Markdown 前処理: Frontmatter除去、見出し調整、Callout変換、画像パス解決、章扉バナー付与"""
    if not os.path.exists(file_path):
        print(f"[WARN] File not found: {file_path}")
        return ""

    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    content = strip_yaml_frontmatter(content)

    # shields.io バッジ等を除去
    content = re.sub(r'\[!\[.*?\]\(https://img\.shields\.io/.*?\)\]\(.*?\)', '', content)
    content = re.sub(r'!\[.*?\]\(https://img\.shields\.io/.*?\)', '', content)

    # 各マークダウン内のインライン言語切り替えリンクを除去
    content = re.sub(r'(?m)^(\[\s*(?:English|日本語)\s*\]\([^)]+\)\s*\|\s*.*|\*\*(?:English|日本語)\*\*\s*\|\s*.*)\n+', '', content)

    # 各マークダウン内のインライン目次ブロック (## 目次 または ## Table of Contents ... 次の見出しまで) を除去
    content = re.sub(r'(?m)^## (?:目次|Table of Contents)[\s\S]*?(?=^## |\Z)', '', content)

    content = convert_github_callouts(content)
    content = resolve_image_paths(content, repo_root)

    # 最初の H1 を除去
    if chapter_info.get("remove_first_h1", False):
        content = re.sub(r'^#\s+.*?\n', '', content, count=1)

    # 既存の見出しレベルを1段階下げる (# -> ##, ## -> ###)
    adjusted_lines = []
    in_code_block = False
    for line in content.split('\n'):
        if line.startswith('```'):
            in_code_block = not in_code_block
        if not in_code_block:
            if line.startswith('#'):
                line = '#' + line
        adjusted_lines.append(line)
    content = '\n'.join(adjusted_lines)

    # Mermaid ブロックの変換
    content = re.sub(r'```mermaid\n([\s\S]*?)```', r'<pre class="mermaid">\1</pre>', content)

    # 章扉バナー (Slate & Ocean Cyan の角丸ボックス) の付加
    c_badge = chapter_info["badge"]
    c_title = chapter_info["title"]
    c_desc = chapter_info["desc"]

    banner_html = f'''
<div class="chapter-page-break"></div>
<div class="chapter-banner">
  <div class="chapter-banner-badge">{c_badge}</div>
  <div class="chapter-banner-title">{c_title}</div>
  <div class="chapter-banner-desc">{c_desc}</div>
</div>

'''
    # PDF 印刷時の絵文字フォールバック対策（特定絵文字の正規化）
    content = content.replace("📋", "📄").replace("📏", "📐").replace("📕", "📖").replace("🩺", "🏥")

    return banner_html + content


def generate_css(lang="ja"):
    """洗練された臨床マニュアル PDF 印刷用 CSS (ContourQA / AutoStructureMaker 書式完全準拠)"""
    header_title = 'EclipseDataMiner v3.0.1 Technical Manual' if lang == 'en' else 'EclipseDataMiner v3.0.1 臨床技術マニュアル'
    css = """
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600&display=swap');

@page {
    size: A4 portrait;
    margin: 22mm 18mm 20mm 18mm;
    @top-left {
        content: 'HEADER_TITLE_PLACEHOLDER';
        font-size: 8.5pt;
        color: #64748B;
        font-family: 'Noto Sans JP', 'BIZ UDPGothic', 'Yu Gothic UI', Meiryo, sans-serif;
        border-bottom: 1px solid #CBD5E1;
        padding-bottom: 4px;
        vertical-align: bottom;
    }
    @top-right {
        content: 'Varian Eclipse ESAPI Standalone Tool';
        font-size: 8.5pt;
        color: #64748B;
        font-family: 'Noto Sans JP', 'BIZ UDPGothic', 'Yu Gothic UI', Meiryo, sans-serif;
        border-bottom: 1px solid #CBD5E1;
        padding-bottom: 4px;
        vertical-align: bottom;
    }
    @bottom-left {
        content: 'Confidential - Radiation Oncology & Medical Physics';
        font-size: 8.5pt;
        color: #94A3B8;
        font-family: 'Noto Sans JP', 'BIZ UDPGothic', 'Yu Gothic UI', Meiryo, sans-serif;
        vertical-align: top;
        padding-top: 6px;
    }
    @bottom-right {
        content: 'Page ' counter(page);
        font-size: 8.5pt;
        color: #94A3B8;
        font-family: 'Noto Sans JP', 'BIZ UDPGothic', 'Yu Gothic UI', Meiryo, sans-serif;
        vertical-align: top;
        padding-top: 6px;
    }
}

* {
    box-sizing: border-box;
}

body {
    font-family: 'Noto Sans JP', 'BIZ UDPGothic', 'Yu Gothic UI', 'Meiryo', 'Hiragino Sans', sans-serif;
    font-size: 9.5pt;
    line-height: 1.68;
    color: #334155;
    background-color: #FFFFFF;
    margin: 0;
    padding: 0;
}

/* ========================================================
   表紙 (Cover Page)
   ======================================================== */
.cover-page {
    padding-top: 15px;
    page-break-after: always;
}

.cover-accent-bar {
    width: 100%;
    height: 5px;
    background: linear-gradient(90deg, #0284C7 0%, #0369A1 100%);
    border-radius: 2px;
    margin-bottom: 24px;
}

.cover-badges {
    display: flex;
    gap: 10px;
    margin-bottom: 26px;
}

.pill-badge {
    font-size: 8.5pt;
    font-weight: 600;
    padding: 4px 14px;
    border-radius: 9999px;
    display: inline-block;
}

.pill-badge.blue {
    background-color: #F0F9FF;
    border: 1px solid #BAE6FD;
    color: #0369A1;
}

.pill-badge.purple {
    background-color: #EEF2FF;
    border: 1px solid #C7D2FE;
    color: #4338CA;
}

.pill-badge.green {
    background-color: #ECFDF5;
    border: 1px solid #A7F3D0;
    color: #047857;
}

.cover-title {
    font-size: 38pt;
    font-weight: 800;
    color: #0F172A;
    letter-spacing: -0.02em;
    margin: 0 0 10px 0;
    line-height: 1.15;
}

.cover-version {
    font-size: 18pt;
    font-weight: 600;
    color: #0284C7;
    margin: 0 0 18px 0;
}

.cover-subtitle {
    font-size: 14pt;
    font-weight: 700;
    color: #1E293B;
    margin: 0 0 16px 0;
}

.cover-lead {
    font-size: 9.5pt;
    line-height: 1.75;
    color: #475569;
    margin: 0 0 35px 0;
    text-align: justify;
}

.cover-meta-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
    margin-bottom: 45px;
}

.meta-card {
    background: #FFFFFF;
    border: 1px solid #E2E8F0;
    border-radius: 8px;
    padding: 14px 18px;
}

.meta-card-label {
    font-size: 8pt;
    font-weight: 700;
    color: #64748B;
    letter-spacing: 0.05em;
    text-transform: uppercase;
    margin-bottom: 4px;
}

.meta-card-value {
    font-size: 10.5pt;
    font-weight: 600;
    color: #0F172A;
}

.cover-footer-meta {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    font-size: 8.5pt;
    color: #64748B;
    padding-top: 30px;
}

/* ========================================================
   目次 (Table of Contents)
   ======================================================== */
.toc-page {
    page-break-after: always;
    padding-top: 5px;
}

.toc-header {
    font-size: 20pt;
    font-weight: 700;
    color: #0F172A;
    border-left: 5px solid #0284C7;
    padding-left: 12px;
    border-bottom: 2px solid #0F172A;
    padding-bottom: 10px;
    margin-bottom: 20px;
}

.toc-card {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: #F8FAFC;
    border: 1px solid #E2E8F0;
    border-radius: 8px;
    padding: 11px 16px;
    margin-bottom: 9px;
    page-break-inside: avoid;
}

.toc-badge {
    background-color: #0284C7;
    color: #FFFFFF;
    font-size: 8.5pt;
    font-weight: 600;
    padding: 5px 12px;
    border-radius: 6px;
    min-width: 54px;
    text-align: center;
}

.toc-card-text {
    flex: 1;
    margin-left: 16px;
}

.toc-card-title {
    font-size: 10pt;
    font-weight: 700;
    color: #0F172A;
}

.toc-card-desc {
    font-size: 8.5pt;
    color: #64748B;
    margin-top: 2px;
}

.toc-arrow {
    font-size: 12pt;
    color: #94A3B8;
    margin-left: 10px;
}

/* ========================================================
   章扉バナー (Chapter Header Banner)
   ======================================================== */
.chapter-page-break {
    page-break-before: always;
}

.chapter-banner {
    background: linear-gradient(135deg, #0F172A 0%, #1E293B 100%);
    border-radius: 10px;
    padding: 22px 26px;
    margin-top: 6px;
    margin-bottom: 24px;
    color: #FFFFFF;
    page-break-inside: avoid;
}

.chapter-banner-badge {
    display: inline-block;
    background: rgba(2, 132, 199, 0.25);
    border: 1px solid rgba(56, 189, 248, 0.4);
    color: #7DD3FC;
    font-size: 8.5pt;
    font-weight: 600;
    padding: 3px 10px;
    border-radius: 9999px;
    margin-bottom: 8px;
}

.chapter-banner-title {
    font-size: 18pt;
    font-weight: 700;
    color: #FFFFFF;
    margin: 0 0 8px 0;
    line-height: 1.25;
}

.chapter-banner-desc {
    font-size: 9pt;
    line-height: 1.5;
    color: #94A3B8;
    margin: 0;
}

/* ========================================================
   見出しスタイル
   ======================================================== */
h2 {
    font-size: 13.5pt;
    font-weight: 700;
    color: #0F172A;
    border-left: 5px solid #0284C7;
    padding-left: 10px;
    margin-top: 26px;
    margin-bottom: 12px;
    page-break-after: avoid;
    break-after: avoid;
}

h3 {
    font-size: 11pt;
    font-weight: 700;
    color: #1E293B;
    margin-top: 20px;
    margin-bottom: 8px;
    page-break-after: avoid;
    break-after: avoid;
}

h4, h5, h6 {
    font-size: 10pt;
    font-weight: 600;
    color: #334155;
    margin-top: 16px;
    margin-bottom: 6px;
    page-break-after: avoid;
    break-after: avoid;
}

p {
    margin-top: 0;
    margin-bottom: 12px;
    text-align: justify;
}

/* ========================================================
   リスト
   ======================================================== */
ul, ol {
    margin-top: 0;
    margin-bottom: 14px;
    padding-left: 22px;
}

li {
    margin-bottom: 4px;
}

/* ========================================================
   テーブル (濃紺ヘッダー + 白文字)
   ======================================================== */
table {
    width: 100%;
    border-collapse: collapse;
    margin: 14px 0 20px 0;
    font-size: 8.5pt;
    border: 1px solid #CBD5E1;
    page-break-inside: avoid;
    break-inside: avoid;
}

th, td {
    border: 1px solid #E2E8F0;
    padding: 7px 10px;
    text-align: left;
    vertical-align: top;
}

th {
    background-color: #0F172A;
    color: #FFFFFF;
    font-weight: 600;
    border: 1px solid #334155;
}

tr:nth-child(even) td {
    background-color: #F8FAFC;
}

tr:nth-child(odd) td {
    background-color: #FFFFFF;
}

/* ========================================================
   コードブロック & インラインコード
   ======================================================== */
code {
    font-family: 'JetBrains Mono', 'Consolas', 'BIZ UDGothic', monospace;
    font-size: 8.5pt;
    background-color: #F1F5F9;
    color: #0F172A;
    padding: 2px 5px;
    border-radius: 4px;
    border: 1px solid #E2E8F0;
    word-break: break-word;
}

pre {
    background-color: #F8FAFC;
    color: #0F172A;
    border: 1px solid #CBD5E1;
    padding: 12px 16px;
    border-radius: 6px;
    white-space: pre-wrap;
    word-wrap: break-word;
    word-break: break-all;
    overflow: visible;
    font-family: 'JetBrains Mono', 'Consolas', 'BIZ UDGothic', monospace;
    font-size: 8.2pt;
    line-height: 1.5;
    margin: 12px 0 16px 0;
    page-break-inside: avoid;
    break-inside: avoid;
}

pre code {
    background: transparent;
    color: inherit;
    padding: 0;
    border: none;
    font-size: inherit;
}

/* ========================================================
   Callouts (洗練されたアクセントボーダー)
   ======================================================== */
.callout {
    border-radius: 4px;
    margin: 14px 0 18px 0;
    padding: 10px 14px;
    font-size: 9pt;
    page-break-inside: avoid;
    break-inside: avoid;
}

.callout-title {
    font-weight: 700;
    font-size: 9.5pt;
    margin-bottom: 4px;
    letter-spacing: 0.02em;
}

.callout-body {
    line-height: 1.55;
}

.callout-caution {
    border-left: 4px solid #DC2626;
    background-color: #FEF2F2;
    color: #7F1D1D;
}
.callout-caution .callout-title { color: #DC2626; }

.callout-warning {
    border-left: 4px solid #D97706;
    background-color: #FFFBEB;
    color: #78350F;
}
.callout-warning .callout-title { color: #D97706; }

.callout-important {
    border-left: 4px solid #7C3AED;
    background-color: #F5F3FF;
    color: #581C87;
}
.callout-important .callout-title { color: #7C3AED; }

.callout-note {
    border-left: 4px solid #0284C7;
    background-color: #F0F9FF;
    color: #075985;
}
.callout-note .callout-title { color: #0284C7; }

.callout-tip {
    border-left: 4px solid #059669;
    background-color: #ECFDF5;
    color: #064E3B;
}
.callout-tip .callout-title { color: #059669; }

/* 引用 */
blockquote {
    border-left: 4px solid #CBD5E1;
    margin: 12px 0;
    padding: 8px 16px;
    color: #475569;
    background-color: #F8FAFC;
    border-radius: 0 4px 4px 0;
}

/* 水平線 */
hr {
    border: none;
    border-top: 1px solid #E2E8F0;
    margin: 22px 0;
}

/* 埋め込み画像 */
img {
    max-width: 100%;
    height: auto;
    display: block;
    margin: 14px auto 18px auto;
    border: 1px solid #CBD5E1;
    border-radius: 6px;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.06);
    page-break-inside: avoid;
    break-inside: avoid;
}

/* Mermaid ダイアグラム */
.mermaid {
    background-color: #F8FAFC;
    border: 1px solid #E2E8F0;
    border-radius: 8px;
    padding: 16px;
    text-align: center;
    margin: 16px 0;
    page-break-inside: avoid;
    break-inside: avoid;
}

/* 数式組版 (MathJax 3 SVG) */
mjx-container {
    font-size: 105% !important;
    page-break-inside: avoid;
    break-inside: avoid;
}
mjx-container[jax="SVG"][display="true"] {
    margin: 14px 0 !important;
    overflow-x: visible !important;
    overflow-y: hidden !important;
}
"""
    return css.replace("HEADER_TITLE_PLACEHOLDER", header_title)


def generate_cover_html(lang="ja"):
    """洗練された表紙 HTML (Slate & Ocean Cyan エグゼクティブ・メディカルスタイル)"""
    if lang == "en":
        return """
<div class="cover-page">
  <div class="cover-accent-bar"></div>
  <div class="cover-badges">
    <div class="pill-badge blue">Varian Medical Systems Eclipse</div>
    <div class="pill-badge purple">ESAPI v15.6 / v16.1</div>
    <div class="pill-badge green">Production Ready (v3.0.1)</div>
  </div>
  <div class="cover-title">EclipseDataMiner</div>
  <div class="cover-version">Version 3.0.1 (Released: September 28, 2026)</div>
  <div class="cover-subtitle">Radiation Therapy Plan Data Mining & Quality Assurance Technical Manual</div>
  <div class="cover-lead">
    This manual provides comprehensive technical documentation for <strong>EclipseDataMiner</strong>, a high-throughput, memory-safe data mining and quality management system querying 10,000+ radiation therapy plans across Varian Eclipse (ESAPI). It covers system installation, operational workflows, structure nomenclature mapping (Pre-Scan &amp; Alias Mapping), Dose-Quality Parameter (DQP) extraction, delivery modulation complexity modeling (MCS, Edge Metric, Leaf Travel Length, Arc Length), STA worker streaming architecture, and clinical commissioning procedures.
  </div>
  <div class="cover-meta-grid">
    <div class="meta-card">
      <div class="meta-card-label">Target Environment</div>
      <div class="meta-card-value">Varian Eclipse v15.6 / v16.1 (ESAPI)</div>
    </div>
    <div class="meta-card">
      <div class="meta-card-label">Execution Framework</div>
      <div class="meta-card-value">Microsoft .NET Framework 4.6.1 (x64)</div>
    </div>
    <div class="meta-card">
      <div class="meta-card-label">Deployment Architecture</div>
      <div class="meta-card-value">Single Standalone Executable (Costura.Fody)</div>
    </div>
    <div class="meta-card">
      <div class="meta-card-label">Validation Status</div>
      <div class="meta-card-value">117/117 Tests Passed (100% PASS)</div>
    </div>
  </div>
  <div class="cover-footer-meta">
    <div>Department of Radiation Oncology &amp; Medical Physics</div>
    <div>Document ID: EDM-MAN-2026-V301-EN • September 28, 2026</div>
  </div>
</div>
"""
    return """
<div class="cover-page">
  <div class="cover-accent-bar"></div>
  <div class="cover-badges">
    <div class="pill-badge blue">Varian Medical Systems Eclipse</div>
    <div class="pill-badge purple">ESAPI v15.6 / v16.1</div>
    <div class="pill-badge green">Production Ready (v3.0.1)</div>
  </div>
  <div class="cover-title">EclipseDataMiner</div>
  <div class="cover-version">Version 3.0.1 (Released: September 28, 2026)</div>
  <div class="cover-subtitle">放射線治療計画データマイニング・品質保証 総合技術マニュアル</div>
  <div class="cover-lead">
    本マニュアルは、放射線治療計画装置 Varian Eclipse における10,000件規模の治療計画データマイニング・品質管理システム
    <strong>EclipseDataMiner</strong> の導入手順、操作仕様、輪郭事前マッピング（Pre-Scan &amp; Alias Mapping）、
    線量指標 (DQP) 抽出、照射野幾何学的複雑度解析モデル (MCS, Edge Metric, Leaf Travel Length, Arc Length)、
    STA ワーカースレッド・ストリーミングアーキテクチャ、およびトラブルシューティングを網羅した公式技術文書です。
  </div>
  <div class="cover-meta-grid">
    <div class="meta-card">
      <div class="meta-card-label">Target Environment</div>
      <div class="meta-card-value">Varian Eclipse v15.6 / v16.1 (ESAPI)</div>
    </div>
    <div class="meta-card">
      <div class="meta-card-label">Execution Framework</div>
      <div class="meta-card-value">Microsoft .NET Framework 4.6.1 (x64)</div>
    </div>
    <div class="meta-card">
      <div class="meta-card-label">Deployment Architecture</div>
      <div class="meta-card-value">Single Executable (Costura.Fody)</div>
    </div>
    <div class="meta-card">
      <div class="meta-card-label">Validation Status</div>
      <div class="meta-card-value">117/117 Tests Passed (100% PASS)</div>
    </div>
  </div>
  <div class="cover-footer-meta">
    <div>Department of Radiation Oncology &amp; Medical Physics</div>
    <div>Document ID: EDM-MAN-2026-V301 • September 28, 2026</div>
  </div>
</div>
"""


def generate_toc_html(documents, lang="ja"):
    """目次 HTML (各章バッジ＋カード風リスト)"""
    header_text = "Table of Contents" if lang == "en" else "目次 (Table of Contents)"
    html = '<div class="toc-page">\n'
    html += f'  <div class="toc-header">{header_text}</div>\n'
    for doc in documents:
        badge = doc["badge"]
        title = doc["title"]
        desc = doc["desc"]
        html += f'''  <div class="toc-card">
    <div class="toc-badge">{badge}</div>
    <div class="toc-card-text">
      <div class="toc-card-title">{title}</div>
      <div class="toc-card-desc">{desc}</div>
    </div>
    <div class="toc-arrow">&rarr;</div>
  </div>\n'''
    html += '</div>\n'
    return html


def find_browser_path():
    """ヘッドレス Chrome または Edge の実行パスを自動検出"""
    candidates = [
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    ]
    for c in candidates:
        if os.path.exists(c):
            return c
    return None


def convert_markdown_with_math(markdown_text):
    """数式保護機能付き HTML 変換 (escape=False を厳守)"""
    code_tokens = []
    def _save_code(m):
        idx = len(code_tokens)
        code_tokens.append(m.group(0))
        return f"@@CODE_TOKEN_{idx}@@"

    text = re.sub(r'```[\s\S]*?```', _save_code, markdown_text)
    text = re.sub(r'`[^`\n]+`', _save_code, text)

    math_blocks = []
    def _save_block(m):
        idx = len(math_blocks)
        math_blocks.append(m.group(0))
        return f"@@MATH_BLOCK_{idx}@@"

    text = re.sub(r'\$\$([\s\S]*?)\$\$', _save_block, text)

    inline_maths = []
    def _save_inline(m):
        idx = len(inline_maths)
        inline_maths.append(m.group(0))
        return f"@@INLINE_MATH_{idx}@@"

    text = re.sub(r'(?<!\$)\$(?!\$)([^\$\n]+?)(?<!\$)\$(?!\$)', _save_inline, text)

    for i, token in enumerate(code_tokens):
        text = text.replace(f"@@CODE_TOKEN_{i}@@", token)

    renderer = mistune.Renderer(escape=False)
    markdown_parser = mistune.Markdown(renderer=renderer, escape=False)
    html = markdown_parser(text)

    for i, block in enumerate(math_blocks):
        html = html.replace(f"@@MATH_BLOCK_{i}@@", block)
    for i, inline in enumerate(inline_maths):
        html = html.replace(f"@@INLINE_MATH_{i}@@", inline)

    return html


def compile_single_manual(lang="ja", repo_root=None, script_dir=None):
    if repo_root is None:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, ".."))

    lang_label = "English" if lang == "en" else "Japanese"
    output_pdf_name = "EclipseDataMiner_v3.0.1_Manual.en.pdf" if lang == "en" else "EclipseDataMiner_v3.0.1_Manual.pdf"
    documents = DOCUMENTS_EN if lang == "en" else DOCUMENTS_JA

    print(f"\n[*] Starting {lang_label} Technical Manual compilation ({output_pdf_name})...")

    # 1. 各マークダウンの結合と前処理
    combined_md = ""
    for doc_info in documents:
        rel_path = doc_info["file"]
        full_path = os.path.join(repo_root, rel_path)
        print(f"  -> Processing {doc_info['badge']}: {rel_path}...")
        processed = preprocess_markdown(full_path, doc_info, repo_root)
        combined_md += processed + "\n\n"

    # 2. 数式保護機能付き HTML 変換 (escape=False を厳守)
    print(f"[*] Converting Markdown to HTML ({lang_label})...")
    body_html = convert_markdown_with_math(combined_md)

    cover_html = generate_cover_html(lang)
    toc_html = generate_toc_html(documents, lang)
    css_content = generate_css(lang)

    html_lang = "en" if lang == "en" else "ja"
    doc_title = "EclipseDataMiner Technical Manual" if lang == "en" else "EclipseDataMiner 臨床技術マニュアル"

    full_html = f"""<!DOCTYPE html>
<html lang="{html_lang}">
<head>
<meta charset="UTF-8">
<title>{doc_title}</title>
<style>
{css_content}
</style>
<script>
window.MathJax = {{
  tex: {{
    inlineMath: [['$', '$']],
    displayMath: [['$$', '$$']]
  }},
  svg: {{
    fontCache: 'global'
  }}
}};
</script>
<script type="text/javascript" id="MathJax-script" async
  src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-svg.js">
</script>
<script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
<script>
mermaid.initialize({{ startOnLoad: true, theme: 'neutral' }});
</script>
</head>
<body>
{cover_html}
{toc_html}
{body_html}
</body>
</html>
"""

    temp_html_path = os.path.join(script_dir, f"temp_compiled_manual_{lang}.html")
    with open(temp_html_path, "w", encoding="utf-8") as f:
        f.write(full_html)
    print(f"[*] Generated intermediate HTML: {temp_html_path}")

    # 3. ブラウザ自動検出と PDF 印刷
    browser_exe = find_browser_path()
    if not browser_exe:
        print("[ERROR] Chrome or Edge executable not found!")
        sys.exit(1)

    print(f"[*] Printing PDF via headless browser ({lang_label})...")
    temp_pdf_path = os.path.join(script_dir, f"_temp_print_manual_{lang}.pdf")

    cmd = [
        browser_exe,
        "--headless=new",
        "--disable-gpu",
        "--no-pdf-header-footer",
        "--run-all-compositor-stages-before-draw",
        "--virtual-time-budget=8000",
        f"--print-to-pdf={temp_pdf_path}",
        f"--lang={'en' if lang == 'en' else 'ja'}",
        temp_html_path
    ]

    proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if proc.returncode != 0 or not os.path.exists(temp_pdf_path):
        print(f"[ERROR] PDF generation failed with code {proc.returncode}")
        print(proc.stderr.decode("utf-8", errors="ignore"))
        sys.exit(1)

    print(f"[+] PDF generated successfully ({os.path.getsize(temp_pdf_path):,} bytes).")

    # 4. PyMuPDF による自動品質検証
    print(f"[*] Validating {lang_label} PDF quality via PyMuPDF...")
    doc = fitz.open(temp_pdf_path)
    total_pages = len(doc)
    print(f"[*] Total Pages ({lang_label}): {total_pages}")

    lint_issues = []
    for i in range(total_pages):
        page = doc[i]
        text = page.get_text()

        # 1. HTMLタグ漏れチェック
        if "&lt;div" in text or "&lt;br" in text or "<div class=" in text or "</div>" in text:
            lint_issues.append(f"Page {i+1}: Unrendered HTML tag detected.")

        # 2. 豆腐文字・不正エンコーディング記号チェック
        if "\ufffd" in text:
            lint_issues.append(f"Page {i+1}: Replacement character (\\ufffd) detected.")

        # 3. 横スクロールバー起因の Fluent Icon チェック
        d = page.get_text("dict")
        for b in d.get("blocks", []):
            for l in b.get("lines", []):
                for s in l.get("spans", []):
                    stext = s.get("text", "")
                    if any(c in stext for c in ['\uedd9', '\uedda']):
                        lint_issues.append(f"Page {i+1}: Scrollbar Fluent icon detected (overflow).")

        # 4. Callout 内未レンダリング Markdown チェック
        for line in text.splitlines():
            if "**" in line and not line.strip().startswith("```"):
                lint_issues.append(f"Page {i+1}: Unrendered markdown bold (**) in line: {line.strip()[:60]}")

        # 5. Mermaid ダイアグラム構文エラーチェック
        if "Syntax error in text" in text or "mermaid version" in text:
            lint_issues.append(f"Page {i+1}: Mermaid diagram syntax error detected ('Syntax error in text').")

    if lint_issues:
        print(f"[WARN] {len(lint_issues)} potential issues detected in {output_pdf_name}:")
        for issue in lint_issues:
            print(f"  - {issue}")
    else:
        print(f"[+] Quality Validation PASSED: All {total_pages} pages clean without raw HTML, scrollbar artifacts, or encoding errors.")


    doc.close()

    # 5. 成果物の配備同期 (Root, docs/, release/)
    print(f"[*] Synchronizing {output_pdf_name} artifacts...")
    deploy_targets = [
        os.path.join(repo_root, output_pdf_name),
        os.path.join(repo_root, "docs", output_pdf_name),
        os.path.join(repo_root, "release", output_pdf_name),
    ]

    for target in deploy_targets:
        target_dir = os.path.dirname(target)
        if not os.path.exists(target_dir):
            os.makedirs(target_dir, exist_ok=True)
        shutil.copyfile(temp_pdf_path, target)
        print(f"  -> Deployed: {os.path.relpath(target, repo_root)}")

    # 中間ファイルのクリーンアップ
    for temp_f in [temp_html_path, temp_pdf_path]:
        try:
            if os.path.exists(temp_f):
                os.remove(temp_f)
        except Exception:
            pass

    print(f"  [SUCCESS] {output_pdf_name} ({total_pages} pages) Complete!\n")
    return True


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    repo_root = os.path.abspath(os.path.join(script_dir, ".."))

    print("===================================================")
    print("  EclipseDataMiner - Technical Manual PDF Compiler")
    print("===================================================")
    print(f"[*] Workspace Root: {repo_root}")

    # Determine languages to compile
    langs = []
    if "--lang=ja" in sys.argv:
        langs = ["ja"]
    elif "--lang=en" in sys.argv:
        langs = ["en"]
    else:
        langs = ["ja", "en"]

    for lang in langs:
        compile_single_manual(lang=lang, repo_root=repo_root, script_dir=script_dir)

    # Legacy redundant PDF cleanup
    legacy_names = [
        "EclipseDataMiner_Manual.pdf",
        "EclipseDataMiner_Manual_v3.0.0.pdf",
        "EclipseDataMiner_v3.0.0_Manual.pdf",
        "EclipseDataMiner_v3.0.0_Manual.en.pdf"
    ]
    for d in [repo_root, os.path.join(repo_root, "docs"), os.path.join(repo_root, "release")]:
        for leg in legacy_names:
            p = os.path.join(d, leg)
            if os.path.exists(p):
                try:
                    os.remove(p)
                    print(f"  -> Removed redundant PDF: {os.path.relpath(p, repo_root)}")
                except Exception:
                    pass

    print("===================================================")
    print("  [ALL COMPLETED] All Technical Manual PDFs Deployed!")
    print("===================================================")


if __name__ == "__main__":
    main()
