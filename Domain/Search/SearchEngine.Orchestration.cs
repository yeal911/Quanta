// ============================================================================
// 文件名：SearchEngine.Orchestration.cs
// 文件用途：SearchEngine 搜索编排职责拆分（Orchestrator）。
// ============================================================================

using System.Collections.Concurrent;
using System.Linq;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

public partial class SearchEngine
{
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetDefaultResultsAsync(cancellationToken);

        // ── 0. 剪贴板历史（clip 前缀短路，不混入其他结果）─────────
        var clipMatch = System.Text.RegularExpressions.Regex.Match(
            query, @"^clip(?:\s+(.*))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (clipMatch.Success)
        {
            string keyword = clipMatch.Groups[1].Value.Trim();
            return ClipboardHistoryService.Instance.Search(keyword);
        }

        // ── 0.5. 录音命令（record 前缀短路）────────────────────────
        var recordMatch = System.Text.RegularExpressions.Regex.Match(
            query, @"^record(?:\s+(.*))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (recordMatch.Success)
        {
            string filePrefix = recordMatch.Groups[1].Value.Trim();
            return new List<SearchResult> { BuildRecordCommandResult(filePrefix) };
        }

        var results = new ConcurrentBag<SearchResult>();

        // ── 1. 搜索自定义命令和内置命令（同步，始终执行）──────────
        var customResults = SearchCustomCommands(query);
        foreach (var r in customResults) results.Add(r);

        // ── 2. 通过命令路由器处理特殊命令（计算、网页搜索、单位换算）──
        var commandResult = await _commandRouter.TryHandleCommandAsync(query);
        if (commandResult != null)
        {
            // 避免重复：如果 SearchCustomCommands 已经添加了同名的系统操作命令，则跳过
            bool alreadyExists = customResults.Any(r =>
                r.Type == SearchResultType.SystemAction &&
                r.Path?.Equals(commandResult.Subtitle, StringComparison.OrdinalIgnoreCase) == true);
            if (!alreadyExists)
            {
                // 根据类型设置分组标签（全部使用国际化 key）
                if (commandResult.Type == SearchResultType.Calculator)
                {
                    commandResult.GroupLabel = LocalizationService.Get("GroupCalc");
                    commandResult.GroupOrder = GetGroupOrder("GroupCalc");
                }
                else if (commandResult.Type == SearchResultType.QRCode)
                {
                    commandResult.GroupLabel = LocalizationService.Get("GroupQRCode");
                    commandResult.GroupOrder = GetGroupOrder("GroupQRCode");
                }
                else if (commandResult.Type == SearchResultType.SystemAction)
                {
                    commandResult.GroupLabel = LocalizationService.Get("GroupQuanta");
                    commandResult.GroupOrder = GetGroupOrder("GroupQuanta");
                }
                else if (commandResult.Type == SearchResultType.WebSearch)
                {
                    commandResult.GroupLabel = LocalizationService.Get("GroupNetwork");
                    commandResult.GroupOrder = GetGroupOrder("GroupNetwork");
                }
                // Calculator 和 Web 结果应该排在最前面（GroupOrder=0），优先级高于 App/File/Window
                commandResult.GroupOrder = 0;
                // 如果没有设置 MatchScore，给一个默认高分确保显示
                if (commandResult.MatchScore <= 0)
                    commandResult.MatchScore = 1.0;
                results.Add(commandResult);
            }
        }

        // ── 2.1. 文本命令建议（当用户输入部分命令时显示提示）───────
        var textSuggestions = _commandRouter.GetTextCommandSuggestions(query);
        foreach (var suggestion in textSuggestions)
        {
            // 检查是否已存在相同标题的建议
            bool exists = results.Any(r => r.Title?.Equals(suggestion.Title, StringComparison.OrdinalIgnoreCase) == true);
            if (!exists)
            {
                results.Add(suggestion);
            }
        }

        // ── 2.5. 如果查询长度超过阈值，自动生成二维码 ──────────────────
        if (query.Length > _qrCodeThreshold && QRCodeService.Instance.CanGenerateQRCode(query))
        {
            var qrCodeResult = new SearchResult
            {
                Title = LocalizationService.Get("QRCodeGenerate"),
                Subtitle = query.Length > 50 ? query.Substring(0, 50) + "..." : query,
                Path = query,
                Type = SearchResultType.QRCode,
                GroupLabel = LocalizationService.Get("GroupQRCode"),
                GroupOrder = GetGroupOrder("GroupQRCode"),
                MatchScore = 1.0,
                IconText = "📱",
                QueryMatch = query,
                QRCodeContent = query,
                QRCodeImage = QRCodeService.Instance.GenerateQRCodeAutoSize(query)
            };
            results.Add(qrCodeResult);
        }
        // ── 2.6. 如果文本超过2000字符，显示提示信息 ─────────────────────
        else if (query.Length > 2000)
        {
            var hintResult = new SearchResult
            {
                Title = LocalizationService.Get("QRCodeTooLong"),
                Subtitle = "",
                Path = "",
                Type = SearchResultType.Command,
                GroupLabel = "",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "⚠️",
                QueryMatch = ""
            };
            results.Add(hintResult);
        }

        // ── 3. 查询长度 >= 2 时并发搜索文件和窗口 ────────
        if (query.Length >= 2)
        {
            var providerTasks = new List<Task>();

            // 3a. 搜索文件（桌面+下载目录）
            providerTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var fileResults = await _fileSearchProvider.SearchAsync(query, cancellationToken);
                    foreach (var r in fileResults)
                    {
                        r.GroupLabel = LocalizationService.Get("GroupFile");
                        r.GroupOrder = GetGroupOrder("GroupFile");
                        r.IconText = "📄";
                        r.QueryMatch = query;
                        results.Add(r);
                    }
                }
                catch (Exception ex) { Logger.Warn($"File search failed: {ex.Message}"); }
            }, cancellationToken));

            // 3c. 搜索当前打开的窗口（同步快速）
            // 使用包含匹配（%keyword%），不使用模糊子序列匹配
            providerTasks.Add(Task.Run(() =>
            {
                try
                {
                    var windows = _windowManager.GetVisibleWindows();
                    var queryLower = query.ToLower();

                    foreach (var w in windows)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var titleLower = w.Title.ToLower();

                        // 使用包含匹配
                        if (titleLower.Contains(queryLower))
                        {
                            // 完全匹配 = 1.0，前缀匹配 = 0.9，包含匹配 = 0.8
                            double score = titleLower == queryLower ? 1.0
                                : titleLower.StartsWith(queryLower) ? 0.9
                                : 0.8;

                            w.MatchScore = score;
                            w.GroupLabel = LocalizationService.Get("GroupWindow");
                            w.GroupOrder = GetGroupOrder("GroupWindow");
                            w.IconText = "🪟";
                            w.QueryMatch = query;
                            results.Add(w);
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { Logger.Debug($"Window search failed: {ex.Message}"); }
            }, cancellationToken));

            await Task.WhenAll(providerTasks);
        }

        // ── 4. 按匹配分数降序排列；同分时按 GroupOrder 升序（Calculator=2.0 始终置顶）──
        var finalList = results
            .OrderByDescending(r => r.MatchScore)
            .ThenBy(r => r.GroupOrder)
            .ThenByDescending(r => _usageTracker.GetUsageCount(r.Id))
            .Take(_maxResults)
            .ToList();

        // 为每个结果设置索引和 QueryMatch
        for (int i = 0; i < finalList.Count; i++)
        {
            finalList[i].Index = i + 1;
            if (string.IsNullOrEmpty(finalList[i].QueryMatch))
                finalList[i].QueryMatch = query;
        }
        return finalList;
    }

    /// <summary>
    /// 在自定义命令和内置命令中搜索匹配项。
    /// 评分优先级：完全匹配(1.0) > 关键词前缀(0.93) > 名称前缀(0.88) > 关键词包含(0.78) > 名称包含(0.72) > 描述包含(0.60)
    /// 用户自定义命令优先于内置命令（排列在前）。
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <returns>匹配的命令搜索结果列表</returns>
    private List<SearchResult> SearchCustomCommands(string query)
    {
        var results = new List<SearchResult>();
        int index = 0;

        // 将用户命令（优先级更高）与内置命令合并搜索
        var allCommands = _customCommands.Concat(GetBuiltInCommands());

        foreach (var cmd in allCommands)
        {
            // 确定结果类型和分组
            var resultType = cmd.Type.ToLowerInvariant() switch
            {
                "systemaction" => SearchResultType.SystemAction,
                "recordcommand" => SearchResultType.RecordCommand,
                _ => SearchResultType.CustomCommand
            };

            // 分组：内置命令优先使用 GroupKey；自定义命令用 GroupCommand
            string groupKey = !string.IsNullOrEmpty(cmd.GroupKey) ? cmd.GroupKey : "GroupCommand";
            string groupLabel = LocalizationService.Get(groupKey);
            int groupOrder = GetGroupOrder(groupKey);

            if (string.IsNullOrEmpty(query))
            {
                // 查询为空时，返回所有命令（默认匹配分数 1.0）
                results.Add(new SearchResult
                {
                    Index = index++,
                    Id = $"cmd:{cmd.Keyword}",
                    Title = cmd.Keyword,
                    Subtitle = cmd.Name,
                    Path = cmd.Path,
                    IconText = GetIconText(cmd),
                    Type = resultType,
                    CommandConfig = cmd,
                    MatchScore = 1.0,
                    GroupLabel = groupLabel,
                    GroupOrder = groupOrder
                });
            }
            else
            {
                // 根据不同匹配方式计算分数
                double score = 0;

                if (query.Equals(cmd.Keyword, StringComparison.OrdinalIgnoreCase))
                    score = 1.00;   // 关键词完全匹配
                else if (cmd.Keyword.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.93;   // 关键词前缀匹配（如 "rec" → "record"）
                else if (!string.IsNullOrEmpty(cmd.Name) && cmd.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.88;   // 名称前缀匹配
                else if (cmd.Keyword.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.78;   // 关键词包含匹配
                else if (!string.IsNullOrEmpty(cmd.Name) && cmd.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.72;   // 命令名称包含匹配
                else if (!string.IsNullOrEmpty(cmd.Description) && cmd.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.60;   // 命令描述包含匹配

                if (score > 0)
                {
                    // record/clip 通过模糊匹配发现后，需构建完整结果
                    if (resultType == SearchResultType.RecordCommand)
                    {
                        var recordResult = BuildRecordCommandResult("");
                        recordResult.MatchScore = score;
                        results.Add(recordResult);
                    }
                    else
                    {
                        results.Add(new SearchResult
                        {
                            Index = index++,
                            Id = $"cmd:{cmd.Keyword}",
                            Title = cmd.Keyword,
                            Subtitle = cmd.Name,
                            Path = cmd.Path,
                            IconText = GetIconText(cmd),
                            Type = resultType,
                            CommandConfig = cmd,
                            MatchScore = score,
                            GroupLabel = groupLabel,
                            GroupOrder = groupOrder
                        });
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 获取默认搜索结果（当用户未输入任何查询时显示）。
    /// 优先展示最近使用过的命令，剩余位置用用户命令和内置命令补充，最多返回 MaxResults 条。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>默认展示的搜索结果列表（最近使用优先）</returns>
    private async Task<List<SearchResult>> GetDefaultResultsAsync(CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();
        var allCommands = _customCommands.Concat(GetBuiltInCommands()).ToList();

        // 将所有命令按关键字索引，方便按使用记录 ID 查找
        var commandByKey = allCommands.ToDictionary(c => $"cmd:{c.Keyword}", c => c);

        // ── 1. 优先展示最近使用过的命令 ──────────────────────────
        var recentIds = _usageTracker.GetRecentItemIds(_maxResults);
        var addedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in recentIds)
        {
            if (commandByKey.TryGetValue(id, out var cmd))
            {
                results.Add(BuildDefaultResult(cmd, results.Count + 1));
                addedKeywords.Add(cmd.Keyword);
            }
        }

        // ── 2. 用剩余命令填充，直到达到 MaxResults ───────────────
        foreach (var cmd in allCommands)
        {
            if (results.Count >= _maxResults) break;
            if (!addedKeywords.Contains(cmd.Keyword))
            {
                results.Add(BuildDefaultResult(cmd, results.Count + 1));
                addedKeywords.Add(cmd.Keyword);
            }
        }

        return results;
    }

    /// <summary>
    /// 构建默认显示状态下的搜索结果对象（无查询关键字时的展示格式）
    /// </summary>
    private SearchResult BuildDefaultResult(CommandConfig cmd, int index)
    {
        var typeName = cmd.Type.ToLower() switch
        {
            "url" => "🌐 " + cmd.Name,
            "program" => "📦 " + cmd.Name,
            "directory" => "📁 " + cmd.Name,
            "shell" => "⚡ " + cmd.Name,
            "calculator" => "🔢 " + cmd.Name,
            _ => cmd.Name
        };

        return new SearchResult
        {
            Index = index,
            Id = $"cmd:{cmd.Keyword}",
            Title = cmd.Keyword,
            Subtitle = typeName,
            Path = cmd.Path,
            IconText = GetIconText(cmd),
            Type = SearchResultType.CustomCommand,
            CommandConfig = cmd,
            MatchScore = 0.5,
            GroupLabel = "",
            GroupOrder = 0
        };
    }


}
