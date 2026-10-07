#!/usr/bin/env python3
"""主题硬编码颜色检查（POLA-30 验收脚本）。

检查项：
1. 视图层（Views/*.xaml 与 Views/*.cs）不允许出现硬编码十六进制颜色
   （主题字典 Resources/Themes/*.xaml 自身除外）。
2. Views/*.xaml 引用的每一个 DynamicResource 键必须在两套主题字典中都定义，
   避免切换主题时出现找不到资源或深浅主题不一致。

用法：python3 scripts/check-theme-colors.py  （在仓库根目录执行）
"""

import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
VIEWS_DIR = REPO_ROOT / "Views"
THEME_DIR = REPO_ROOT / "Resources" / "Themes"

HEX_COLOR_RE = re.compile(r"#[0-9A-Fa-f]{6,8}")
DYNAMIC_RESOURCE_RE = re.compile(r"\{DynamicResource\s+([A-Za-z0-9_]+)\}")
THEME_KEY_RE = re.compile(r'x:Key="([A-Za-z0-9_]+)"')

errors: list[str] = []

# 1) 视图层（XAML + code-behind）不允许硬编码十六进制颜色
view_sources = sorted(VIEWS_DIR.glob("*.xaml")) + sorted(VIEWS_DIR.glob("*.cs"))
for source in view_sources:
    for lineno, line in enumerate(source.read_text(encoding="utf-8").splitlines(), start=1):
        for match in HEX_COLOR_RE.finditer(line):
            errors.append(f"{source.relative_to(REPO_ROOT)}:{lineno}: 硬编码颜色 {match.group(0)}")

# 2) 两套主题字典键集合一致，且覆盖 Views 引用的所有 DynamicResource 键
theme_keys: dict[str, set[str]] = {}
for theme in sorted(THEME_DIR.glob("*.xaml")):
    theme_keys[theme.name] = set(THEME_KEY_RE.findall(theme.read_text(encoding="utf-8")))

theme_names = list(theme_keys)
if len(theme_names) >= 2:
    base = theme_keys[theme_names[0]]
    for other in theme_names[1:]:
        for missing in base - theme_keys[other]:
            errors.append(f"{THEME_DIR.name}/{theme_names[0]} 定义了键 {missing}，但 {other} 缺失")
        for extra in theme_keys[other] - base:
            errors.append(f"{THEME_DIR.name}/{other} 定义了键 {extra}，但 {theme_names[0]} 缺失")

all_theme_keys = set().union(*theme_keys.values()) if theme_keys else set()
for xaml in sorted(VIEWS_DIR.glob("*.xaml")):
    for lineno, line in enumerate(xaml.read_text(encoding="utf-8").splitlines(), start=1):
        for key in DYNAMIC_RESOURCE_RE.findall(line):
            if key not in all_theme_keys:
                errors.append(f"{xaml.relative_to(REPO_ROOT)}:{lineno}: DynamicResource 键 {key} 未在任何主题字典中定义")

if errors:
    print("FAIL")
    for error in errors:
        print(f"  {error}")
    sys.exit(1)

print("OK: 视图层（Views/*.xaml 与 Views/*.cs）无硬编码十六进制颜色；所有 DynamicResource 键在两套主题字典中均已定义")
