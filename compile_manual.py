#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
compile_manual.py
ルート直下からの実行用エントリポイント。
実際の処理は docs/compile_manual_pdf.py に委譲します。
"""

import os
import sys
import subprocess

script_dir = os.path.dirname(os.path.abspath(__file__))
docs_compiler = os.path.join(script_dir, "docs", "compile_manual_pdf.py")

if not os.path.exists(docs_compiler):
    print(f"[ERROR] Compiler script not found: {docs_compiler}")
    sys.exit(1)

result = subprocess.run([sys.executable, docs_compiler] + sys.argv[1:])
sys.exit(result.returncode)
